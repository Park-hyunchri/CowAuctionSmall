using CowAuctionSmall.Domain.Auction;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CowAuctionSmall.Application.Auction
{
    /// <summary>Only notifications are delayed; canonical state is already updated on the ingress consumer.</summary>
    public sealed class AuctionDisplayUpdateBatcher : IDisposable
    {
        private readonly Action<Action> _post;
        private readonly Action<IReadOnlyList<gValues>> _publish;
        private readonly Dictionary<string, gValues> _pending = new(StringComparer.Ordinal);
        private readonly TimeSpan _interval;
        private readonly object _gate = new();
        private Timer? _timer;
        private long _lastPublish;
        private long _epoch;
        private bool _disposed;

        public AuctionDisplayUpdateBatcher(Action<Action> post, Action<IReadOnlyList<gValues>> publish, TimeSpan? interval = null)
        {
            _post = post;
            _publish = publish;
            _interval = interval ?? TimeSpan.FromMilliseconds(30);
        }

        public void Add(IEnumerable<gValues> rows)
        {
            bool flush;
            lock (_gate)
            {
                if (_disposed) return;
                foreach (var item in rows) _pending[item.SpaceIndex] = item.Clone();
                if (_pending.Count == 0) return;
                TimeSpan elapsed = _lastPublish == 0 ? _interval : Stopwatch.GetElapsedTime(_lastPublish);
                flush = elapsed >= _interval;
                if (!flush && _timer != null) return;
                if (!flush)
                {
                    long epoch = _epoch;
                    _timer = new Timer(_ => _post(() =>
                    {
                        lock (_gate) if (_disposed || epoch != _epoch) return;
                        Flush();
                    }), null, _interval - elapsed, Timeout.InfiniteTimeSpan);
                }
            }
            if (flush) Flush();
        }

        public void Flush()
        {
            gValues[] rows;
            lock (_gate)
            {
                _timer?.Dispose();
                _timer = null;
                _epoch++;
                if (_disposed || _pending.Count == 0) return;
                rows = _pending.Values.ToArray();
                _pending.Clear();
                _lastPublish = Stopwatch.GetTimestamp();
            }
            _publish(rows);
        }

        public void Clear()
        {
            lock (_gate)
            {
                _timer?.Dispose();
                _timer = null;
                _epoch++;
                _pending.Clear();
                _lastPublish = 0;
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                _disposed = true;
                Clear();
            }
        }
    }
}
