using CowAuctionSmall.Infra.XmlSettings;
using CowAuctionSmall.Infra.Logging;
using CowAuctionSmall.Infra.Api;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Domain.Auction;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Reflection;

internal static class ServiceChecks
{
    internal static async Task Run()
    {
        using var handler = new OfflineHandler();
        using var http = new HttpClient(handler);
        var connection = new ServerConn(http);
        using var netty = new NettyAsyncMsgProcess(NLogger.Instance);
        var source = new OfflineSource();
        using var service = new ServerGetData(new AuctionSessionRuntime(connection, new XmlParserCont(new BoardXmlParser(), new UserXmlParser())),
            netty, source, new AuctionSnapshotParser(new AnimalParseData()), NLogger.Instance);
        var cow = new gValues { SpaceIndex = "7", SipNumber = "7", LowestPrice = "100", AuctionResultStatus = "11" };
        Set(service, "_userInfo", new UserInfo { Auction = new Auction { AuctionHouseCode = "fixture", BidderName = "B" } });
        Set(service, "_token", "fixture-token");
        var previous = typeof(ServerGetData).GetField("_beforeAuctionDataList", BindingFlags.Static | BindingFlags.NonPublic)!;
        previous.SetValue(null, new List<gValues> { cow });
        var capture = new object();
        var received = new ConcurrentQueue<DataChangedMessage>();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(capture, (_, message) => received.Enqueue(message));
        try
        {
            CheckSnapshotSessions(service, previous, received);
            previous.SetValue(null, new List<gValues> { cow });
            await SendSv(netty, Sv("412"));
            await Send(netty, As("8003", "12"));
            Program.Check(cow.LowestPrice == "412" && !cow.IsRunning,
                "SV updates the price even before an AT method notification arrives");
            await Send(netty, new[] { "AT", "fixture", "20" });
            await CheckPriceChanges(service, netty, cow, received);
            previous.SetValue(null, new List<gValues> { cow });
            await Send(netty, As("8004"));
            Program.Check(cow.IsRunning && ServerGetData._runRunSipNumber == 7, "real AS8004 handler begins single auction");
            Program.Check(received.Last().Data[0].IsRunning, "real handler publishes owned running snapshot");
            await Send(netty, As("8004"));
            Program.Check(cow.IsRunning && cow.AuctionResultStatus == "11", "duplicate progress remains stable");
            await Send(netty, new[] { "AF", "fixture", "7", "22", "", "", "100" });
            Program.Check(!cow.IsRunning && cow.AuctionResultStatus == "22" && ServerGetData._runRunSipNumber == -1,
                "real AF22 handler applies result and releases running lock");
            await source.Requested.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Send(netty, As("8006"));
            Program.Check(cow.AuctionResultStatus == "22", "AS8006 after AF22 retains sold result");
            await Send(netty, As("8004"));
            Program.Check(cow.IsRunning && cow.AuctionResultStatus == "11", "real AS8004 reauction resets previous result");
            await Send(netty, new[] { "AF", "fixture", "7", "24" });
            Program.Check(!cow.IsRunning && cow.AuctionResultStatus == "11" && ServerGetData._runRunSipNumber == -1,
                "legacy AF24 releases running state without rewriting stored result");
            await Send(netty, Array.Empty<string>());
            await Send(netty, new[] { "AS" });
            await Send(netty, new[] { "AF" });
            await Send(netty, new[] { "AT", "fixture", "invalid" });
            await Send(netty, As("8004"));
            Program.Check(cow.IsRunning, "malformed inputs do not kill protocol consumer");
            await Send(netty, As("8006"));
            await Send(netty, new[] { "AF", "fixture", "7", "23" });
            Program.Check(!cow.IsRunning && cow.AuctionResultStatus == "23", "AF23 after AS8006 applies unsold result");
            Program.Check(!source.Response.Task.IsCompleted, "later protocol state applies while HTTP recheck remains blocked");
            Program.Check(!netty.Ingress.NeedsResync, "normal protocol flow has no hidden handler exceptions");
            source.Response.TrySetResult(new List<string>());
            await netty.Ingress.RequestAsync(_ => Task.FromResult<Action?>(() => { })).WaitAsync(TimeSpan.FromSeconds(5));
            source.Response = new(TaskCreationOptions.RunContinuationsAsynchronously);
            source.Response.TrySetResult(new List<string> { "SV|fixture|7|bad" });
            int messagesBeforeInvalid = received.Count;
            string resultBeforeInvalid = cow.AuctionResultStatus;
            long staleBeforeInvalid = netty.Ingress.StaleResponseCount;
            service.RequestSoldItemRecheck(new[] { "AF", "fixture", "7", "23" });
            await Send(netty, Array.Empty<string>());
            await netty.Ingress.RequestAsync(_ => Task.FromResult<Action?>(() => { })).WaitAsync(TimeSpan.FromSeconds(5));
            Program.Check(received.Count == messagesBeforeInvalid && cow.AuctionResultStatus == resultBeforeInvalid
                && netty.Ingress.StaleResponseCount == staleBeforeInvalid,
                "malformed current-generation recheck response preserves existing snapshot and display");
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(capture);
            WeakReferenceMessenger.Default.UnregisterAll(connection);
            source.Response.TrySetResult(new List<string>());
            service.Dispose();
            netty.Dispose();
            await service.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Program.Check(service.PollingCompletion.IsCompleted && netty.Ingress.Completion.IsCompleted,
                "StopAsync awaits polling and both ingress workers");
            previous.SetValue(null, null);
            ServerGetData._runRunSipNumber = -1;
            ServerGetData._batchRunningState = false;
        }
    }

