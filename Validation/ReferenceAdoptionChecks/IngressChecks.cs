using CowAuctionSmall.Application.Auction;

internal static class IngressChecks
{
    internal static async Task Run()
    {
        using (var ingress = new AuctionIngress())
        {
            bool applied = false;
            Program.Check(await ingress.RequestAsync(_ => Task.FromResult<Action?>(() => applied = true)).WaitAsync(TimeSpan.FromSeconds(5)) && applied,
                "RequestAsync succeeds only after state application");
            Program.Check(!await ingress.RequestAsync(_ => Task.FromResult<Action?>(null)).WaitAsync(TimeSpan.FromSeconds(5)),
                "null HTTP result completes RequestAsync as false");
            Program.Check(!await ingress.RequestAsync(_ => throw new InvalidOperationException("synthetic")).WaitAsync(TimeSpan.FromSeconds(5)),
                "HTTP failure completes RequestAsync as false");
            var started = Signal();
            var release = Signal();
            var stale = ingress.RequestAsync(async cancellation =>
            {
                started.TrySetResult(); await release.Task.WaitAsync(cancellation); return () => { };
            });
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            ingress.InvalidateSession(); release.TrySetResult();
            Program.Check(!await stale.WaitAsync(TimeSpan.FromSeconds(5)), "stale HTTP result completes RequestAsync as false");
            var neverCompletes = new TaskCompletionSource<Action?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var stopping = ingress.RequestAsync(_ => neverCompletes.Task);
            ingress.Dispose();
            Program.Check(!await stopping.WaitAsync(TimeSpan.FromSeconds(5)), "stopped HTTP request completes RequestAsync as false");
            await ingress.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            Program.Check(!await ingress.RequestAsync(_ => Task.FromResult<Action?>(() => { })), "RequestAsync after stop completes false");
        }
        using (var ingress = new AuctionIngress())
        {
            var seen = new List<int>();
            var done = Signal();
            for (int index = 0; index < 100; index++)
            {
                int value = index;
                Program.Check(ingress.TryPost(() => seen.Add(value)), "normal event accepted");
            }
            ingress.TryPost(() => done.TrySetResult());
            await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Program.Check(seen.SequenceEqual(Enumerable.Range(0, 100)), "accepted events preserve FIFO order");
            var afterFailure = Signal();
            ingress.TryPost(() => throw new InvalidOperationException("synthetic"));
            ingress.TryPost(() => afterFailure.TrySetResult());
            await afterFailure.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Program.Check(ingress.NeedsResync, "state exception marks synchronization failure while loop survives");
        }

        using (var ingress = new AuctionIngress(stateCapacity: 1))
        using (var release = new ManualResetEventSlim())
        {
            var started = Signal();
            ingress.TryPost(() => { started.TrySetResult(); release.Wait(TimeSpan.FromSeconds(5)); });
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            try
            {
                Program.Check(ingress.TryPost(() => { }), "remaining bounded queue slot accepts event");
                Program.Check(!ingress.TryPost(() => { }), "saturated queue rejects explicitly without waiting");
                Program.Check(ingress.NeedsResync && ingress.RejectedCount == 1 && ingress.StateDepth == 1,
                    "queue saturation reports resync requirement and bounded depth");
            }
            finally { release.Set(); }
        }

        using (var ingress = new AuctionIngress())
        {
            var requested = Signal();
            var release = Signal();
            bool staleApplied = false;
            ingress.TryRequest(async cancellation =>
            {
                requested.TrySetResult();
                await release.Task.WaitAsync(cancellation);
                return () => staleApplied = true;
            });
            await requested.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var resultApplied = Signal();
            ingress.TryPost(() => resultApplied.TrySetResult(), invalidates: true);
            await resultApplied.Task.WaitAsync(TimeSpan.FromSeconds(5));
            release.TrySetResult();
            await Until(() => ingress.StaleResponseCount == 1);
            Program.Check(!staleApplied, "late HTTP response cannot overwrite newer auction event");
            ingress.InvalidateSession();
            var recovered = Signal();
            ingress.TryRequest(_ => Task.FromResult<Action?>(() => recovered.TrySetResult()), recovers: true);
            await recovered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Until(() => !ingress.NeedsResync);
            Program.Check(!ingress.NeedsResync, "current full refresh clears synchronization failure");
        }

        using (var ingress = new AuctionIngress(requestCapacity: 1))
        {
            var started = Signal();
            var release = Signal();
            ingress.TryRequest(async cancellation =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(cancellation);
                return null;
            });
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Program.Check(ingress.TryRequest(_ => Task.FromResult<Action?>(null)), "bounded HTTP queue accepts remaining slot");
            Program.Check(!ingress.TryRequest(_ => Task.FromResult<Action?>(null)) && ingress.NeedsResync
                && ingress.RequestDepth == 1, "HTTP queue saturation is explicit and bounded");
            release.TrySetResult();
        }

        using (var ingress = new AuctionIngress())
        {
            var survived = Signal();
            ingress.TryRequest(_ => throw new InvalidOperationException("synthetic HTTP failure"));
            ingress.TryRequest(_ => Task.FromResult<Action?>(() => survived.TrySetResult()));
            await survived.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Program.Check(true, "HTTP exception does not terminate request consumer");
        }

        using (var ingress = new AuctionIngress())
        {
            var started = Signal();
            bool applied = false;
            ingress.TryRequest(async cancellation =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellation);
                return () => applied = true;
            });
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            ingress.Dispose();
            await ingress.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            Program.Check(!applied && !ingress.TryPost(() => { })
                && !ingress.TryRequest(_ => Task.FromResult<Action?>(null)),
                "shutdown cancels pending HTTP and rejects later input");
        }
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task Until(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition()) await Task.Delay(5, timeout.Token);
    }
}
