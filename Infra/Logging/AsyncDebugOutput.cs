using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace CowAuctionSmall.Infra.Logging
{
    public sealed record DebugOutputStatistics(long Accepted, long Written, long Dropped,
        long RejectedAfterStop, long Failed, long OutputCalls, int PendingCount,
        int PendingCharacters, int PeakCount, int PeakCharacters, long LastSequence,
        long FirstDroppedSequence, long LastDroppedSequence, long UnwrittenAtStop,
        double MaximumQueueWaitMilliseconds, double OutputMilliseconds, bool Stopped);

    // The sink is called only by this dedicated background thread, never under the queue lock.
    public sealed class AsyncDebugWriter : IDisposable
    {
        private sealed record Entry(long Sequence, DateTimeOffset OccurredAt, long EnqueuedAt,
            string Category, string Message);

        private readonly object _gate = new object();
        private readonly Queue<Entry> _queue = new Queue<Entry>();
        private readonly Action<string> _sink;
        private readonly Thread _thread;
        private readonly int _capacity, _characterCapacity, _batchSize, _batchCharacters;
        private readonly TimeSpan _batchInterval;
        private long _accepted, _written, _dropped, _rejected, _failed, _outputCalls, _sequence;
        private long _firstDropped, _lastDropped, _reportedDropped, _reportedFailed, _unwrittenAtStop;
        private int _characters, _peakCount, _peakCharacters, _inFlight;
        private double _maximumQueueWait, _outputMilliseconds;
        private bool _closing, _abort, _stopped;

        public AsyncDebugWriter(Action<string> sink, int capacity = 2048,
            int characterCapacity = 4000000, int batchSize = 128, int batchCharacters = 65536,
            TimeSpan? batchInterval = null)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            if (capacity <= 0 || characterCapacity <= 0 || batchSize <= 0 || batchCharacters <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            _batchInterval = batchInterval ?? TimeSpan.FromMilliseconds(50);
            if (_batchInterval < TimeSpan.Zero || _batchInterval.TotalMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(batchInterval));
            _capacity = capacity;
            _characterCapacity = characterCapacity;
            _batchSize = batchSize;
            _batchCharacters = batchCharacters;
            _thread = new Thread(Consume) { IsBackground = true, Name = "Auction Debug output" };
            _thread.Start();
        }

        public bool TryWrite(string? message, string category = "Debug", DateTimeOffset? occurredAt = null)
        {
            DateTimeOffset timestamp = occurredAt ?? DateTimeOffset.Now;
            long enqueuedAt = Stopwatch.GetTimestamp();
            message ??= string.Empty;
            lock (_gate)
            {
                if (_closing) { _rejected++; return false; }
                long sequence = ++_sequence;
                int length = message.Length + category.Length;
                if (_queue.Count >= _capacity || length > _characterCapacity - _characters)
                {
                    _dropped++;
                    if (_firstDropped == 0) _firstDropped = sequence;
                    _lastDropped = sequence;
                    Monitor.Pulse(_gate);
                    return false;
                }
                _queue.Enqueue(new Entry(sequence, timestamp, enqueuedAt, category, message));
                _accepted++;
                _characters += length;
                _peakCount = Math.Max(_peakCount, _queue.Count);
                _peakCharacters = Math.Max(_peakCharacters, _characters);
                Monitor.Pulse(_gate);
                return true;
            }
        }

        public DebugOutputStatistics GetStatistics()
        {
            lock (_gate)
                return new DebugOutputStatistics(_accepted, _written, _dropped, _rejected, _failed,
                    _outputCalls, _queue.Count + _inFlight, _characters, _peakCount, _peakCharacters,
                    _sequence, _firstDropped, _lastDropped, _unwrittenAtStop, _maximumQueueWait,
                    _outputMilliseconds, _stopped);
        }

        public DebugOutputStatistics Stop(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));
            lock (_gate)
            {
                _closing = true;
                Monitor.PulseAll(_gate);
            }
            if (Thread.CurrentThread != _thread && !_thread.Join(timeout))
            {
                lock (_gate)
                {
                    _abort = true;
                    _unwrittenAtStop = Math.Max(_unwrittenAtStop, _queue.Count + _inFlight);
                    _queue.Clear();
                    _characters = 0;
                    Monitor.PulseAll(_gate);
                }
            }
            return GetStatistics();
        }

        public void Dispose() => Stop(TimeSpan.FromSeconds(1));

        private void Consume()
        {
            try
            {
                while (true)
                {
                    List<Entry> batch = new List<Entry>(_batchSize);
                    long dropped, failed, firstDropped, lastDropped;
                    lock (_gate)
                    {
                        while (!_closing && _queue.Count == 0 && _dropped == _reportedDropped)
                            Monitor.Wait(_gate);
                        if (_abort || (_closing && _queue.Count == 0 && _dropped == _reportedDropped)) return;

                        // The first queued entry fixes the deadline; subsequent writes cannot postpone it.
                        while (!_closing && _queue.Count > 0 && _queue.Count < _batchSize && _characters < _batchCharacters)
                        {
                            TimeSpan remaining = _batchInterval - Stopwatch.GetElapsedTime(_queue.Peek().EnqueuedAt);
                            if (remaining <= TimeSpan.Zero) break;
                            Monitor.Wait(_gate, remaining);
                        }
                        if (_abort) return;
                        int length = 0;
                        while (_queue.Count > 0 && batch.Count < _batchSize)
                        {
                            Entry next = _queue.Peek();
                            int nextLength = next.Message.Length + next.Category.Length;
                            if (batch.Count > 0 && length + nextLength > _batchCharacters) break;
                            batch.Add(_queue.Dequeue());
                            length += nextLength;
                            _characters -= nextLength;
                            _maximumQueueWait = Math.Max(_maximumQueueWait,
                                Stopwatch.GetElapsedTime(next.EnqueuedAt).TotalMilliseconds);
                        }
                        _inFlight = batch.Count;
                        dropped = _dropped;
                        failed = _failed;
                        firstDropped = _firstDropped;
                        lastDropped = _lastDropped;
                        _outputCalls++;
                    }

                    StringBuilder output = new StringBuilder();
                    if (dropped != _reportedDropped || failed != _reportedFailed)
                        output.AppendLine($"[Debug output] dropped={dropped}, dropped-sequence-range={firstDropped}..{lastDropped}, failed={failed}");
                    foreach (Entry entry in batch)
                        output.Append('[').Append(entry.OccurredAt.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture))
                            .Append("] [#").Append(entry.Sequence).Append("] [").Append(entry.Category)
                            .Append("] ").Append(entry.Message).AppendLine();
                    long started = Stopwatch.GetTimestamp();
                    bool success = false;
                    try { _sink(output.ToString()); success = true; }
                    catch (Exception) { /* A broken debugger sink must not terminate auction processing. */ }
                    lock (_gate)
                    {
                        _outputMilliseconds += Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                        if (success) _written += batch.Count;
                        else _failed += batch.Count;
                        // A failed summary is not retried in a busy loop; later input carries updated counters.
                        _reportedDropped = dropped;
                        _reportedFailed = failed;
                        _inFlight = 0;
                    }
                }
            }
            finally { lock (_gate) _stopped = true; }
        }
    }

    [InterpolatedStringHandler]
    public ref struct DebugOutputInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;
        private readonly bool _enabled;

        public DebugOutputInterpolatedStringHandler(int literalLength, int formattedCount, out bool shouldAppend)
        {
            shouldAppend = AsyncDebugOutput.Enabled;
            _enabled = shouldAppend;
            _builder = shouldAppend ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
        }

        public void AppendLiteral(string value) => _builder.AppendLiteral(value);
        public void AppendFormatted<T>(T value) => _builder.AppendFormatted(value);
        public void AppendFormatted<T>(T value, string? format) => _builder.AppendFormatted(value, format);
        public void AppendFormatted<T>(T value, int alignment) => _builder.AppendFormatted(value, alignment);
        public void AppendFormatted<T>(T value, int alignment, string? format) => _builder.AppendFormatted(value, alignment, format);
        public string GetFormattedText() => _enabled ? _builder.ToStringAndClear() : string.Empty;
    }

    [InterpolatedStringHandler]
    public ref struct DiagnosticOutputInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;
        private readonly bool _enabled;

        public DiagnosticOutputInterpolatedStringHandler(int literalLength, int formattedCount, out bool shouldAppend)
        {
            shouldAppend = AsyncDebugOutput.DiagnosticsEnabled;
            _enabled = shouldAppend;
            _builder = shouldAppend ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
        }

        public void AppendLiteral(string value) => _builder.AppendLiteral(value);
        public void AppendFormatted<T>(T value) => _builder.AppendFormatted(value);
        public void AppendFormatted<T>(T value, string? format) => _builder.AppendFormatted(value, format);
        public void AppendFormatted<T>(T value, int alignment) => _builder.AppendFormatted(value, alignment);
        public void AppendFormatted<T>(T value, int alignment, string? format) => _builder.AppendFormatted(value, alignment, format);
        public string GetFormattedText() => _enabled ? _builder.ToStringAndClear() : string.Empty;
    }

    public static class AsyncDebugOutput
    {
        private static readonly object Gate = new object();
        private static AsyncDebugWriter? _writer;
        private static bool _stopped;
        private static volatile bool _enabled = Environment.GetEnvironmentVariable("COWAUCTION_DEBUG_OUTPUT") != "0";
        private static volatile bool _diagnosticsEnabled = Environment.GetEnvironmentVariable("COWAUCTION_DIAGNOSTICS") == "1";

        public static bool Enabled { get => _enabled; set => _enabled = value; }
        public static bool DiagnosticsEnabled { get => _enabled && _diagnosticsEnabled; set => _diagnosticsEnabled = value; }

        [Conditional("DEBUG")]
        public static void Initialize()
        {
            if (!Enabled) return;
            lock (Gate)
                if (!_stopped && _writer == null)
                    _writer = new AsyncDebugWriter(text => Debug.Write(text));
        }

        [Conditional("DEBUG")]
        public static void WriteLine(string? message)
        {
            if (!Enabled) return;
            Initialize();
            _writer?.TryWrite(message);
        }

        [Conditional("DEBUG")]
        public static void WriteLine(ref DebugOutputInterpolatedStringHandler message)
        {
            string text = message.GetFormattedText();
            if (Enabled) WriteLine(text);
        }

        [Conditional("DEBUG")]
        public static void WriteLine(string format, params object?[] args)
        {
            if (Enabled) WriteLine(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        [Conditional("DEBUG")]
        public static void WriteRaw(string message)
        {
            if (!Enabled) return;
            DateTimeOffset occurredAt = DateTimeOffset.Now;
            Initialize();
            _writer?.TryWrite(message, "Netty 수신", occurredAt);
        }

        [Conditional("DEBUG")]
        public static void WriteDiagnostic(string? message)
        {
            if (DiagnosticsEnabled) WriteLine(message);
        }

        [Conditional("DEBUG")]
        public static void WriteDiagnostic(ref DiagnosticOutputInterpolatedStringHandler message)
        {
            string text = message.GetFormattedText();
            if (DiagnosticsEnabled) WriteLine(text);
        }

        public static DebugOutputStatistics? GetStatistics() => _writer?.GetStatistics();

        public static DebugOutputStatistics? Stop(TimeSpan timeout)
        {
            AsyncDebugWriter? writer;
            lock (Gate) { _stopped = true; writer = _writer; }
            return writer?.Stop(timeout);
        }
    }
}
