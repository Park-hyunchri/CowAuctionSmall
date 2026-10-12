using CowAuctionSmall.Infra.XmlSettings;
using CowAuctionSmall.Infra.Api;
using CowAuctionSmall.Infra.Logging;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Settings;
using CommunityToolkit.Mvvm.Messaging;
using System.Net.Http;
using System.Reflection;

internal static class QcnChecks
{
    internal static async Task Run()
    {
        using var http = new HttpClient(new NoNetworkHandler());
        var connection = new ServerConn(http);
        using var netty = new NettyAsyncMsgProcess(NLogger.Instance);
        var source = new QcnSource();
        using var service = new ServerGetData(new AuctionSessionRuntime(connection, new XmlParserCont(new BoardXmlParser(), new UserXmlParser())),
            netty, source, new AuctionSnapshotParser(new AnimalParseData()), NLogger.Instance);
        var user = new UserInfo();
        var capture = new object();
        int timerMessages = 0;
        WeakReferenceMessenger.Default.Register<DisplaySelectRefresh>(capture, (_, _) => Interlocked.Increment(ref timerMessages));
        try
        {
            SetDate(service, "20260917");
            using var releaseState = new ManualResetEventSlim();
            var stateStarted = Signal();
            netty.Ingress.TryPost(() => { stateStarted.TrySetResult(); releaseState.Wait(TimeSpan.FromSeconds(5)); });
            await stateStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            netty.Ingress.TryPost(() => SetDate(service, "20260918"), invalidates: true);
            Task<bool> oldRequest = Queue(service, user, "20260917");
            releaseState.Set();
            Program.Check(!await oldRequest.WaitAsync(TimeSpan.FromSeconds(5)) && source.Calls == 0,
                "old QCN date queued behind date switch is rejected before source request");

            source.Response = Task.FromResult<AuctionRoundInfo?>(new AuctionRoundInfo { AuctionDate = "20260918" });
            Program.Check(await Queue(service, user, "20260918").WaitAsync(TimeSpan.FromSeconds(5))
                && source.Calls == 1 && CacheDate(service) == "20260918",
                "current QCN date launches and applies on state consumer");

            var response = new TaskCompletionSource<AuctionRoundInfo?>(TaskCreationOptions.RunContinuationsAsynchronously);
            source.Response = response.Task;
            source.Requested = Signal();
            var pending = Queue(service, user, "20260918");
            await source.Requested.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var switched = Signal();
            netty.Ingress.TryPost(() => { SetDate(service, "20260919"); switched.TrySetResult(); }, invalidates: true);
            await switched.Task.WaitAsync(TimeSpan.FromSeconds(5));
            int timersBefore = timerMessages;
            response.TrySetResult(null);
            Program.Check(!await pending.WaitAsync(TimeSpan.FromSeconds(5)) && CacheDate(service) == "20260918"
                && timerMessages == timersBefore,
                "QCN response from previous date cannot overwrite cache or stop page timer");
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(capture);
            WeakReferenceMessenger.Default.UnregisterAll(connection);
            await service.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private static Task<bool> Queue(ServerGetData service, UserInfo user, string date) => (Task<bool>)typeof(ServerGetData)
        .GetMethod("QueueQcnRequestAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
        .Invoke(service, new object[] { user, "fixture-token", date })!;
    private static void SetDate(ServerGetData service, string date) => typeof(ServerGetData)
        .GetField("_currentRefreshDate", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(service, date);
    private static string? CacheDate(ServerGetData service) => (string?)typeof(ServerGetData)
        .GetField("_qcnCacheDate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service);
    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class QcnSource : IAuctionDataSource
    {
        internal int Calls;
        internal Task<AuctionRoundInfo?> Response { get; set; } = Task.FromResult<AuctionRoundInfo?>(null);
        internal TaskCompletionSource Requested { get; set; } = Signal();
        public Task<List<string>> ReadSnapshotAsync(UserInfo user, string token, string? date) => throw new InvalidOperationException("Snapshot query is not expected.");
        public Task<AuctionRoundInfo?> ReadQcnAsync(UserInfo user, string token, string? date, string? qcn = null)
        {
            Interlocked.Increment(ref Calls);
            Requested.TrySetResult();
            return Response;
        }
    }

    private sealed class NoNetworkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Network calls are prohibited in QCN checks.");
    }
}
