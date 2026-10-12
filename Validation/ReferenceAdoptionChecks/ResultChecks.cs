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

internal static class ResultChecks
{
    internal static async Task Run()
    {
        foreach (string mode in new[] { "Y", "X", "B", "N", "" })
        {
            await using var fixture = new Fixture(mode);
            await fixture.Send(Af("22", "412", "500"));
            var immediate = fixture.Last;
            Program.Check(immediate.AuctionResultStatus == "22" && !immediate.IsRunning && immediate.BidPrice == "500"
                && ServerGetData._runRunSipNumber == -1, $"AF22 immediately publishes the winning amount in mode {mode}");
            Program.Check(immediate.Bidder == (mode is "Y" or "X" or "B" ? "-" : "412")
                && immediate.BidderString == "" && immediate.BidderNum == (mode == "B" ? "412" : ""),
                $"AF22 clears old names and respects the pending bidder display in mode {mode}");
            await fixture.Respond(Row("22", "412", "500"));
            var confirmed = fixture.Last;
            Program.Check(confirmed.BidPrice == "500" && fixture.Current.ResultBidderNumber == "412"
                && confirmed.Bidder == (mode is "Y" or "X" or "B" ? "CONFIRMED_NAME" : "412"),
                $"matching HTTP result supplies the current bidder display in mode {mode}");
            Program.Check(mode != "B" || (confirmed.BidderString == "CONFIRMED_NAME" && confirmed.BidderNum == "412"),
                "matching combined bidder display contains its confirmed name and participant number");
        }

        foreach (var mismatch in new[] { Row("11", "412", "500"), Row("22", "999", "500"), Row("22", "412", "490") })
        {
            await using var fixture = new Fixture("Y");
            await fixture.Send(Af("22", "412", "500"));
            await fixture.Respond(mismatch);
            CheckPending(fixture.Last, "mismatching HTTP result cannot replace the AF winning values or expose its name");
            fixture.Apply(mismatch);
            CheckPending(fixture.Last, "mismatching full snapshot cannot confirm a different result, participant, or amount");
            fixture.Apply(Row("22", "412", "500"));
            Program.Check(fixture.Last.Bidder == "CONFIRMED_NAME" && fixture.Last.BidPrice == "500",
                "a later matching full snapshot confirms the winner name");
        }

        await using (var fixture = new Fixture("Y"))
        {
            await fixture.Send(Af("22", "412", "500"));
            await fixture.Respond(Row("22", "412", "500", "OTHER_ANIMAL_NAME", "999999000000999"));
            Program.Check(fixture.Current.TraceNumber == "999999000000007" && fixture.Current.AuctionResultStatus == "22"
                && fixture.Current.BidPrice == "500" && fixture.Current.Bidder == "-",
                "an AF recheck for a different animal at the same entry cannot replace its identity or confirm an unrelated bidder name");
        }

        foreach (bool incompleteAf in new[] { false, true })
        {
            await using var fixture = new Fixture("Y");
            string[] fields = incompleteAf ? new[] { "AF", "fixture", "7", "22" } : Af("22", "412", "500");
            await fixture.Send(fields);
            await fixture.Respond(Row("22", "412", "500"));
            Program.Check(fixture.Last.BidPrice == "500" && fixture.Last.Bidder == "CONFIRMED_NAME"
                && fixture.Last.ResultBidderNumber == "412", "HTTP confirms the winning values even when AF omitted them");
            fixture.ResetResponse();
            await fixture.Send(fields);
            Program.Check(fixture.Last.BidPrice == "500" && fixture.Last.Bidder == "CONFIRMED_NAME"
                && fixture.Last.ResultBidderNumber == "412", "duplicate AF preserves the already confirmed winner name and amount");
            await fixture.Respond(Row("22", "999", "490", "STALE_NAME"));
            Program.Check(fixture.Last.BidPrice == "500" && fixture.Last.Bidder == "CONFIRMED_NAME"
                && fixture.Last.ResultBidderNumber == "412", "stale HTTP cannot replace a winner confirmed after complete or incomplete AF");
            fixture.Apply(Row("22", "999", "490", "STALE_NAME"));
            Program.Check(fixture.Last.BidPrice == "500" && fixture.Last.Bidder == "CONFIRMED_NAME"
                && fixture.Last.ResultBidderNumber == "412", "stale full snapshot cannot replace the confirmed winning values");
        }

        await using (var fixture = new Fixture("B"))
        {
            await fixture.Send(Af("23", "412", "500"));
            CheckUnsold(fixture.Last, "AF23 immediately removes all previous winning values");
            await fixture.Respond(Row("22", "999", "490"));
            CheckUnsold(fixture.Last, "an old sold HTTP response cannot restore winning values after AF23");
            fixture.Apply(Row("22", "999", "490"));
            CheckUnsold(fixture.Last, "an old sold full snapshot cannot restore winning values after AF23");
        }

        foreach (string[] fields in new[] { new[] { "AF", "fixture", "7", "22" }, Af("22", "412", ""),
            Af("22", "412", "invalid"), Af("22", "412", "-1") })
        {
            await using var fixture = new Fixture("Y");
            await fixture.Send(fields);
            Program.Check(fixture.Last.AuctionResultStatus == "22" && fixture.Last.BidPrice == "-"
                && fixture.Last.Bidder == "-" && fixture.Last.BidderNum == "" && fixture.Last.BidderString == "",
                "missing or invalid AF22 amount never reuses the previous winning amount or name");
        }

        await using (var fixture = new Fixture("Y", hasInitialSnapshot: false))
        {
            await fixture.Send(Af("22", "412", "500"));
            await fixture.Respond(Row("11", "999", "490"));
            fixture.Apply(Row("11", "999", "490"));
            CheckPending(fixture.Last, "AF22 received before the initial list overlays its eventual stale snapshot");
            fixture.Apply(Row("22", "412", "500"));
            Program.Check(fixture.Last.Bidder == "CONFIRMED_NAME", "early AF22 can later confirm its matching API winner");
        }
    }

