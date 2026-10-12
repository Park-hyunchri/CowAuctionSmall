using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CowAuctionSmall.Application.Auction
{
    /// <summary>입력과 HTTP 완료의 상태 반영을 한 소비자에서 수행한다.</summary>
    public sealed class AuctionIngress : IDisposable
    {
        private sealed record StateWork(Action Apply, long Generation, long EnqueuedAt, bool IsResponse, bool Recovers, long ResyncVersion, TaskCompletionSource<bool>? Completion = null, Func<bool>? IsCurrent = null);
        private sealed record RequestWork(Func<CancellationToken, Task<Action?>> Request, long Generation, bool Recovers, string? Key, TaskCompletionSource<bool>? Completion = null, Func<bool>? IsCurrent = null);
        private readonly HashSet<string> _pendingKeys = new();
        private readonly Channel<StateWork> _states;
        private readonly Channel<RequestWork> _requests;
        private readonly CancellationTokenSource _stop = new();
        private readonly object _admission = new();
        private readonly Action<string>? _diagnostic;
        private readonly Task _stateWorker;
        private readonly Task _httpWorker;
        [ThreadStatic] private static AuctionIngress? _executing;
        [ThreadStatic] private static long _executingGeneration;
        [ThreadStatic] private static long _executingResyncVersion;
        private long _generation;
        private long _resyncVersion;
        private long _rejected;
        private long _stale;
        private long _processed;
        private long _coalesced;
        private long _maxDelayTicks;
        private int _needsResync;
        private int _stopped;
        private int _applying;
        private int _requesting;

        public AuctionIngress(int stateCapacity = 256, int requestCapacity = 16, Action<string>? diagnostic = null)
        {
            if (stateCapacity < 1 || requestCapacity < 1) throw new ArgumentOutOfRangeException(nameof(stateCapacity));
            _diagnostic = diagnostic;
            _states = Channel.CreateBounded<StateWork>(new BoundedChannelOptions(stateCapacity)
            {
                SingleReader = true, FullMode = BoundedChannelFullMode.Wait, AllowSynchronousContinuations = false
            });
            _requests = Channel.CreateBounded<RequestWork>(new BoundedChannelOptions(requestCapacity)
            {
                SingleReader = true, FullMode = BoundedChannelFullMode.Wait, AllowSynchronousContinuations = false
            });
            _stateWorker = Task.Run(ConsumeStatesAsync);
            _httpWorker = Task.Run(ConsumeRequestsAsync);
        }

        public bool IsConsumer => ReferenceEquals(_executing, this);
        public bool NeedsResync => Volatile.Read(ref _needsResync) != 0;
        public bool IsStopped => Volatile.Read(ref _stopped) != 0;
        public long Generation => Interlocked.Read(ref _generation);
        public long ResyncVersion => Interlocked.Read(ref _resyncVersion);
        public long CurrentResyncVersion => IsConsumer ? _executingResyncVersion : ResyncVersion;
        public long RejectedCount => Interlocked.Read(ref _rejected);
        public long StaleResponseCount => Interlocked.Read(ref _stale);
        public long ProcessedCount => Interlocked.Read(ref _processed);
        public long CoalescedCount => Interlocked.Read(ref _coalesced);
        public int StateDepth => _states.Reader.Count;
        public int RequestDepth => _requests.Reader.Count;
        public bool IsBusy => Volatile.Read(ref _applying) != 0 || Volatile.Read(ref _requesting) != 0 || StateDepth > 0 || RequestDepth > 0;
        public TimeSpan MaximumQueueDelay => TimeSpan.FromSeconds((double)Interlocked.Read(ref _maxDelayTicks) / Stopwatch.Frequency);
        public Task Completion => Task.WhenAll(_stateWorker, _httpWorker);

        public bool TryPost(Action action, bool invalidates = false)
        {
            ArgumentNullException.ThrowIfNull(action);
            lock (_admission)
            {
                if (IsStopped) return false;
                long generation = invalidates ? ++_generation : _generation;
                if (_states.Writer.TryWrite(new StateWork(action, generation, Stopwatch.GetTimestamp(), false, false, _resyncVersion))) return true;
                Reject("state");
                return false;
            }
        }

        // 같은 입력의 messenger 전달은 다시 큐 끝으로 보내지 않는다.
        public bool Dispatch(Action action, bool invalidates = false)
        {
            if (!IsConsumer) return TryPost(action, invalidates);
            if (IsStopped) return false;
            action();
            return true;
        }

        public bool TryRequest(Func<CancellationToken, Task<Action?>> request, bool recovers = false, string? key = null, Func<bool>? isCurrent = null)
            => TryRequestCore(request, recovers, key, null, isCurrent);

        public Task<bool> RequestAsync(Func<CancellationToken, Task<Action?>> request, bool recovers = false)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!TryRequestCore(request, recovers, null, completion)) completion.TrySetResult(false);
            return completion.Task;
        }

        private bool TryRequestCore(Func<CancellationToken, Task<Action?>> request, bool recovers, string? key, TaskCompletionSource<bool>? completion, Func<bool>? isCurrent = null)
        {
            ArgumentNullException.ThrowIfNull(request);
            lock (_admission)
            {
                if (IsStopped) return false;
                if (key != null && _pendingKeys.Contains(key))
                {
                    Interlocked.Increment(ref _coalesced);
                    return true;
                }
                long generation = IsConsumer ? _executingGeneration : _generation;
                if (_requests.Writer.TryWrite(new RequestWork(request, generation, recovers, key, completion, isCurrent)))
                {
                    if (key != null) _pendingKeys.Add(key);
                    return true;
                }
                Reject("http");
                return false;
            }
        }

        public void InvalidateSession()
        {
            lock (_admission)
            {
                if (IsStopped) return;
                ++_generation;
                ++_resyncVersion;
                Volatile.Write(ref _needsResync, 1);
            }
        }

        private void Reject(string queue)
        {
            ++_generation;
            ++_resyncVersion;
            Interlocked.Increment(ref _rejected);
            Volatile.Write(ref _needsResync, 1);
            Report($"auction-sync-failed queue={queue}, rejected={RejectedCount}, coalesced={CoalescedCount}, state-depth={StateDepth}, http-depth={RequestDepth}; full refresh required");
        }

        private async Task ConsumeStatesAsync()
        {
            try
            {
                await foreach (var work in _states.Reader.ReadAllAsync(_stop.Token).ConfigureAwait(false))
                {
                    if (IsStopped) { work.Completion?.TrySetResult(false); break; }
                    if (work.IsResponse && work.Generation != Generation)
                    {
                        Interlocked.Increment(ref _stale);
                        work.Completion?.TrySetResult(false);
                        continue;
                    }
                    long delay = Stopwatch.GetTimestamp() - work.EnqueuedAt;
                    if (delay > Interlocked.Read(ref _maxDelayTicks)) Interlocked.Exchange(ref _maxDelayTicks, delay);
                    try
                    {
                        Volatile.Write(ref _applying, 1);
                        _executing = this;
                        _executingGeneration = work.Generation;
                        _executingResyncVersion = work.ResyncVersion;
                        if (work.IsCurrent != null && !work.IsCurrent())
                        {
                            Interlocked.Increment(ref _stale);
                            work.Completion?.TrySetResult(false);
                            continue;
                        }
                        work.Apply();
                        Interlocked.Increment(ref _processed);
                        if (work.Recovers && work.Generation == Generation)
                        {
                            bool recovered = Interlocked.Exchange(ref _needsResync, 0) != 0;
                            if (recovered) Report($"auction-sync-restored rejected={RejectedCount}, stale={StaleResponseCount}");
                        }
                        work.Completion?.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        InvalidateSession();
                        Report($"auction-state-failed type={ex.GetType().Name}; full refresh required");
                        work.Completion?.TrySetResult(false);
                    }
                    finally { _executing = null; Volatile.Write(ref _applying, 0); }
                }
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
            finally
            {
                while (_states.Reader.TryRead(out var pending)) pending.Completion?.TrySetResult(false);
            }
        }

        private async Task ConsumeRequestsAsync()
        {
            try
            {
                await foreach (var work in _requests.Reader.ReadAllAsync(_stop.Token).ConfigureAwait(false))
                {
                    if (IsStopped) { work.Completion?.TrySetResult(false); break; }
                    bool responseQueued = false;
                    try
                    {
                        Volatile.Write(ref _requesting, 1);
                        if (work.Generation != Generation)
                        {
                            Interlocked.Increment(ref _stale);
                            continue;
                        }
                        var apply = await work.Request(_stop.Token).WaitAsync(_stop.Token).ConfigureAwait(false);
                        if (apply == null || IsStopped) continue;
                        lock (_admission)
                        {
                            if (IsStopped) continue;
                            if (work.Generation != _generation) { Interlocked.Increment(ref _stale); continue; }
                            responseQueued = _states.Writer.TryWrite(new StateWork(apply, work.Generation, Stopwatch.GetTimestamp(), true, work.Recovers, _resyncVersion, work.Completion, work.IsCurrent));
                            if (!responseQueued) Reject("response");
                        }
                    }
                    catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
                    catch (Exception ex) { Report($"auction-http-failed type={ex.GetType().Name}"); }
                    finally
                    {
                        Volatile.Write(ref _requesting, 0);
                        if (work.Key != null) lock (_admission) _pendingKeys.Remove(work.Key);
                        if (!responseQueued) work.Completion?.TrySetResult(false);
                    }
                }
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
            finally
            {
                while (_requests.Reader.TryRead(out var pending)) pending.Completion?.TrySetResult(false);
            }
        }

        private void Report(string text)
        {
            try { _diagnostic?.Invoke(text); } catch { /* 진단 실패로 소비 루프를 중단하지 않는다. */ }
        }

        public void Dispose()
        {
            lock (_admission)
            {
                if (Interlocked.Exchange(ref _stopped, 1) != 0) return;
                ++_generation;
                _states.Writer.TryComplete();
                _requests.Writer.TryComplete();
                _stop.Cancel();
            }
            _ = Completion.ContinueWith(_ => _stop.Dispose(), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
