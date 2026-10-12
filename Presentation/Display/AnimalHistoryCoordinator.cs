using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CowAuctionSmall.Domain.AnimalHistory;

namespace CowAuctionSmall.Presentation.Display
{
    /// <summary>Publishes each cached or retrieved history to its current display targets.</summary>
    public sealed class AnimalHistoryCoordinator : IDisposable
    {
        private sealed class Request
        {
            public required string Number { get; init; }
            public required string Epoch { get; init; }
            public required long Id { get; init; }
            public required CancellationTokenSource Stop { get; init; }
            public bool Refresh { get; set; }
            public bool Completed { get; set; }
            public bool ApplyFailed { get; set; }
            public HistoryLookupOutcome? Outcome { get; set; }
        }

        private readonly Func<string, bool, CancellationToken, Task<HistoryLookupResult>> _load;
        private readonly Func<string, Task<HistoryLookupResult>>? _cache;
        private readonly Func<string> _getEpoch;
        private readonly Action<Action> _dispatch;
        private readonly Action<HistoryItemUpdate> _onApplied;
        private readonly SemaphoreSlim _publicationSlots;
        private readonly object _gate = new();
        private readonly CancellationTokenSource _stop = new();
        private readonly CancellationToken _token;
        private readonly Channel<Request> _preparation = Channel.CreateUnbounded<Request>();
        private readonly Channel<Request> _pending = Channel.CreateUnbounded<Request>();
        private readonly Dictionary<string, Request> _requests = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HistoryLookupResult> _committed = new(StringComparer.Ordinal);
        private readonly Task _lifetime;
        private TaskCompletionSource _completion = CompletedSource();
        private HistoryBatchProgress _progress;
        private string _epoch;
        private long _version;
        private long _requestId;
        private int _notes;
        private bool _manualPending;
        private bool _progressQueued;
        private bool _disposed;

        public AnimalHistoryCoordinator(
            Func<string, bool, Task<(bool HasValue, string Note)>> load,
            Func<string> getEpoch, Action<Action> dispatch, Action<HistoryItemUpdate> onApplied)
            : this(async (number, refresh, _) =>
            {
                var value = await load(number, refresh).ConfigureAwait(false);
                return new HistoryLookupResult(value.HasValue, value.Note, value.HasValue ? HistoryLookupOutcome.Success : HistoryLookupOutcome.Failed);
            }, null, getEpoch, dispatch, onApplied, 1)
        {
        }

        public AnimalHistoryCoordinator(
            Func<string, bool, CancellationToken, Task<HistoryLookupResult>> load,
            Func<string, Task<HistoryLookupResult>>? cache,
            Func<string> getEpoch, Action<Action> dispatch, Action<HistoryItemUpdate> onApplied, int workers = 4)
        {
            _load = load;
            _cache = cache;
            _getEpoch = getEpoch;
            _dispatch = dispatch;
            _onApplied = onApplied;
            _publicationSlots = new SemaphoreSlim(Math.Clamp(workers, 1, 4) + 1);
            _epoch = getEpoch();
            _progress = new(0, _epoch, HistoryBatchStage.Empty);
            _token = _stop.Token;
            _lifetime = Task.WhenAll(Enumerable.Range(0, Math.Clamp(workers, 1, 4))
                .Select(_ => Task.Run(WorkerAsync)).Append(Task.Run(PrepareAsync)));
        }

        public event Action<HistoryBatchProgress>? ProgressChanged;
        public HistoryBatchProgress Progress { get { lock (_gate) return _progress; } }
        // Includes valid UI callbacks, not just the network requests.
        public Task Completion { get { lock (_gate) return _completion.Task; } }

        private static TaskCompletionSource CompletedSource()
        {
            var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            source.SetResult();
            return source;
        }

        public bool TryGetNote(string number, out string note)
        {
            bool found = TryGetHistory(number, out var history);
            note = history.Note;
            return found;
        }

