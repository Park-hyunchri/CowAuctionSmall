using CowAuctionSmall.Infra.Logging;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

internal static class LoggingChecks
{
    internal static async Task Run()
    {
        await CheckDebugOutput();
        CheckFileConfiguration();
        await CheckFileBackpressure();
        CheckFileFailureAndReload();
        CheckFileShutdownTimeout();
        await MeasureOutput();
    }

    private static async Task CheckDebugOutput()
    {
        var batches = new ConcurrentQueue<string>();
        var occurred = new DateTimeOffset(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero);
        string[] messages = Enumerable.Range(1, 501).Select(index => $"SV|SYNTHETIC|{index}|||612|END|").ToArray();
        using (var writer = new AsyncDebugWriter(batches.Enqueue))
        {
            Program.Check(messages.All(message => writer.TryWrite(message, "Raw", occurred)), "501 synthetic raw messages fit debug queue");
            var result = writer.Stop(TimeSpan.FromSeconds(3));
            string[] lines = batches.SelectMany(Lines).ToArray();
            Program.Check(lines.Length == 501 && lines.Select(Raw).SequenceEqual(messages), "501 raw strings preserve pipes, empty fields, boundaries, and order");
            Program.Check(lines.All(line => line.StartsWith("[03:04:05.678]", StringComparison.Ordinal)), "delayed output preserves captured occurrence time");
            Program.Check(lines.Select(Sequence).SequenceEqual(Enumerable.Range(1, 501).Select(index => (long)index)), "debug sequence follows enqueue order");
            Program.Check(result.Written == 501 && result.Dropped == 0 && result.PendingCount == 0 && result.Stopped,
                "normal debug stop drains all accepted messages");
            Program.Check(result.OutputCalls < 501, "debug messages are batched into fewer sink calls");
            Program.Check(!writer.TryWrite("LATE") && writer.GetStatistics().RejectedAfterStop == 1, "late debug writes rejected after shutdown");
            Program.Check(writer.Stop(TimeSpan.Zero).Written == 501, "repeated debug shutdown does not duplicate messages");
        }

        batches.Clear();
        using (var writer = new AsyncDebugWriter(batches.Enqueue))
        {
            await Task.WhenAll(Enumerable.Range(0, 4).Select(producer => Task.Run(() =>
            {
                for (int index = 0; index < 100; index++)
                    if (!writer.TryWrite($"P{producer}:{index}", "Producer")) throw new InvalidOperationException("Unexpected multi-producer drop.");
            })));
            var result = writer.Stop(TimeSpan.FromSeconds(3));
            string[] lines = batches.SelectMany(Lines).ToArray();
            Program.Check(result.Written == 400 && lines.Select(Sequence).SequenceEqual(Enumerable.Range(1, 400).Select(index => (long)index)),
                "multiple producers get a single ordered nonduplicated sequence");
            for (int producer = 0; producer < 4; producer++)
                Program.Check(lines.Select(Raw).Where(raw => raw.StartsWith($"P{producer}:", StringComparison.Ordinal))
                    .SequenceEqual(Enumerable.Range(0, 100).Select(index => $"P{producer}:{index}")), "each producer retains its own order");
        }

        using (var entered = new ManualResetEventSlim())
        using (var release = new ManualResetEventSlim())
        using (var writer = new AsyncDebugWriter(text => { entered.Set(); release.Wait(TimeSpan.FromSeconds(10)); batches.Enqueue(text); },
            capacity: 8, batchSize: 1, batchInterval: TimeSpan.Zero))
        {
            try
            {
                writer.TryWrite("GATE");
                Program.Check(entered.Wait(TimeSpan.FromSeconds(3)), "debug sink reached controlled blocking point");
                var producer = Task.Run(() =>
                {
                    for (int index = 0; index < 501; index++) writer.TryWrite("SYNTHETIC_" + index);
                });
                Program.Check(await Task.WhenAny(producer, Task.Delay(2000)) == producer, "501 debug registrations complete while sink remains blocked");
                await producer;
                var result = writer.GetStatistics();
                Program.Check(result.Accepted == 9 && result.Dropped == 493 && result.PeakCount == 8,
                    "debug count cap rejects precisely excess entries without blocking");
                Program.Check(result.FirstDroppedSequence == 10 && result.LastDroppedSequence == 502,
                    "debug overload tracks missing sequence range");
                var stopwatch = Stopwatch.StartNew();
                result = writer.Stop(TimeSpan.FromMilliseconds(30));
                Program.Check(stopwatch.Elapsed < TimeSpan.FromSeconds(1) && result.UnwrittenAtStop == 9 && !result.Stopped,
                    "blocked debug sink respects stop budget and reports queued plus in-flight remainder");
                Program.Check(!writer.TryWrite("LATE"), "stop timeout does not reopen debug admission");
            }
            finally { release.Set(); writer.Stop(TimeSpan.FromSeconds(3)); }
        }

        batches.Clear();
        using (var entered = new ManualResetEventSlim())
        using (var release = new ManualResetEventSlim())
        using (var writer = new AsyncDebugWriter(text => { entered.Set(); release.Wait(TimeSpan.FromSeconds(10)); batches.Enqueue(text); },
            characterCapacity: 20, batchSize: 1, batchInterval: TimeSpan.Zero))
        {
            try
            {
                writer.TryWrite("G", "");
                Program.Check(entered.Wait(TimeSpan.FromSeconds(3)), "character cap test blocks the consumer");
                Program.Check(writer.TryWrite(new string('a', 20), "") && !writer.TryWrite("b", "")
                    && !writer.TryWrite(new string('c', 21), ""), "debug total character cap handles both saturation and a single oversized message");
                Program.Check(writer.GetStatistics().PeakCharacters == 20, "debug retained character count stays within configured cap");
            }
            finally { release.Set(); writer.Stop(TimeSpan.FromSeconds(3)); }
            Program.Check(batches.Any(batch => batch.Contains("dropped=2", StringComparison.Ordinal)), "recovered debug output reports overload without recursively queueing it");
            Program.Check(batches.SelectMany(Lines).Where(line => line.Contains("[#", StringComparison.Ordinal)).Select(Raw)
                .SequenceEqual(new[] { "G", new string('a', 20) }), "oversized-for-batch accepted message remains whole");
        }

        using (var writer = new AsyncDebugWriter(_ => throw new IOException("Synthetic output failure")))
        {
            writer.TryWrite("SYNTHETIC_FAIL");
            var result = writer.Stop(TimeSpan.FromSeconds(3));
            Program.Check(result.Failed == 1 && result.Written == 0 && result.Stopped, "debug sink exception is counted without escaping to producer");
        }

        bool enabled = AsyncDebugOutput.Enabled;
        bool diagnostics = AsyncDebugOutput.DiagnosticsEnabled;
        int sideEffects = 0;
        try
        {
            AsyncDebugOutput.Enabled = false;
            AsyncDebugOutput.WriteLine($"disabled {++sideEffects}");
            Program.Check(sideEffects == 0, "disabled interpolated debug logging skips expression evaluation");
            AsyncDebugOutput.Enabled = true;
            AsyncDebugOutput.DiagnosticsEnabled = false;
            AsyncDebugOutput.WriteDiagnostic($"disabled diagnostic {++sideEffects}");
            Program.Check(sideEffects == 0, "disabled repeated diagnostic skips interpolation");
            AsyncDebugOutput.WriteLine($"conditional {++sideEffects}");
#if DEBUG
            Program.Check(sideEffects == 1, "Debug build evaluates enabled logging expression once");
#else
            Program.Check(sideEffects == 0 && AsyncDebugOutput.GetStatistics() == null, "Release conditional calls collect no raw data or strings and start no output worker");
#endif
        }
        finally { AsyncDebugOutput.Enabled = enabled; AsyncDebugOutput.DiagnosticsEnabled = diagnostics; }
    }