    private static void CheckPending(gValues cow, string label) => Program.Check(cow.AuctionResultStatus == "22"
        && !cow.IsRunning && cow.BidPrice == "500" && cow.Bidder == "-" && cow.BidderString == "", label);

    private static void CheckUnsold(gValues cow, string label) => Program.Check(cow.AuctionResultStatus == "23"
        && !cow.IsRunning && cow.BidPrice == "유찰" && cow.Bidder == "-" && cow.BidderNum == "" && cow.BidderString == "", label);

    private static string[] Af(string result, string bidder, string price) => new[] { "AF", "fixture", "7", result, "", bidder, price };

    private static string Row(string result, string bidder, string price, string name = "CONFIRMED_NAME", string trace = "999999000000007")
    {
        var fields = Enumerable.Repeat("", 40).ToArray();
        fields[0] = "SV";
        fields[1] = "fixture";
        fields[2] = "7";
        fields[5] = trace;
        fields[29] = result;
        fields[30] = bidder;
        fields[31] = price;
        fields[36] = name;
        return string.Join('|', fields);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly HttpClient _http = new(new OfflineHandler());
        private readonly ServerConn _connection;
        private readonly NettyAsyncMsgProcess _netty = new(NLogger.Instance);
        private readonly OfflineSource _source = new();
        private readonly ServerGetData _service;
        private readonly UserInfo _user;
        private readonly FakeParser _parser = new();
        private readonly object _capture = new();
        private readonly ConcurrentQueue<DataChangedMessage> _received = new();
        private static readonly FieldInfo Previous = typeof(ServerGetData).GetField("_beforeAuctionDataList", BindingFlags.Static | BindingFlags.NonPublic)!;
        internal gValues Last => _received.Last(message => message.Data.Any(item => item.SipNumber == "7")).Data.Single(item => item.SipNumber == "7");
        internal gValues Current => ((List<gValues>)Previous.GetValue(null)!).Single(item => item.SipNumber == "7");

        internal Fixture(string mode, bool hasInitialSnapshot = true)
        {
            _connection = new ServerConn(_http);
            _user = new UserInfo { Auction = new Auction { AuctionHouseCode = "fixture", BidderName = mode } };
            _service = new ServerGetData(new AuctionSessionRuntime(_connection, new XmlParserCont(new BoardXmlParser(), new UserXmlParser())),
                _netty, _source, new AuctionSnapshotParser(_parser), NLogger.Instance);
            Set("_userInfo", _user);
            Set("_token", "fixture-token");
            var original = new gValues { SpaceIndex = "7", SipNumber = "7", EntityNumber = "SYNTHETIC_ENTITY", LowestPrice = "100",
                TraceNumber = "999999000000007",
                AuctionResultStatus = "11", IsRunning = true, BidPrice = "490", Bidder = "OLD_NAME", BidderString = "OLD_NAME",
                BidderNum = "999", ResultBidderNumber = "999", BidderName = mode };
            Previous.SetValue(null, hasInitialSnapshot ? new List<gValues> { original } : null);
            ServerGetData._runRunSipNumber = hasInitialSnapshot ? 7 : -1;
            ServerGetData._batchRunningState = false;
            WeakReferenceMessenger.Default.Register<DataChangedMessage>(_capture, (_, message) => _received.Enqueue(message));
        }

        internal async Task Send(string[] fields)
        {
            WeakReferenceMessenger.Default.Send(new DataStringArrMessage(fields));
            var consumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _netty.Ingress.TryPost(() => consumed.TrySetResult());
            await consumed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        internal void ResetResponse()
        {
            _source.Requested = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _source.Response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        internal async Task Respond(string row)
        {
            await _source.Requested.Task.WaitAsync(TimeSpan.FromSeconds(5));
            _source.Response.TrySetResult(new List<string> { row });
            await _netty.Ingress.RequestAsync(_ => Task.FromResult<Action?>(() => { })).WaitAsync(TimeSpan.FromSeconds(5));
        }

        internal void Apply(string row)
        {
            var parsed = _parser.ParsePacket(row, _user);
            typeof(ServerGetData).GetMethod("ApplySnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(_service, new object?[] { new List<gValues> { parsed }, null });
        }

        private void Set(string field, object value) => typeof(ServerGetData).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_service, value);

        public async ValueTask DisposeAsync()
        {
            WeakReferenceMessenger.Default.UnregisterAll(_capture);
            WeakReferenceMessenger.Default.UnregisterAll(_connection);
            _source.Response.TrySetResult(new List<string>());
            _service.Dispose();
            _netty.Dispose();
            await _service.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
            _http.Dispose();
            Previous.SetValue(null, null);
            ServerGetData._runRunSipNumber = -1;
            ServerGetData._batchRunningState = false;
        }
    }

    private sealed class FakeParser : IAuctionPacketParser
    {
        public gValues ParsePacket(string message, UserInfo user)
        {
            var fields = message.Split('|');
            string mode = user.Auction?.BidderName ?? string.Empty;
            return new gValues
            {
                SpaceIndex = "7", SipNumber = fields[2], EntityNumber = "SYNTHETIC_ENTITY", LowestPrice = "100",
                TraceNumber = fields[5],
                AuctionResultStatus = fields[29], ResultBidderNumber = fields[30], BidPrice = fields[31], BidderName = mode,
                Bidder = mode is "Y" or "X" or "B" ? fields[36] : fields[30],
                BidderNum = mode == "B" ? fields[30] : "", BidderString = mode == "B" ? fields[36] : ""
            };
        }
    }

    private sealed class OfflineSource : IAuctionDataSource
    {
        internal TaskCompletionSource Requested { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource<List<string>> Response { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<List<string>> ReadSnapshotAsync(UserInfo user, string token, string? date)
        {
            Requested.TrySetResult();
            return Response.Task;
        }
        public Task<AuctionRoundInfo?> ReadQcnAsync(UserInfo user, string token, string? date, string? qcn = null) => Task.FromResult<AuctionRoundInfo?>(null);
    }

    private sealed class OfflineHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"success\":true,\"data\":[]}") });
    }
}