        public bool TryGetHistory(string number, out HistoryLookupResult history)
        {
            lock (_gate)
            {
                history = HistoryLookupResult.Missing;
                if (_disposed || string.IsNullOrEmpty(number) || _epoch != _getEpoch() || !_committed.TryGetValue(number, out var committed)) return false;
                history = committed;
                return true;
            }
        }

        public bool IsCurrent(HistoryItemUpdate update)
        {
            lock (_gate)
                return !_disposed && update.Epoch == _epoch && _epoch == _getEpoch() &&
                    _requests.TryGetValue(update.Number, out var request) && request.Id == update.RequestId;
        }

        private bool IsCurrent(Request request)
        {
            lock (_gate)
                return !_disposed && request.Epoch == _epoch && _epoch == _getEpoch() &&
                    _requests.TryGetValue(request.Number, out var current) && ReferenceEquals(current, request);
        }

        public void UpdateTargets(IEnumerable<string> numbers, bool refresh = false)
        {
            var ordered = numbers.Where(number => number != null && number.Length == 15 &&
                number.All(c => c >= '0' && c <= '9')).Distinct(StringComparer.Ordinal).ToArray();
            var targets = ordered.ToHashSet(StringComparer.Ordinal);
            lock (_gate)
            {
                if (_disposed) return;
                string epoch = _getEpoch();
                if (_epoch != epoch) ResetCore(epoch);
                bool changed = !_requests.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(targets);
                bool startRefresh = refresh && !_manualPending;
                if (!changed && !startRefresh) return;
                foreach (string number in _requests.Keys.Where(number => !targets.Contains(number)).ToArray())
                {
                    Retire(_requests[number]);
                    _requests.Remove(number);
                    _committed.Remove(number);
                }
                _version++;
                if (startRefresh) _manualPending = true;
                foreach (string number in ordered)
                {
                    if (_requests.TryGetValue(number, out var current))
                    {
                        if (!startRefresh) continue;
                        if (!current.Completed)
                        {
                            // The automatic load is retained; its worker follows it with refresh semantics.
                            current.Refresh = true;
                            continue;
                        }
                        Retire(current);
                    }
                    var request = new Request
                    {
                        Number = number, Epoch = _epoch, Id = ++_requestId,
                        Stop = CancellationTokenSource.CreateLinkedTokenSource(_token), Refresh = startRefresh
                    };
                    _requests[number] = request;
                    _preparation.Writer.TryWrite(request);
                }
                if (_requests.Values.Any(request => !request.Completed) && _completion.Task.IsCompleted)
                    _completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                RecountProgress();
            }
        }

