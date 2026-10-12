using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;

internal static class PollingLifetimeChecks
{
    internal static async Task Run()
    {
        using var lifetime = new AuctionPollingLifetime();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool stopped = false;
        Task work = lifetime.StartAsync(async () =>
        {
            started.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, lifetime.Token); }
            finally { stopped = true; }
        });
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Program.Check(!lifetime.Completion.IsCompleted, "polling lifetime exposes active worker completion");
        lifetime.Dispose();
        try { await work.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (OperationCanceledException) { }
        Program.Check(stopped && lifetime.Completion.IsCompleted && lifetime.Token.IsCancellationRequested,
            "polling lifetime cancellation releases pending delay and completes worker");
        var runtime = new OfflineRuntime();
        var source = new UnusedDataSource();
        var log = new SilentLog();
        using var netty = new NettyAsyncMsgProcess(log);
        using var service = new ServerGetData(runtime, netty, source, new AuctionSnapshotParser(new UnusedParser()), log);
        bool authenticated = false;
        Task initializing = service.InitializeAsync(_ => authenticated = true);
        await runtime.NetworkChecked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
        await initializing.WaitAsync(TimeSpan.FromSeconds(5));
        Program.Check(!authenticated && runtime.ExternalCalls == 0 && source.Calls == 0 && initializing.IsCompleted,
            "fake offline initialization is canceled by StopAsync before authentication or data requests");
    }

    private sealed class OfflineRuntime : IAuctionSessionRuntime
    {
        internal int ExternalCalls;
        internal TaskCompletionSource NetworkChecked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public UserInfo? LoadUserSettings() => new() { Auction = new Auction { AuctionHouseCode = "fixture" } };
        public bool IsNetworkAvailable { get { NetworkChecked.TrySetResult(); return false; } }
        public bool IsConnected => false;
        public long TotalApiCount => 0;
        public Task<AuctionAuthenticationResult?> AuthenticateAsync(UserInfo user) { ExternalCalls++; throw new InvalidOperationException("Authentication not expected."); }
        public Task<List<gValues>?> ReadCachedAuctionAsync(UserInfo user) { ExternalCalls++; throw new InvalidOperationException("Cache request not expected."); }
        public Task<AuctionConnectionResult> ConnectAsync(UserInfo user, string token) { ExternalCalls++; throw new InvalidOperationException("Connection not expected."); }
        public Task DisconnectAsync() { ExternalCalls++; throw new InvalidOperationException("Disconnect not expected."); }
    }

    private sealed class UnusedDataSource : IAuctionDataSource
    {
        internal int Calls;
        public Task<List<string>> ReadSnapshotAsync(UserInfo user, string token, string? date) { Calls++; throw new InvalidOperationException("Snapshot not expected."); }
        public Task<AuctionRoundInfo?> ReadQcnAsync(UserInfo user, string token, string? date, string? qcn = null) { Calls++; throw new InvalidOperationException("QCN not expected."); }
    }

    private sealed class UnusedParser : IAuctionPacketParser
    {
        public gValues ParsePacket(string message, UserInfo user) => throw new InvalidOperationException("Parsing not expected.");
    }

    private sealed class SilentLog : IAppLog
    {
        public void LogInfo(string message) { }
        public void LogWarn(string message) { }
        public void LogError(string message) { }
    }
}