    private static void CheckFileConfiguration()
    {
        string directory = Path.Combine(Path.GetTempPath(), "CowAuction-logging-checks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string logPath = Path.Combine(directory, "synthetic.log");
        using var factory = new LogFactory();
        var lifetime = new LoggingLifetime(factory, _ => { });
        try
        {
            var configuration = new XmlLoggingConfiguration(Path.Combine(AppContext.BaseDirectory, "NLog.config"), factory);
            var wrappers = configuration.AllTargets.OfType<AsyncTargetWrapper>().ToArray();
            Program.Check(wrappers.Length == 1, "product NLog configuration has one explicit async wrapper");
            var wrapper = wrappers.Single();
            Program.Check(wrapper.QueueLimit == 10000 && wrapper.BatchSize == 200
                && wrapper.OverflowAction == AsyncTargetWrapperOverflowAction.Discard,
                "file queue explicitly bounds count and discards overload rather than blocking or growing");
            Program.Check(configuration.LoggingRules.Count > 0
                && configuration.LoggingRules.SelectMany(rule => rule.Targets).All(target => ReferenceEquals(target, wrapper)),
                "all product rules including direct App NLog calls route through async wrapper");
            Program.Check(wrapper.WrappedTarget is FileTarget, "async wrapper retains a file target");
            var file = (FileTarget)wrapper.WrappedTarget!;
            file.FileName = logPath;
            file.Layout = "${message}";
            factory.Configuration = configuration;
            var logger = factory.GetLogger("synthetic-offline");
            string[] expected = Enumerable.Range(1, 501).Select(index => "SYNTHETIC_FILE_" + index).ToArray();
            foreach (string line in expected) logger.Info(line);
            logger.Warn("SYNTHETIC_FINAL_WARNING");
            logger.Error("SYNTHETIC_FINAL_ERROR");
            factory.Flush(TimeSpan.FromSeconds(3));
            var reloaded = new XmlLoggingConfiguration(Path.Combine(AppContext.BaseDirectory, "NLog.config"), factory);
            var replacement = reloaded.AllTargets.OfType<AsyncTargetWrapper>().Single();
            var replacementFile = (FileTarget)replacement.WrappedTarget!;
            replacementFile.FileName = logPath;
            replacementFile.Layout = "${message}";
            factory.Configuration = reloaded;
            logger.Info("SYNTHETIC_XML_RELOAD");
            var stopped = lifetime.Stop(TimeSpan.FromSeconds(3));
            Program.Check(stopped.Completed && stopped.Flushed, "file lifetime flush and shutdown finish within budget");
            Program.Check(stopped.Statistics.Submitted == 504 && stopped.Statistics.Written == 504 && stopped.Statistics.Pending == 0,
                "factory registration retains shared file counters when XML configuration is reloaded");
            Program.Check(File.ReadAllLines(logPath).SequenceEqual(expected.Concat(new[] { "SYNTHETIC_FINAL_WARNING", "SYNTHETIC_FINAL_ERROR", "SYNTHETIC_XML_RELOAD" })),
                "real temp file flush preserves 501 ordered messages and final Warn/Error");
        }
        finally
        {
            factory.Shutdown();
            if (File.Exists(logPath)) File.Delete(logPath);
            Directory.Delete(directory);
        }
    }

    private static async Task CheckFileBackpressure()
    {
        using var factory = new LogFactory();
        using var target = new GatedTarget();
        var lifetime = new LoggingLifetime(factory, _ => { });
        var wrapper = lifetime.CreateFileTarget(target, "synthetic-async");
        wrapper.QueueLimit = 8;
        wrapper.OverflowAction = AsyncTargetWrapperOverflowAction.Discard;
        wrapper.BatchSize = 1;
        wrapper.TimeToSleepBetweenBatches = 1;
        int dropped = 0;
        wrapper.LogEventDropped += (_, _) => Interlocked.Increment(ref dropped);
        var configuration = new LoggingConfiguration();
        configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, wrapper);
        factory.Configuration = configuration;
        try
        {
            var logger = factory.GetLogger("synthetic-slow-file");
            logger.Info("SYNTHETIC_GATE");
            Program.Check(target.Entered.Wait(TimeSpan.FromSeconds(3)), "fake file target entered its controlled blocking write");
            var producer = Task.Run(() =>
            {
                for (int index = 0; index < 501; index++) logger.Info("SYNTHETIC_" + index);
            });
            Program.Check(await Task.WhenAny(producer, Task.Delay(2000)) == producer,
                "501 file log producers finish while slow file target remains blocked");
            await producer;
            Program.Check(dropped == 493, "bounded NLog queue reports exactly the excess 493 events");
            Program.Check(target.Messages.Count == 0, "producer completion does not depend on a file write finishing");
            Program.Check(lifetime.GetStatistics().Dropped == 493, "file lifetime exposes overflow without logging recursively");
            int debugBatches = 0;
            using var debug = new AsyncDebugWriter(_ => Interlocked.Increment(ref debugBatches));
            debug.TryWrite("SYNTHETIC_INDEPENDENT_DEBUG");
            Program.Check(debug.Stop(TimeSpan.FromSeconds(2)).Written == 1 && debugBatches == 1,
                "blocked file output does not stop the independent debug consumer");
        }
        finally
        {
            target.Release.Set();
            factory.Flush(TimeSpan.FromSeconds(3));
            factory.Shutdown();
        }
        Program.Check(target.Messages.Count == 9, "gate plus eight queued file messages finish after release");
        var statistics = lifetime.GetStatistics();
        Program.Check(statistics.Submitted == 502 && statistics.Written == 9 && statistics.Pending == 0
            && statistics.Failed == 0 && statistics.Dropped == 493, "file accounting distinguishes actual writes, dropped events, and pending entries");
        lifetime.Stop(TimeSpan.FromSeconds(3));
    }