        private async Task PrepareAsync()
        {
            try
            {
                await foreach (var request in _preparation.Reader.ReadAllAsync(_token).ConfigureAwait(false))
                {
                    if (!IsCurrent(request)) { request.Stop.Dispose(); continue; }
                    bool queued = false;
                    try
                    {
                        HistoryLookupResult cached;
                        lock (_gate) cached = _committed.GetValueOrDefault(request.Number, HistoryLookupResult.Missing);
                        if (!cached.HasValue && _cache != null)
                            cached = await _cache(request.Number).WaitAsync(request.Stop.Token).ConfigureAwait(false);
                        if (cached.HasValue && !await ApplyAsync(request, cached).ConfigureAwait(false)) continue;
                        lock (_gate)
                        {
                            if (!IsCurrent(request)) continue;
                            if (cached.HasValue && !request.Refresh)
                            {
                                Finish(request, HistoryLookupOutcome.Cache);
                                continue;
                            }
                            queued = _pending.Writer.TryWrite(request);
                        }
                    }
                    catch (OperationCanceledException) when (request.Stop.IsCancellationRequested) { }
                    catch (Exception)
                    {
                        // A cache read failure can still be recovered by the normal lookup service.
                        if (IsCurrent(request)) queued = _pending.Writer.TryWrite(request);
                    }
                    finally
                    {
                        if (!queued)
                        {
                            Finish(request, HistoryLookupOutcome.Failed);
                            DisposeRetired(request);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (_token.IsCancellationRequested) { }
        }

        private async Task WorkerAsync()
        {
            try
            {
                await foreach (var request in _pending.Reader.ReadAllAsync(_token).ConfigureAwait(false))
                {
                    var outcome = HistoryLookupOutcome.Failed;
                    try
                    {
                        while (IsCurrent(request))
                        {
                            bool refresh;
                            lock (_gate) refresh = request.Refresh;
                            outcome = HistoryLookupOutcome.Failed;
                            var result = await _load(request.Number, refresh, request.Stop.Token)
                                .WaitAsync(request.Stop.Token).ConfigureAwait(false);
                            if (!IsCurrent(request)) break;
                            if (result.HasValue && !await ApplyAsync(request, result).ConfigureAwait(false)) break;
                            outcome = result.Outcome;
                            lock (_gate)
                            {
                                if (!refresh && request.Refresh) continue;
                                Finish(request, outcome);
                                break;
                            }
                        }
                    }
                    catch (OperationCanceledException) when (request.Stop.IsCancellationRequested) { }
                    catch (Exception) { }
                    finally
                    {
                        Finish(request, outcome);
                        DisposeRetired(request);
                    }
                }
            }
            catch (OperationCanceledException) when (_token.IsCancellationRequested) { }
        }

        private async Task<bool> ApplyAsync(Request request, HistoryLookupResult result)
        {
            if (!IsCurrent(request)) return false;
            await _publicationSlots.WaitAsync(request.Stop.Token).ConfigureAwait(false);
            if (!IsCurrent(request)) { _publicationSlots.Release(); return false; }
            var applied = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var update = new HistoryItemUpdate(request.Number, request.Epoch, request.Id, result);
            int released = 0;
            void ReleasePublication()
            {
                if (Interlocked.Exchange(ref released, 1) == 0) _publicationSlots.Release();
            }
            try
            {
                _dispatch(() =>
                {
                    try
                    {
                        HistoryLookupResult? previous;
                        lock (_gate)
                        {
                            if (!IsCurrent(update)) { applied.TrySetResult(false); return; }
                            _committed.TryGetValue(request.Number, out previous);
                            _committed[request.Number] = result;
                            _notes += NoteCount(result) - NoteCount(previous);
                        }
                        try
                        {
                            _onApplied(update);
                            applied.TrySetResult(true);
                        }
                        catch (Exception)
                        {
                            lock (_gate)
                            {
                                if (IsCurrent(update))
                                {
                                    request.ApplyFailed = true;
                                    _notes += NoteCount(previous) - NoteCount(result);
                                    if (previous != null) _committed[request.Number] = previous;
                                    else _committed.Remove(request.Number);
                                }
                            }
                            applied.TrySetResult(false);
                        }
                    }
                    finally { ReleasePublication(); }
                });
            }
            catch (Exception)
            {
                ReleasePublication();
                lock (_gate) request.ApplyFailed = true;
                return false;
            }
            // Cancellation releases the producer, but the callback owns its slot until the UI dequeues it.
            return await applied.Task.WaitAsync(request.Stop.Token).ConfigureAwait(false);
        }

        private void Finish(Request request, HistoryLookupOutcome outcome)
        {
            lock (_gate)
            {
                if (!_requests.TryGetValue(request.Number, out var current) || !ReferenceEquals(current, request) || request.Completed) return;
                request.Completed = true;
                request.Outcome = request.ApplyFailed ? HistoryLookupOutcome.Failed : outcome;
                var progress = request.Outcome switch
                {
                    HistoryLookupOutcome.Cache => _progress with { Cached = _progress.Cached + 1 },
                    HistoryLookupOutcome.Success => _progress with { Succeeded = _progress.Succeeded + 1 },
                    HistoryLookupOutcome.Stopped => _progress with { Stopped = _progress.Stopped + 1 },
                    _ => _progress with { Failed = _progress.Failed + 1 }
                };
                bool pending = progress.Processed < _requests.Count;
                var stage = request.ApplyFailed || _progress.Stage == HistoryBatchStage.Failed ? HistoryBatchStage.Failed :
                    pending ? HistoryBatchStage.Loading : HistoryBatchStage.Complete;
                SetProgress(progress with { Stage = stage, Notes = _notes });
                if (!pending)
                {
                    _manualPending = false;
                    _completion.TrySetResult();
                }
            }
        }

        private static int NoteCount(HistoryLookupResult? result) => string.IsNullOrWhiteSpace(result?.Note) ? 0 : 1;

        private static void Retire(Request request)
        {
            request.Stop.Cancel();
            if (request.Completed) request.Stop.Dispose();
        }

        private void DisposeRetired(Request request)
        {
            lock (_gate)
                if (!_requests.TryGetValue(request.Number, out var current) || !ReferenceEquals(current, request))
                    request.Stop.Dispose();
        }

        private void RecountProgress()
        {
            int cached = 0, succeeded = 0, failed = 0, stopped = 0;
            bool pending = false, applyFailed = false;
            foreach (var request in _requests.Values)
            {
                pending |= !request.Completed;
                applyFailed |= request.ApplyFailed;
                switch (request.Outcome)
                {
                    case HistoryLookupOutcome.Cache: cached++; break;
                    case HistoryLookupOutcome.Success: succeeded++; break;
                    case HistoryLookupOutcome.Failed: failed++; break;
                    case HistoryLookupOutcome.Stopped: stopped++; break;
                }
            }
            var stage = _requests.Count == 0 ? HistoryBatchStage.Empty : applyFailed ? HistoryBatchStage.Failed :
                pending ? HistoryBatchStage.Loading : HistoryBatchStage.Complete;
            _notes = _committed.Count(pair => !string.IsNullOrWhiteSpace(pair.Value.Note));
            SetProgress(new(_version, _epoch, stage, _requests.Count, cached, succeeded, failed, stopped, _notes));
            if (!pending)
            {
                _manualPending = false;
                _completion.TrySetResult();
            }
        }

        public void Reset()
        {
            lock (_gate) if (!_disposed) ResetCore(_getEpoch());
        }

        private void ResetCore(string epoch)
        {
            foreach (var request in _requests.Values) Retire(request);
            _requests.Clear();
            _committed.Clear();
            _notes = 0;
            _epoch = epoch;
            _version++;
            _manualPending = false;
            _completion.TrySetResult();
            SetProgress(new(_version, epoch, HistoryBatchStage.Empty));
        }

        private void SetProgress(HistoryBatchProgress progress)
        {
            var previousStage = _progress.Stage;
            _progress = progress;
            if (ProgressChanged == null || _disposed) return;
            bool terminal = progress.Stage is HistoryBatchStage.Empty or HistoryBatchStage.Complete ||
                progress.Stage == HistoryBatchStage.Failed && previousStage != HistoryBatchStage.Failed;
            if (_progressQueued) return;
            _progressQueued = true;
            if (terminal)
            {
                DispatchProgress();
                return;
            }
            _ = NotifyLaterAsync();
        }

        private void DispatchProgress()
        {
            try { _dispatch(() => { lock (_gate) _progressQueued = false; NotifyProgress(); }); }
            catch (Exception) { lock (_gate) _progressQueued = false; }
        }

        private async Task NotifyLaterAsync()
        {
            try
            {
                await Task.Delay(200, _token).ConfigureAwait(false);
                DispatchProgress();
            }
            catch (Exception) { lock (_gate) _progressQueued = false; }
        }

        private void NotifyProgress()
        {
            lock (_gate)
                if (!_disposed && _progress.Epoch == _getEpoch()) ProgressChanged?.Invoke(_progress);
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                ResetCore(_epoch);
            }
            _preparation.Writer.TryComplete();
            _pending.Writer.TryComplete();
            _stop.Cancel();
            _ = _lifetime.ContinueWith(_ =>
            {
                while (_preparation.Reader.TryRead(out var preparing)) preparing.Stop.Dispose();
                while (_pending.Reader.TryRead(out var pending)) pending.Stop.Dispose();
                _stop.Dispose();
            }, CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
