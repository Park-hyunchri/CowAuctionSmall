using CowAuctionSmall.Domain.Auction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CowAuctionSmall.Application.Auction
{
    public sealed record AuctionSectionContext(string HouseCode, DateOnly Date);

    /// <summary>구간 조회는 업무 HTTP 큐와 분리하고, 결과 반영만 순차 상태 큐에서 처리한다.</summary>
    public sealed class AuctionSectionLookup : IDisposable
    {
        private sealed record Key(AuctionSectionContext Context, int CowType, int SectionNumber);
        private sealed record Cached(AuctionSectionRange? Range, DateTimeOffset Expires);
        private readonly object _gate = new();
        private readonly AuctionIngress _ingress;
        private readonly Func<AuctionSectionContext?> _context;
        private readonly Func<DateOnly, int, int, CancellationToken, Task<AuctionSectionRange?>> _read;
        private readonly Action<AuctionSectionState> _publish;
        private readonly Func<DateTimeOffset> _now;
        private readonly CancellationToken _lifetime;
        private readonly SemaphoreSlim _slot = new(1);
        private readonly Dictionary<Key, Cached> _cache = new();
        private CancellationTokenSource? _pending;
        private Key? _pendingKey;
        private AuctionSectionState _selection = AuctionSectionState.Empty;
        private long _version;
        private bool _disposed;

        public AuctionSectionLookup(AuctionIngress ingress, Func<AuctionSectionContext?> context,
            Func<DateOnly, int, int, CancellationToken, Task<AuctionSectionRange?>> read,
            Action<AuctionSectionState> publish, CancellationToken lifetime = default, Func<DateTimeOffset>? now = null)
        {
            _ingress = ingress; _context = context; _read = read; _publish = publish;
            _lifetime = lifetime; _now = now ?? (() => DateTimeOffset.UtcNow);
        }

        public void Apply(AuctionSectionState state, bool refresh = false)
        {
            lock (_gate)
            {
                if (_disposed || _lifetime.IsCancellationRequested) return;
                _selection = state;
                if (refresh) { CancelPending(); _cache.Clear(); }
                var context = _context();
                if (!state.IsValid || context == null || state.HouseCode != context.HouseCode)
                {
                    CancelPending();
                    _publish(state);
                    return;
                }
                if (state.IsRangeConfirmed && state.AuctionDate.HasValue)
                {
                    CancelPending();
                    var supplied = new Key(new AuctionSectionContext(state.HouseCode, state.AuctionDate.Value),
                        state.CowType!.Value, state.SectionNumber!.Value);
                    Store(supplied, state.Range);
                    _publish(state);
                    return;
                }
                if (state.Source != "AS" || state.SectionNumber is not > 0)
                {
                    CancelPending();
                    _publish(state);
                    return;
                }
                var key = new Key(context, state.CowType!.Value, state.SectionNumber.Value);
                if (_cache.TryGetValue(key, out var cached) && cached.Expires > _now())
                {
                    CancelPending();
                    _publish(cached.Range == null ? state : state.ConfirmRange(context.Date, cached.Range));
                    return;
                }
                _publish(state);
                if (_pendingKey == key) return;
                CancelPending();
                _pendingKey = key;
                var request = CancellationTokenSource.CreateLinkedTokenSource(_lifetime);
                request.CancelAfter(TimeSpan.FromSeconds(10));
                _pending = request;
                long version = _version;
                _ = Task.Run(() => LoadAsync(key, version, request));
            }
        }

        private async Task LoadAsync(Key key, long version, CancellationTokenSource request)
        {
            AuctionSectionRange? result = null;
            bool entered = false;
            try
            {
                await _slot.WaitAsync(request.Token).ConfigureAwait(false);
                entered = true;
                result = await _read(key.Context.Date, key.CowType, key.SectionNumber, request.Token)
                    .WaitAsync(request.Token).ConfigureAwait(false);
                if (result != null && (result.StartNumber < 1 || result.EndNumber < result.StartNumber)) result = null;
            }
            catch (Exception) { /* 상태줄은 미확인을 유지하며 다음 신호에서 재시도할 수 있다. */ }
            finally
            {
                if (entered) _slot.Release();
                var range = result;
                if (!_ingress.TryPost(() => Complete(key, version, request, range)))
                {
                    lock (_gate)
                    {
                        if (ReferenceEquals(_pending, request)) { _pending = null; _pendingKey = null; }
                        request.Dispose();
                    }
                }
            }
        }

        private void Complete(Key key, long version, CancellationTokenSource request, AuctionSectionRange? range)
        {
            lock (_gate)
            {
                bool current = !_disposed && !_lifetime.IsCancellationRequested && version == _version && _pendingKey == key;
                if (ReferenceEquals(_pending, request)) { _pending = null; _pendingKey = null; }
                request.Dispose();
                if (!current) return;
                if (_context() != key.Context) return;
                Store(key, range);
                _publish(range == null ? _selection : _selection.ConfirmRange(key.Context.Date, range));
            }
        }

        private void Store(Key key, AuctionSectionRange? range)
        {
            // 세션 메모리만 사용하며 정상 결과는 5분, 실패는 30초 동안 재사용한다.
            if (_cache.Count >= 64) _cache.Clear();
            _cache[key] = new Cached(range, _now().Add(range == null ? TimeSpan.FromSeconds(30) : TimeSpan.FromMinutes(5)));
        }

        private void CancelPending()
        {
            _version++;
            _pendingKey = null;
            if (_pending != null)
            {
                _pending.Cancel();
                // 실행 중인 작업이 Token을 사용할 수 있으므로 여기서 Dispose하지 않는다.
                _pending = null;
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                CancelPending();
                _cache.Clear();
            }
        }
    }
}
