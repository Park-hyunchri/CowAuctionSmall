using NLog;
using NLog.Common;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CowAuctionSmall.Infra.Logging
{
    public sealed record FileLogStatistics(long Submitted, long Written, long Failed, long Dropped,
        long Pending, long MaximumPending, double MaximumCompletionMilliseconds, long RejectedAfterStop);

    public sealed record LoggingStopResult(bool Completed, bool Flushed, FileLogStatistics Statistics);

    internal sealed class FileLogCounters
    {
        internal long Submitted, Written, Failed, Dropped, Pending, MaximumPending, MaximumCompletionTicks, Rejected;
        internal int Closed;

        internal static void Maximum(ref long target, long value)
        {
            long before;
            do
            {
                before = Interlocked.Read(ref target);
                if (before >= value) return;
            } while (Interlocked.CompareExchange(ref target, value, before) != before);
        }

        internal FileLogStatistics Snapshot() => new(Interlocked.Read(ref Submitted), Interlocked.Read(ref Written),
            Interlocked.Read(ref Failed), Interlocked.Read(ref Dropped), Math.Max(0, Interlocked.Read(ref Pending)),
            Interlocked.Read(ref MaximumPending), Interlocked.Read(ref MaximumCompletionTicks) * 1000d / Stopwatch.Frequency,
            Interlocked.Read(ref Rejected));
    }

    /// <summary>NLog의 큐·묶음 기록을 그대로 사용하면서 완료·실패·포화를 값으로만 집계한다.</summary>
    [Target("ObservedAsyncWrapper", IsWrapper = true)]
    public sealed class ObservedAsyncTarget : AsyncTargetWrapper
    {
        private readonly FileLogCounters _counters;

        public ObservedAsyncTarget() : this(new FileLogCounters()) { }

        internal ObservedAsyncTarget(FileLogCounters counters)
        {
            _counters = counters;
            LogEventDropped += (_, _) =>
            {
                Interlocked.Increment(ref _counters.Dropped);
                Interlocked.Decrement(ref _counters.Pending);
            };
        }

        public FileLogStatistics GetStatistics() => _counters.Snapshot();

        protected override void Write(AsyncLogEventInfo logEvent)
        {
            if (Volatile.Read(ref _counters.Closed) != 0)
            {
                Interlocked.Increment(ref _counters.Rejected);
                logEvent.Continuation(null);
                return;
            }

            Interlocked.Increment(ref _counters.Submitted);
            FileLogCounters.Maximum(ref _counters.MaximumPending, Interlocked.Increment(ref _counters.Pending));
            long started = Stopwatch.GetTimestamp();
            int completed = 0;
            void Finish(Exception? error)
            {
                if (Interlocked.Exchange(ref completed, 1) != 0) return;
                if (error == null) Interlocked.Increment(ref _counters.Written);
                else Interlocked.Increment(ref _counters.Failed);
                Interlocked.Decrement(ref _counters.Pending);
                FileLogCounters.Maximum(ref _counters.MaximumCompletionTicks, Stopwatch.GetTimestamp() - started);
                logEvent.Continuation(error);
            }

            try { base.Write(new AsyncLogEventInfo(logEvent.LogEvent, Finish)); }
            catch (Exception error) { Finish(error); }
        }
    }

    /// <summary>원문·예외 내용을 보관하지 않으며, 종료 시 파일 I/O를 호출 스레드에서 기다리는 시간을 제한한다.</summary>
    public sealed class LoggingLifetime : IDisposable
    {
        private readonly LogFactory _factory;
        private readonly FileLogCounters _counters = new();
        private readonly Action<string>? _report;
        private readonly Timer? _reportTimer;
        private readonly object _stopGate = new();
        private Thread? _stopThread;
        private long _reportedDropped, _reportedFailed;
        private int _reporting, _completed, _flushed;

        public LoggingLifetime(LogFactory factory, Action<string>? report = null, TimeSpan? reportInterval = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _report = report;
            // ProcessExit의 자동 Shutdown이 제한 시간 뒤에 다시 무제한 대기하지 않게 직접 소유한다.
            _factory.AutoShutdown = false;
            _factory.Setup().SetupExtensions(builder => builder.RegisterTarget(
                () => new ObservedAsyncTarget(_counters), "ObservedAsyncWrapper"));
            if (report != null)
            {
                var interval = reportInterval ?? TimeSpan.FromSeconds(5);
                if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(reportInterval));
                _reportTimer = new Timer(_ => ReportProblems(), null, interval, interval);
            }
        }

        public ObservedAsyncTarget CreateFileTarget(Target wrappedTarget, string name = "f") => new(_counters)
        {
            Name = name,
            WrappedTarget = wrappedTarget ?? throw new ArgumentNullException(nameof(wrappedTarget)),
            QueueLimit = 10000,
            BatchSize = 200,
            OverflowAction = AsyncTargetWrapperOverflowAction.Discard
        };

        public FileLogStatistics GetStatistics() => _counters.Snapshot();

        private void ReportProblems()
        {
            if (Volatile.Read(ref _counters.Closed) != 0 || Interlocked.Exchange(ref _reporting, 1) != 0) return;
            try
            {
                var stats = GetStatistics();
                if (stats.Dropped == _reportedDropped && stats.Failed == _reportedFailed) return;
                _reportedDropped = stats.Dropped;
                _reportedFailed = stats.Failed;
                if (Volatile.Read(ref _counters.Closed) == 0)
                    _report?.Invoke($"[파일 로그] 누락={stats.Dropped}, 기록 실패={stats.Failed}, 미완료={stats.Pending}");
            }
            catch { /* 진단 안내 실패를 다시 같은 로거에 기록하지 않는다. */ }
            finally { Volatile.Write(ref _reporting, 0); }
        }

        public LoggingStopResult Stop(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));
            long started = Stopwatch.GetTimestamp();
            Thread worker;
            lock (_stopGate)
            {
                if (_stopThread == null)
                {
                    Volatile.Write(ref _counters.Closed, 1);
                    _reportTimer?.Dispose();
                    _stopThread = new Thread(() => StopCore(started, timeout))
                    {
                        IsBackground = true,
                        Name = "Auction file log shutdown"
                    };
                    _stopThread.Start();
                }
                worker = _stopThread;
            }
            var remaining = Remaining(started, timeout);
            if (Thread.CurrentThread != worker && remaining > TimeSpan.Zero) worker.Join(remaining);
            return new LoggingStopResult(Volatile.Read(ref _completed) != 0, Volatile.Read(ref _flushed) != 0, GetStatistics());
        }

        private void StopCore(long started, TimeSpan timeout)
        {
            try
            {
                var flushed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _factory.Flush(error => flushed.TrySetResult(error == null), Remaining(started, timeout));
                if (flushed.Task.Wait(Remaining(started, timeout)) && flushed.Task.Result)
                    Volatile.Write(ref _flushed, 1);
            }
            catch { /* 실패 여부는 Flushed=false로 반환한다. 쓰기 실패 건수와 섞지 않는다. */ }
            finally
            {
                try
                {
                    _factory.Shutdown();
                    Volatile.Write(ref _completed, 1);
                }
                catch { /* Shutdown 완료를 보고하지 않는다. */ }
            }
        }

        private static TimeSpan Remaining(long started, TimeSpan timeout)
        {
            var left = timeout - Stopwatch.GetElapsedTime(started);
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }

        public void Dispose() => Stop(TimeSpan.FromSeconds(2));
    }
}
