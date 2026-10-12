using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Infra.Api;
using CowAuctionSmall.Infra.XmlSettings;
using CowAuctionSmall.Infra.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Reflection;

internal static class BatchStateChecks
{
    internal static async Task Run()
    {
        await using (var fixture = new Fixture(hasInitialSnapshot: false))
        {
            await fixture.Send(At("10"));
            await fixture.Send(As("8004"));
            Program.Check(ServerGetData._batchRunningState, "batch start before the initial list sets the running state");
            await fixture.Send(At("10"));
            Program.Check(ServerGetData._batchRunningState, "duplicate batch method notification preserves its running state");
            await fixture.Send(As("8006"));
            Program.Check(!ServerGetData._batchRunningState, "batch completion without an initial list clears the running state");
            await fixture.Send(As("8004"));
            await fixture.Send(As("8007"));
            Program.Check(!ServerGetData._batchRunningState, "round completion without an initial list clears the batch running state");
            await fixture.Send(As("8004"));
            await fixture.Send(At("20"));
            Program.Check(!ServerGetData._batchRunningState, "switching to single auction releases batch running state");
        }

        await using (var fixture = new Fixture())
        {
            await fixture.Send(At("20"));
            await fixture.Send(ServiceChecks.Sv("120"));
            await fixture.Send(As("8004", "120"));
            Program.Check(fixture.Last.IsRunning && ServerGetData._runRunSipNumber == 7, "single auction begins before a method switch");
            await fixture.Send(At("10"));
            Program.Check(!fixture.Last.IsRunning && ServerGetData._runRunSipNumber == -1 && !ServerGetData._batchRunningState,
                "switching from single to batch releases both the displayed running flag and single running entry");
        }

        await using (var fixture = new Fixture(hasInitialSnapshot: false))
        {
            await fixture.Send(At("20"));
            await fixture.Send(ServiceChecks.Sv("120"));
            await fixture.Send(As("8004", "120"));
            await fixture.Apply(recovery: false);
            Program.Check(fixture.Last.IsRunning && fixture.Last.LowestPrice == "120" && fixture.Last.AuctionResultStatus == "11"
                && ServerGetData._runRunSipNumber == 7, "start received before the initial list overlays its price and running state on the first snapshot");
        }

        foreach (bool newStartAfterResync in new[] { false, true })
        {
            await using var fixture = new Fixture();
            await fixture.Send(At("20"));
            await fixture.Send(ServiceChecks.Sv("120"));
            await fixture.Send(As("8004", "120"));
            fixture.Invalidate();
            if (newStartAfterResync)
            {
                await fixture.Send(ServiceChecks.Sv("130"));
                await fixture.Send(As("8004", "130"));
            }
            await fixture.Apply(recovery: true);
            Program.Check(!fixture.NeedsResync, "recovery snapshot acknowledges the resynchronization boundary");
            if (newStartAfterResync)
                Program.Check(fixture.Last.IsRunning && fixture.Last.LowestPrice == "130" && ServerGetData._runRunSipNumber == 7,
                    "recovery snapshot retains a new start received after the resynchronization boundary");
            else
                Program.Check(!fixture.Last.IsRunning && fixture.Last.LowestPrice == "100" && ServerGetData._runRunSipNumber == -1,
                    "recovery snapshot releases an old single start when no new start followed the resynchronization boundary");
        }

        foreach (bool newStartAfterResync in new[] { false, true })
        {
            await using var fixture = new Fixture();
            await fixture.Send(At("10"));
            await fixture.Send(As("8004"));
            fixture.Invalidate();
            if (newStartAfterResync) await fixture.Send(As("8004"));
            await fixture.Apply(recovery: true);
            Program.Check(ServerGetData._batchRunningState == newStartAfterResync,
                "batch recovery keeps only a start received after its resynchronization boundary");
        }
    }

    private static string[] At(string method) => new[] { "AT", "fixture", method };
    private static string[] As(string status, string price = "100") => new[] { "AS", "fixture", "7", "1", price, "0", status };

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly HttpClient _http = new(new OfflineHandler());
        private readonly ServerConn _connection;
        private readonly NettyAsyncMsgProcess _netty = new(NLogger.Instance);
        private readonly ServerGetData _service;
        private readonly object _capture = new();
        private readonly ConcurrentQueue<DataChangedMessage> _received = new();
        private static readonly FieldInfo Previous = typeof(ServerGetData).GetField("_beforeAuctionDataList", BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo ApplySnapshot = typeof(ServerGetData).GetMethod("ApplySnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!;
        internal gValues Last => _received.Last(message => message.Data.Any(item => item.SipNumber == "7")).Data.Single(item => item.SipNumber == "7");
        internal bool NeedsResync => _netty.Ingress.NeedsResync;

        internal Fixture(bool hasInitialSnapshot = true)
        {
            _connection = new ServerConn(_http);
            _service = new ServerGetData(new AuctionSessionRuntime(_connection, new XmlParserCont(new BoardXmlParser(), new UserXmlParser())),
                _netty, new OfflineSource(), new AuctionSnapshotParser(new AnimalParseData()), NLogger.Instance);
            Previous.SetValue(null, hasInitialSnapshot ? new List<gValues> { Cow() } : null);
            ServerGetData._runRunSipNumber = -1;
            ServerGetData._batchRunningState = false;
            WeakReferenceMessenger.Default.Register<DataChangedMessage>(_capture, (_, message) => _received.Enqueue(message));
        }

        internal void Invalidate() => _netty.Ingress.InvalidateSession();

        internal async Task Send(string[] fields)
        {
            WeakReferenceMessenger.Default.Send(new DataStringArrMessage(fields));
            var consumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _netty.Ingress.TryPost(() => consumed.TrySetResult());
            await consumed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        internal async Task Apply(bool recovery)
        {
            bool applied = await _netty.Ingress.RequestAsync(_ => Task.FromResult<Action?>(() =>
                ApplySnapshot.Invoke(_service, new object?[] { new List<gValues> { Cow() }, null })), recovers: recovery)
                .WaitAsync(TimeSpan.FromSeconds(5));
            Program.Check(applied, "offline snapshot reaches the ingress application boundary");
        }

        private static gValues Cow() => new() { SpaceIndex = "7", SipNumber = "7", EntityNumber = "SYNTHETIC_ENTITY",
            LowestPrice = "100", AuctionResultStatus = "11" };

        public async ValueTask DisposeAsync()
        {
            WeakReferenceMessenger.Default.UnregisterAll(_capture);
            WeakReferenceMessenger.Default.UnregisterAll(_connection);
            _service.Dispose();
            _netty.Dispose();
            await _service.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
            _http.Dispose();
            Previous.SetValue(null, null);
            ServerGetData._runRunSipNumber = -1;
            ServerGetData._batchRunningState = false;
        }
    }

    private sealed class OfflineSource : IAuctionDataSource
    {
        public Task<List<string>> ReadSnapshotAsync(UserInfo user, string token, string? date) =>
            throw new InvalidOperationException("Batch state fixture must not issue HTTP requests.");
        public Task<AuctionRoundInfo?> ReadQcnAsync(UserInfo user, string token, string? date, string? qcn = null) =>
            throw new InvalidOperationException("Batch state fixture must not issue HTTP requests.");
    }

    private sealed class OfflineHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"success\":true,\"data\":[]}") });
    }
}
