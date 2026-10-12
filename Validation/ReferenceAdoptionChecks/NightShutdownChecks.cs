using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Auction;
using System.Diagnostics;
using System.Reflection;

internal static class NightShutdownChecks
{
    internal static async Task Run()
    {
        var quiet = TimeSpan.FromMinutes(2);
        foreach (var sample in new[] { (22, 59, 59, false), (23, 0, 0, true), (0, 0, 0, true), (0, 59, 59, true), (1, 0, 0, false) })
        {
            var local = new DateTime(2026, 9, 22, sample.Item1, sample.Item2, sample.Item3);
            Program.Check(NightShutdownPolicy.ShouldShutdown(local, false, false, quiet) == sample.Item4,
                "night shutdown uses the inclusive 23:00 and exclusive 01:00 time boundaries");
        }
        var night = new DateTime(2026, 9, 22, 23, 30, 0);
        Program.Check(!NightShutdownPolicy.ShouldShutdown(night, true, false, quiet), "active auction postpones night shutdown");
        Program.Check(!NightShutdownPolicy.ShouldShutdown(night, false, true, quiet), "pending synchronization postpones night shutdown");
        Program.Check(!NightShutdownPolicy.ShouldShutdown(night, false, false, TimeSpan.FromMilliseconds(59999)),
            "the complete sixty-second signal grace interval is required");
        Program.Check(NightShutdownPolicy.ShouldShutdown(night, false, false, TimeSpan.FromSeconds(60)),
            "night shutdown becomes eligible at the sixty-second grace boundary");
        Program.Check(!NightShutdownPolicy.ShouldShutdown(night, false, false, TimeSpan.FromSeconds(-1)),
            "a negative signal interval cannot authorize shutdown");

        await CheckSignalGrace(night);
        await CheckBusyLifetime();
    }

    private static async Task CheckSignalGrace(DateTime night)
    {
        using var netty = new NettyAsyncMsgProcess(new SilentLog());
        try
        {
            AgeSignal(netty);
            Program.Check(NightShutdownPolicy.ShouldShutdown(night, false, false, netty.TimeSinceAuctionSignal),
                "the signal timer can represent a completed grace interval without sleeping");
            await Send(netty, new[] { "AS", "fixture", "7", "1", "100", "0", "8006" });
            Program.Check(!NightShutdownPolicy.ShouldShutdown(night, false, false, netty.TimeSinceAuctionSignal),
                "AS8006 opens a grace interval so shutdown cannot occur before the final AF result arrives");
            AgeSignal(netty);
            await Send(netty, new[] { "AF", "fixture", "7", "22", "", "412", "500" });
            Program.Check(!NightShutdownPolicy.ShouldShutdown(night, false, false, netty.TimeSinceAuctionSignal),
                "the later AF result restarts the grace interval after auction completion");
            AgeSignal(netty);
            await Send(netty, new[] { "UNRECOGNIZED", "fixture" });
            Program.Check(NightShutdownPolicy.ShouldShutdown(night, false, false, netty.TimeSinceAuctionSignal),
                "unknown traffic does not restart the auction signal grace interval");
        }
        finally
        {
            netty.Dispose();
            await netty.Ingress.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private static async Task CheckBusyLifetime()
    {
        using var ingress = new AuctionIngress();
        var firstStarted = Signal();
        var secondStarted = Signal();
        var releaseFirst = new TaskCompletionSource<Action?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSecond = new TaskCompletionSource<Action?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseApply = new ManualResetEventSlim();
        using var releaseState = new ManualResetEventSlim();
        try
        {
            Program.Check(!ingress.IsBusy, "new ingress starts idle");
            Task<bool> first = ingress.RequestAsync(_ => { firstStarted.TrySetResult(); return releaseFirst.Task; });
            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Program.Check(ingress.IsBusy && ingress.RequestDepth == 0,
                "an executing HTTP request remains busy after leaving the request queue");
            Task<bool> second = ingress.RequestAsync(_ => { secondStarted.TrySetResult(); return releaseSecond.Task; });
            Program.Check(ingress.IsBusy && ingress.RequestDepth == 1, "queued HTTP work keeps ingress busy behind an active request");
            releaseFirst.TrySetResult(null);
            await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Program.Check(ingress.IsBusy && ingress.RequestDepth == 0, "the next executing HTTP request remains busy with an empty queue");
            var applying = Signal();
            releaseSecond.TrySetResult(() =>
            {
                applying.TrySetResult();
                if (!releaseApply.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Synthetic apply release missing.");
            });
            await applying.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Program.Check(ingress.IsBusy, "HTTP response application stays busy until the state action finishes");
            releaseApply.Set();
            await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
            await WaitUntilIdle(ingress);
            Program.Check(!ingress.IsBusy && await second, "completed HTTP and response application release the busy state");

            var stateStarted = Signal();
            ingress.TryPost(() =>
            {
                stateStarted.TrySetResult();
                if (!releaseState.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Synthetic state release missing.");
            });
            await stateStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            ingress.TryPost(() => { });
            Program.Check(ingress.IsBusy && ingress.StateDepth == 1, "an active state action and queued protocol work postpone shutdown");
            releaseState.Set();
            await ingress.RequestAsync(_ => Task.FromResult<Action?>(() => { })).WaitAsync(TimeSpan.FromSeconds(5));
            await WaitUntilIdle(ingress);
            Program.Check(!ingress.IsBusy, "draining the state and request queues returns ingress to idle");
        }
        finally
        {
            releaseFirst.TrySetResult(null);
            releaseSecond.TrySetResult(null);
            releaseApply.Set();
            releaseState.Set();
            ingress.Dispose();
            await ingress.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private static void AgeSignal(NettyAsyncMsgProcess netty) => typeof(NettyAsyncMsgProcess)
        .GetField("_lastAuctionSignal", BindingFlags.Instance | BindingFlags.NonPublic)!
        .SetValue(netty, Stopwatch.GetTimestamp() - 61L * Stopwatch.Frequency);

    private static async Task WaitUntilIdle(AuctionIngress ingress)
    {
        long started = Stopwatch.GetTimestamp();
        while (ingress.IsBusy)
        {
            if (Stopwatch.GetElapsedTime(started) > TimeSpan.FromSeconds(5)) throw new TimeoutException("Ingress did not become idle.");
            await Task.Yield();
        }
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static async Task Send(NettyAsyncMsgProcess netty, string[] fields)
    {
        WeakReferenceMessenger.Default.Send(new DataStringArrMessage(fields));
        var consumed = Signal();
        netty.Ingress.TryPost(() => consumed.TrySetResult());
        await consumed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class SilentLog : IAppLog
    {
        public void LogInfo(string message) { }
        public void LogWarn(string message) { }
        public void LogError(string message) { }
    }
}