    private static string[] As(string status, string price = "100") => new[] { "AS", "fixture", "7", "1", price, "0", status };

    internal static string[] Sv(string price)
    {
        var fields = ItemUpdateChecks.Sv(7);
        fields[27] = price;
        return fields;
    }

    private static async Task SendSv(NettyAsyncMsgProcess netty, string[] fields)
    {
        var decoder = new CowAuctionSmall.Infra.Netty.netty.handlers.AuctionClientInboundDecoder(new OfflineController());
        try
        {
            string packet = string.Join("|", fields);
            var output = new List<object>();
            decoder.GetType().GetMethod("Decode", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(decoder, new object?[] { null, packet, output });
            Program.Check(output.Count == 1 && Equals(output[0], packet),
                "the real Netty decoder forwards an SV packet outside the refresh window without altering fields");
            foreach (string row in output) await Send(netty, row.Split('|'));
        }
        finally { WeakReferenceMessenger.Default.UnregisterAll(decoder); }
    }

    private sealed class OfflineController : CowAuctionSmall.Infra.Netty.interfaces.iNettyControllable
    {
        public void onActiveChannel(DotNetty.Transport.Channels.IChannelHandlerContext context) { }
        public void OnResponseConnectionInfo(CowAuctionSmall.Infra.Netty.models.ResponseConnectionInfo response) { }
        public void OnCurrentAuctionData(string data) { }
        public void onCheckSession(DotNetty.Transport.Channels.IChannelHandlerContext context,
            CowAuctionSmall.Infra.Netty.models.AuctionCheckSession session) { }
        public void onChannelInactive(int port) { }
        public void onConnectionException() { }
    }

    private static async Task CheckPriceChanges(ServerGetData service, NettyAsyncMsgProcess netty, gValues cow,
        ConcurrentQueue<DataChangedMessage> received)
    {
        await SendSv(netty, Sv("612"));
        await Send(netty, As("8003", "12"));
        Program.Check(cow.LowestPrice == "612" && received.Last().Data.Single().LowestPrice == "612" && !cow.IsRunning,
            "SV current minimum 612 wins over original minimum 312 and the companion AS8003 value 12");
        await SendSv(netty, Sv("120"));
        await Send(netty, As("8003", "12"));
        Program.Check(cow.LowestPrice == "120" && !cow.IsRunning && ServerGetData._runRunSipNumber == -1,
            "SV updates the changed price without starting the auction");
        Program.Check(received.Last().Data.Single().LowestPrice == "120",
            "SV publishes the changed price for the waiting display");

        cow.AuctionResultStatus = "22";
        cow.BidPrice = "500";
        cow.Bidder = "SYNTHETIC_BIDDER";
        cow.BidderNum = "901";
        cow.BidderString = "SYNTHETIC_BIDDER";
        cow.BidderName = "B";
        await SendSv(netty, Sv("130"));
        await Send(netty, As("8004", "12"));
        Program.Check(cow.LowestPrice == "130" && cow.IsRunning && cow.AuctionResultStatus == "11"
            && ServerGetData._runRunSipNumber == 7,
            "AS8004 starts with the received SV price without using its own price field");
        Program.Check(cow.BidPrice == "-" && cow.Bidder == "-" && cow.BidderNum == "" && cow.BidderString == ""
            && cow.BidderName == "B",
            "reauction clears previous winning values and preserves the bidder display option");
        var published = received.Last().Data.Single();
        Program.Check(published.LowestPrice == "130" && published.IsRunning && published.BidPrice == "-"
            && published.Bidder == "-" && published.BidderNum == "" && published.BidderString == "",
            "price-changing start publishes a consistent running snapshot without previous winning values");

        await Send(netty, As("8004", "130"));
        Program.Check(cow.LowestPrice == "130" && cow.IsRunning && cow.AuctionResultStatus == "11",
            "duplicate price-changing start preserves the running state");
        foreach (string state in new[] { "8003", "8004" })
        {
            foreach (string price in new[] { "", "invalid", "-1" })
            {
                await Send(netty, As(state, price));
                Program.Check(cow.LowestPrice == "130" && cow.IsRunning && cow.AuctionResultStatus == "11"
                    && ServerGetData._runRunSipNumber == 7,
                    $"{state} with invalid price preserves the current auction state");
            }
        }

        foreach (string price in new[] { "", "invalid", "-1" })
        {
            await SendSv(netty, Sv(price));
            Program.Check(cow.LowestPrice == "130" && cow.IsRunning,
                "malformed SV price preserves the current price and running state");
        }
        await Send(netty, new[] { "SV", "fixture", "7" });
        Program.Check(cow.LowestPrice == "130", "truncated SV does not overwrite the current price");

        var apply = typeof(ServerGetData).GetMethod("ApplySnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var stale = new gValues { SpaceIndex = "7", SipNumber = "7", LowestPrice = "100", AuctionResultStatus = "22", BidPrice = "500" };
        apply.Invoke(service, new object[] { new List<gValues> { stale }, "20260918" });
        var snapshot = ((List<gValues>)typeof(ServerGetData).GetField("_beforeAuctionDataList", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!).Single();
        Program.Check(snapshot.LowestPrice == "130" && snapshot.IsRunning && snapshot.AuctionResultStatus == "11"
            && snapshot.BidPrice == "-" && ServerGetData._runRunSipNumber == 7,
            "same-day stale polling response preserves the new running price and cleared winning values");

        cow.LowestPrice = "100";
        cow.IsRunning = false;
        cow.AuctionResultStatus = "11";
        cow.BidderName = "";
        ServerGetData._runRunSipNumber = -1;
    }

    private static void CheckSnapshotSessions(ServerGetData service, FieldInfo previous, ConcurrentQueue<DataChangedMessage> received)
    {
        var parse = typeof(ServerGetData).GetMethod("ParseSnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var user = new UserInfo();
        var empty = (List<gValues>)parse.Invoke(service, new object[] { new List<string>(), user })!;
        Program.Check(empty.Count == 0, "empty API result is a valid empty full snapshot");
        foreach (string row in new[] { "", "SV|fixture|7", string.Join("|", Enumerable.Repeat("fixture", 34)) })
        {
            bool rejected = false;
            try { parse.Invoke(service, new object[] { new List<string> { row }, user }); }
            catch (TargetInvocationException ex) when (ex.InnerException is FormatException) { rejected = true; }
            Program.Check(rejected, "incomplete API snapshot row is rejected before applying state");
        }
        var apply = typeof(ServerGetData).GetMethod("ApplySnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var running = new gValues { SpaceIndex = "7", SipNumber = "7", EntityNumber = "SYNTHETIC_ENTITY", IsRunning = true, AuctionResultStatus = "11" };
        var response = new gValues { SpaceIndex = "9", SipNumber = "7", EntityNumber = "SYNTHETIC_ENTITY", AuctionResultStatus = "22" };
        previous.SetValue(null, new List<gValues> { running });
        Set(service, "_appliedSnapshotDate", "20260917");
        ServerGetData._runRunSipNumber = 7;
        apply.Invoke(service, new object[] { new List<gValues> { response }, "20260917" });
        var sameDay = received.Last(message => message.SourceStatistics != null);
        var current = ((List<gValues>)previous.GetValue(null)!).Single();
        Program.Check(current.SpaceIndex == "7" && current.IsRunning
            && sameDay.SourceStatistics?.RawRowCount == 1, "same-day full response preserves running position and raw API count");
        apply.Invoke(service, new object[] { new List<gValues> { response }, "20260918" });
        var nextDay = received.Last(message => message.IsFullSnapshot);
        Program.Check(nextDay.Data.Single().SpaceIndex == "9" && nextDay.Data.Single().AuctionResultStatus == "22"
            && !nextDay.Data.Single().IsRunning && ServerGetData._runRunSipNumber == -1
            && nextDay.SourceStatistics?.RawRowCount == 1, "new-date snapshot releases old running overlay and preserves source count");
    }

    private static async Task Send(NettyAsyncMsgProcess netty, string[] fields)
    {
        WeakReferenceMessenger.Default.Send(new DataStringArrMessage(fields));
        var consumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        netty.Ingress.TryPost(() => consumed.TrySetResult());
        await consumed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private static void Set(object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    private sealed class OfflineHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"success\":true,\"data\":[]}") });
    }

    private sealed class OfflineSource : IAuctionDataSource
    {
        internal TaskCompletionSource Requested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource<List<string>> Response { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<List<string>> ReadSnapshotAsync(UserInfo user, string token, string? date)
        {
            Requested.TrySetResult();
            return Response.Task;
        }
        public Task<AuctionRoundInfo?> ReadQcnAsync(UserInfo user, string token, string? date, string? qcn = null) => Task.FromResult<AuctionRoundInfo?>(null);
    }
}
