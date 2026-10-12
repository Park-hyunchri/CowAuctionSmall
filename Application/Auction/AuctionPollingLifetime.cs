using System;
using System.Threading;
using System.Threading.Tasks;

namespace CowAuctionSmall.Application.Auction
{
    /// <summary>주기 작업의 시작·취소·완료를 소유한다. 동기 Dispose는 작업을 기다리지 않는다.</summary>
    public sealed class AuctionPollingLifetime : IDisposable
    {
        private readonly CancellationTokenSource _stop = new();
        private readonly CancellationToken _token;
        private readonly object _gate = new();
        private Task _completion = Task.CompletedTask;
        private bool _started;
        private bool _disposed;

        public AuctionPollingLifetime() => _token = _stop.Token;
        public CancellationToken Token => _token;
        public Task Completion { get { lock (_gate) return _completion; } }

        public Task StartAsync(Func<Task> run)
        {
            lock (_gate)
            {
                if (_disposed || _started) return _completion;
                _started = true;
                _completion = Task.Run(run);
                return _completion;
            }
        }

        public void Dispose()
        {
            Task completion;
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                _stop.Cancel();
                completion = _completion;
            }
            _ = completion.ContinueWith(task =>
            {
                _ = task.Exception;
                _stop.Dispose();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