    private static void CheckFileFailureAndReload()
    {
        using var factory = new LogFactory();
        var lifetime = new LoggingLifetime(factory, _ => { }, TimeSpan.FromMilliseconds(20));
        var failing = lifetime.CreateFileTarget(new FailingTarget(), "synthetic-failing");
        var configuration = new LoggingConfiguration();
        configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, failing);
        factory.Configuration = configuration;
        var logger = factory.GetLogger("synthetic-reload");
        logger.Error("SYNTHETIC_FAILURE");
        factory.Flush(TimeSpan.FromSeconds(3));
        var afterFailure = lifetime.GetStatistics();
        Program.Check(afterFailure.Failed == 1 && afterFailure.Written == 0 && afterFailure.Pending == 0,
            "failed file write is counted and not propagated to auction caller");
        var memory = new MemoryTarget { Layout = "${message}" };
        var replacement = lifetime.CreateFileTarget(memory, "synthetic-replacement");
        configuration = new LoggingConfiguration();
        configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, replacement);
        factory.Configuration = configuration;
        logger.Info("SYNTHETIC_AFTER_RELOAD");
        factory.Flush(TimeSpan.FromSeconds(3));
        var statistics = lifetime.GetStatistics();
        Program.Check(memory.Logs.SequenceEqual(new[] { "SYNTHETIC_AFTER_RELOAD" }) && statistics.Submitted == 2
            && statistics.Written == 1 && statistics.Failed == 1 && statistics.Pending == 0,
            "configuration replacement retains counters and does not duplicate writes or subscriptions");
        var stopped = lifetime.Stop(TimeSpan.FromSeconds(3));
        Program.Check(stopped.Completed && lifetime.Stop(TimeSpan.Zero).Completed,
            "file lifetime repeated stop completes without another shutdown worker");
    }

    private static void CheckFileShutdownTimeout()
    {
        using var factory = new LogFactory();
        using var target = new GatedTarget();
        var lifetime = new LoggingLifetime(factory, _ => { });
        var wrapper = lifetime.CreateFileTarget(target, "synthetic-shutdown");
        wrapper.BatchSize = 1;
        wrapper.TimeToSleepBetweenBatches = 1;
        var configuration = new LoggingConfiguration();
        configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, wrapper);
        factory.Configuration = configuration;
        var logger = factory.GetLogger("synthetic-shutdown");
        try
        {
            logger.Info("SYNTHETIC_GATE");
            Program.Check(target.Entered.Wait(TimeSpan.FromSeconds(3)), "file timeout test enters blocked target");
            logger.Info("SYNTHETIC_PENDING");
            var watch = Stopwatch.StartNew();
            var stopped = lifetime.Stop(TimeSpan.FromMilliseconds(30));
            Program.Check(watch.Elapsed < TimeSpan.FromSeconds(1) && !stopped.Completed && stopped.Statistics.Pending > 0,
                "file flush and close cannot make caller exceed shutdown budget while disk is blocked");
            logger.Info("SYNTHETIC_LATE");
            Program.Check(lifetime.GetStatistics().RejectedAfterStop >= 1, "file admission closes before asynchronous flush completes");
        }
        finally
        {
            target.Release.Set();
            lifetime.Stop(TimeSpan.FromSeconds(3));
        }
        Program.Check(!target.Messages.Contains("SYNTHETIC_LATE"), "late file events are not written after admission closes");
    }

    private static Task MeasureOutput()
    {
        const int count = 501;
        string[] messages = Enumerable.Range(0, count).Select(index => $"SV|SYNTHETIC|{index}|||612|").ToArray();
        int calls = 0;
        void Sink(string _) { Thread.Sleep(1); Interlocked.Increment(ref calls); }
        Sink("WARMUP");
        calls = 0;
        var watch = Stopwatch.StartNew();
        foreach (string message in messages) Sink(message);
        double synchronous = watch.Elapsed.TotalMilliseconds;
        int synchronousCalls = calls;
        calls = 0;
        using var writer = new AsyncDebugWriter(Sink);
        long allocationBefore = GC.GetAllocatedBytesForCurrentThread();
        watch.Restart();
        foreach (string message in messages) writer.TryWrite(message, "Raw");
        double enqueue = watch.Elapsed.TotalMilliseconds;
        long producerAllocation = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
        var stats = writer.Stop(TimeSpan.FromSeconds(3));
        Program.Check(stats.Written == count && stats.Dropped == 0, "synthetic comparison drains every message");
        Console.WriteLine($"Logging synthetic comparison: input={count}; synchronous_producer_ms={synchronous:F2}; synchronous_sink_calls={synchronousCalls}; async_producer_ms={enqueue:F2}; async_drain_ms={watch.Elapsed.TotalMilliseconds:F2}; async_sink_calls={calls}; producer_allocated_bytes={producerAllocation}; peak_queue={stats.PeakCount}; peak_characters={stats.PeakCharacters}; max_queue_ms={stats.MaximumQueueWaitMilliseconds:F2}; debugger={Debugger.IsAttached}; fake sink requests 1 ms sleep per call (OS timer granularity applies); no real VS output, disk, Netty or rendering.");
        foreach (var mode in new[] { (Debug: false, File: false), (Debug: true, File: false), (Debug: false, File: true), (Debug: true, File: true) })
        {
            using var factory = new LogFactory();
            using var target = new DelayTarget();
            var lifetime = new LoggingLifetime(factory);
            var configuration = new LoggingConfiguration();
            if (mode.File) configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, lifetime.CreateFileTarget(target));
            factory.Configuration = configuration;
            var logger = factory.GetLogger("synthetic-measurement");
            using var debug = new AsyncDebugWriter(_ => Thread.Sleep(1));
            allocationBefore = GC.GetAllocatedBytesForCurrentThread();
            watch.Restart();
            foreach (string message in messages)
            {
                if (mode.Debug) debug.TryWrite(message, "Raw");
                if (mode.File) logger.Info("SYNTHETIC_FILE_EVENT");
            }
            enqueue = watch.Elapsed.TotalMilliseconds;
            producerAllocation = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
            var debugStatistics = debug.Stop(TimeSpan.FromSeconds(3));
            var fileStopped = lifetime.Stop(TimeSpan.FromSeconds(3));
            Program.Check(fileStopped.Completed && fileStopped.Statistics.Dropped == 0 && debugStatistics.Dropped == 0,
                "synthetic debug/file option matrix drains without losses");
            Program.Check(fileStopped.Statistics.Written + fileStopped.Statistics.Failed + fileStopped.Statistics.Dropped
                + fileStopped.Statistics.Pending == fileStopped.Statistics.Submitted, "file counters balance every submitted event");
            Console.WriteLine($"Logging synthetic modes: debug={mode.Debug}; file={mode.File}; producer_ms={enqueue:F2}; total_drain_ms={watch.Elapsed.TotalMilliseconds:F2}; producer_allocated_bytes={producerAllocation}; debug_peak={debugStatistics.PeakCount}; file_peak={fileStopped.Statistics.MaximumPending}; file_written={fileStopped.Statistics.Written}; fake sinks request 1 ms sleep per batch; debugger={Debugger.IsAttached}; no real disk or rendering.");
        }
        return Task.CompletedTask;
    }

    private static IEnumerable<string> Lines(string batch) => batch.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    private static string Raw(string line) => line[(line.IndexOf("] ", line.IndexOf("] [#", StringComparison.Ordinal) + 4, StringComparison.Ordinal) + 2)..]
        .Split("] ", 2, StringSplitOptions.None)[1];
    private static long Sequence(string line) => long.Parse(Regex.Match(line, @"\[#(\d+)\]").Groups[1].Value, CultureInfo.InvariantCulture);

    private sealed class GatedTarget : Target
    {
        internal readonly ManualResetEventSlim Entered = new(false);
        internal readonly ManualResetEventSlim Release = new(false);
        internal readonly ConcurrentQueue<string> Messages = new();

        protected override void Write(LogEventInfo logEvent)
        {
            Entered.Set();
            if (!Release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Synthetic gate was not released.");
            Messages.Enqueue(logEvent.FormattedMessage);
        }
    }

    private sealed class FailingTarget : Target
    {
        protected override void Write(LogEventInfo logEvent) => throw new IOException("Synthetic file failure");
    }

    private sealed class DelayTarget : Target
    {
        protected override void Write(LogEventInfo logEvent) => Thread.Sleep(1);

        protected override void Write(IList<NLog.Common.AsyncLogEventInfo> logEvents)
        {
            Thread.Sleep(1);
            foreach (var entry in logEvents) entry.Continuation(null);
        }
    }
}
