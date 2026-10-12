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
using System.Net.Http;
using System.Reflection;
using System.Threading.Channels;

internal static class RefreshChecks
{
    internal static async Task Run()
    {
        await CheckDateRaces();
        await CheckDateIdentity();
        foreach (string? mode in new string?[] { "20", "10", null })
        {
            await using var fixture = new Fixture();
            if (mode != null) await fixture.Send(new[] { "AT", "fixture", mode });
            await fixture.Send(Refresh());
            var request = await fixture.NextRequest();
            Program.Check(fixture.RefreshCount == 0 && fixture.MessageCount == 0 && fixture.Current.LowestPrice == "100",
                $"F5 in mode {mode ?? "unknown"} starts a full query without clearing the existing display");
            request.TrySetResult(new List<string> { Row("7", "150") });
            await fixture.Drain();
            Program.Check(fixture.RefreshCount == 1 && fixture.Last.LowestPrice == "150" && fixture.Last.SpaceIndex == "7",
                $"successful F5 in mode {mode ?? "unknown"} refreshes the display and publishes the queried data");
        }

        foreach (bool malformed in new[] { false, true })
        {
            await using var fixture = new Fixture();
            await fixture.Send(new[] { "AT", "fixture", "20" });
            await fixture.Send(Refresh());
            var request = await fixture.NextRequest();
            if (malformed) request.TrySetResult(new List<string> { "SV|fixture|7|bad" });
            else request.TrySetException(new InvalidOperationException("Synthetic offline failure"));
            await fixture.Drain();
            Program.Check(fixture.RefreshCount == 0 && fixture.MessageCount == 0 && fixture.Current.LowestPrice == "100",
                "failed or malformed F5 response leaves the existing display intact without a success refresh");
        }

        await using (var fixture = new Fixture())
        {
            await fixture.Send(new[] { "AT", "fixture", "20" });
            await fixture.Send(ServiceChecks.Sv("120"));
            await fixture.Send(new[] { "AS", "fixture", "7", "1", "120", "0", "8004" });
            await fixture.Send(Refresh());
            var request = await fixture.NextRequest();
            Program.Check(fixture.RefreshCount == 0 && fixture.Last.IsRunning && fixture.Last.LowestPrice == "120",
                "F5 keeps the running display while its snapshot is pending");
            request.TrySetResult(new List<string> { Row("9", "100") });
            await fixture.Drain();
            Program.Check(fixture.RefreshCount == 1 && fixture.Last.IsRunning && fixture.Last.LowestPrice == "120"
                && fixture.Last.SpaceIndex == "7" && ServerGetData._runRunSipNumber == 7,
                "same-day F5 preserves the running entry position, updated price, and running state");
        }

        await using (var fixture = new Fixture())
        {
            await fixture.Send(new[] { "AT", "fixture", "20" });
            fixture.QueuePoll();
            var oldPoll = await fixture.NextRequest();
            await fixture.Send(Refresh());
            oldPoll.TrySetResult(new List<string> { Row("7", "90") });
            var refresh = await fixture.NextRequest();
            Program.Check(fixture.Current.LowestPrice == "100" && fixture.MessageCount == 0 && fixture.RefreshCount == 0,
                "F5 rejects the older pending poll and retains a separate full refresh request");
            refresh.TrySetResult(new List<string> { Row("7", "160") });
            await fixture.Drain();
            Program.Check(fixture.CallCount == 2 && fixture.RefreshCount == 1 && fixture.Last.LowestPrice == "160",
                "F5 is not coalesced into a pending poll and publishes its own new snapshot");
        }

        await using (var fixture = new Fixture())
        {
            await fixture.Send(Refresh());
            var refresh = await fixture.NextRequest();
            fixture.RequestRoundRefresh();
            await fixture.StateBarrier();
            refresh.TrySetResult(new List<string> { Row("7", "170") });
            await fixture.Drain();
            Program.Check(fixture.RefreshCount == 1 && fixture.Last.LowestPrice == "170",
                "the companion round refresh does not invalidate the pending AS8001 full snapshot");
        }
    }

    private static async Task CheckDateRaces()
    {
        const string nextDate = "20000102";
        await using (var fixture = new Fixture())
        {
            fixture.RoundDate = nextDate;
            fixture.RequestRoundRefresh();
            var todayProbe = await fixture.NextRequest();
            todayProbe.TrySetResult(new List<string>());
            var newDate = await fixture.NextRequest();
            fixture.QueuePoll();
            await fixture.StateBarrier();
            newDate.TrySetResult(new List<string> { Row("7", "180") });
            var oldDate = await fixture.NextRequest();
            await fixture.StateBarrier();
            Program.Check(fixture.Last.LowestPrice == "180" && fixture.RequestDates.Last() == "20260922",
                "round refresh applies its new date while an older-date poll remains pending");
            int messages = fixture.MessageCount;
            oldDate.TrySetResult(new List<string> { Row("7", "90") });
            await fixture.Drain();
            Program.Check(fixture.Last.LowestPrice == "180" && fixture.MessageCount == messages && fixture.RefreshCount == 1,
                "a poll queued before the round date changed cannot overwrite the new-date display");
        }

        await using (var fixture = new Fixture())
        {
            await fixture.Send(new[] { "AT", "fixture", "10" });
            await fixture.Send(new[] { "SZ", "fixture", nextDate, "0", "1", "P", "1", "900" });
            var superseded = await fixture.NextRequest();
            await fixture.Send(new[] { "AS", "fixture", "7", "1", "120", "0", "8004" });
            Program.Check(ServerGetData._batchRunningState, "batch start after selecting a new date becomes active before its first snapshot");
            fixture.QueuePoll();
            await fixture.StateBarrier();
            superseded.TrySetResult(new List<string> { Row("7", "90") });
            var firstAccepted = await fixture.NextRequest();
            Program.Check(fixture.RequestDates.Last() == nextDate, "the replacement poll uses the date selected by SZ");
            firstAccepted.TrySetResult(new List<string> { Row("7", "180") });
            await fixture.Drain();
            Program.Check(ServerGetData._batchRunningState && fixture.Last.LowestPrice == "180",
                "the first accepted snapshot for the selected date preserves a newer batch start");
        }
    }

    private static async Task CheckDateIdentity()
    {
        const string nextDate = "20000102";
        await using (var fixture = new Fixture())
        {
            fixture.Current.EntityNumber = "OLD_ENTITY";
            await fixture.Send(new[] { "AT", "fixture", "10" });
            await fixture.Send(new[] { "SZ", "fixture", nextDate, "0", "1", "P", "1", "900" });
            var superseded = await fixture.NextRequest();
            await fixture.Send(new[] { "AT", "fixture", "20" });
            var newEntry = ServiceChecks.Sv("120");
            newEntry[34] = "9";
            string newEntity = new AnimalParseData().ParseUpdate(string.Join('|', newEntry),
                new UserInfo { Auction = new Auction() }, true).Values.EntityNumber;
            await fixture.Send(newEntry);
            await fixture.Send(new[] { "AS", "fixture", "7", "1", "120", "0", "8004" });
            Program.Check(fixture.Current.EntityNumber == "OLD_ENTITY" && fixture.Current.LowestPrice == "100" && !fixture.Current.IsRunning,
                "a new-date start never marks the previous day's matching entry number as running");
            superseded.TrySetResult(new List<string> { Row("7", "90") });
            var initial = await fixture.NextRequest();
            initial.TrySetResult(new List<string> { Row("9", "180", newEntity) });
            await fixture.Drain();
            Program.Check(fixture.Last.EntityNumber == newEntity && fixture.Last.SpaceIndex == "9"
                && fixture.Last.IsRunning && fixture.Last.LowestPrice == "120",
                "the first new-date snapshot keeps its new animal and position while applying the new start price");
        }

        await using (var fixture = new Fixture())
        {
            fixture.Current.EntityNumber = "OLD_ENTITY";
            await fixture.Send(new[] { "AT", "fixture", "10" });
            await fixture.Send(new[] { "SZ", "fixture", nextDate, "0", "1", "P", "1", "900" });
            var superseded = await fixture.NextRequest();
            await fixture.Send(new[] { "AT", "fixture", "20" });
            await fixture.Send(new[] { "AF", "fixture", "7", "22", "", "412", "500" });
            Program.Check(fixture.Current.EntityNumber == "OLD_ENTITY" && fixture.Current.AuctionResultStatus == "11",
                "a new-date AF result does not assign the new result to the previous day's animal");
            superseded.TrySetResult(new List<string> { Row("7", "90") });
            var initial = await fixture.NextRequest();
            initial.TrySetResult(new List<string> { Row("9", "180", "NEW_ENTITY", "7", "22", "412", "500"),
                Row("10", "200", "OTHER_NEW_ENTITY", "8") });
            await fixture.Drain();
            Program.Check(fixture.Snapshot.Count == 2 && fixture.Snapshot.All(item => item.EntityNumber != "OLD_ENTITY")
                && fixture.Snapshot.Any(item => item.SipNumber == "8" && item.EntityNumber == "OTHER_NEW_ENTITY")
                && fixture.Last.EntityNumber == "NEW_ENTITY" && fixture.Last.SpaceIndex == "9"
                && fixture.Last.AuctionResultStatus == "22" && fixture.Last.BidPrice == "500",
                "the first new-date AF recheck replaces the old list with every new-date row and its confirmed result");
        }
    }

    private static string[] Refresh() => new[] { "AS", "fixture", "", "1", "", "", "8001" };
    private static string Row(string position, string price, string entity = "SYNTHETIC_ENTITY", string sip = "7",
        string result = "11", string bidder = "", string bidPrice = "-")
    {
        var fields = Enumerable.Repeat("", 40).ToArray();
        fields[0] = "SV";
        fields[1] = "fixture";
        fields[2] = sip;
        fields[3] = position;
        fields[4] = price;
        fields[5] = entity;
        fields[29] = result;
        fields[30] = bidder;
        fields[31] = bidPrice;
        return string.Join('|', fields);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly HttpClient _http = new(new NoNetworkHandler());
        private readonly ServerConn _connection;
        private readonly NettyAsyncMsgProcess _netty = new(NLogger.Instance);
        private readonly OfflineSource _source = new();
        private readonly ServerGetData _service;
        private readonly object _capture = new();
        private readonly ConcurrentQueue<DataChangedMessage> _received = new();
        private int _refreshCount;
        private static readonly FieldInfo Previous = typeof(ServerGetData).GetField("_beforeAuctionDataList", BindingFlags.Static | BindingFlags.NonPublic)!;
        internal gValues Last => _received.Last(message => message.Data.Any(item => item.SipNumber == "7")).Data.Single(item => item.SipNumber == "7");
        internal gValues Current => ((List<gValues>)Previous.GetValue(null)!).Single();
        internal IReadOnlyList<gValues> Snapshot => ((List<gValues>)Previous.GetValue(null)!).ToArray();
        internal int RefreshCount => Volatile.Read(ref _refreshCount);
        internal int MessageCount => _received.Count;
        internal int CallCount => Volatile.Read(ref _source.Calls);
        internal IEnumerable<string?> RequestDates => _source.Dates.ToArray();
        internal string? RoundDate { set => _source.RoundDate = value; }

        internal Fixture()
        {
            _connection = new ServerConn(_http);
            _service = new ServerGetData(new AuctionSessionRuntime(_connection, new XmlParserCont(new BoardXmlParser(), new UserXmlParser())),
                _netty, _source, new AuctionSnapshotParser(new FakeParser()), NLogger.Instance);
            Set("_userInfo", new UserInfo { Auction = new Auction { AuctionHouseCode = "fixture" }, CurrentInfo = new CurrentInfo { Date = "20260922" } });
            Set("_token", "fixture-token");
            Set("_appliedSnapshotDate", "20260922");
            Previous.SetValue(null, new List<gValues> { new() { SpaceIndex = "7", SipNumber = "7", EntityNumber = "SYNTHETIC_ENTITY",
                LowestPrice = "100", AuctionResultStatus = "11" } });
            ServerGetData._runRunSipNumber = -1;
            ServerGetData._batchRunningState = false;
            WeakReferenceMessenger.Default.Register<DataChangedMessage>(_capture, (_, message) => _received.Enqueue(message));
            WeakReferenceMessenger.Default.Register<DisplaySelectRefresh>(_capture, (_, message) =>
            {
                if (message.Data == "Refresh") Interlocked.Increment(ref _refreshCount);
            });
        }

        internal async Task Send(string[] fields)
        {
            WeakReferenceMessenger.Default.Send(new DataStringArrMessage(fields));
            await StateBarrier();
        }

        internal async Task StateBarrier()
        {
            var consumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _netty.Ingress.TryPost(() => consumed.TrySetResult());
            await consumed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        internal Task<TaskCompletionSource<List<string>>> NextRequest() => _source.Requests.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        internal async Task Drain()
        {
            await _netty.Ingress.RequestAsync(_ => Task.FromResult<Action?>(() => { })).WaitAsync(TimeSpan.FromSeconds(5));
        }
        internal void QueuePoll() => typeof(ServerGetData).GetMethod("ProcessMessageAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_service, null);
        internal void RequestRoundRefresh() => _service.RequestRefresh("1");
        private void Set(string name, object value) => typeof(ServerGetData).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_service, value);

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
        internal int Calls;
        internal string? RoundDate;
        internal ConcurrentQueue<string?> Dates { get; } = new();
        internal Channel<TaskCompletionSource<List<string>>> Requests { get; } = Channel.CreateUnbounded<TaskCompletionSource<List<string>>>();
        public Task<List<string>> ReadSnapshotAsync(UserInfo user, string token, string? date)
        {
            Interlocked.Increment(ref Calls);
            Dates.Enqueue(date);
            var response = new TaskCompletionSource<List<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
            Requests.Writer.TryWrite(response);
            return response.Task;
        }
        public Task<AuctionRoundInfo?> ReadQcnAsync(UserInfo user, string token, string? date, string? qcn = null) => Task.FromResult<AuctionRoundInfo?>(RoundDate == null ? null : new AuctionRoundInfo { AuctionDate = RoundDate });
    }

    private sealed class FakeParser : IAuctionPacketParser
    {
        public AuctionItemUpdate ParseUpdate(string message, UserInfo user, bool isSv, gValues? previous = null)
        {
            if (isSv) return new AnimalParseData().ParseUpdate(message, user, true, previous);
            var value = ParsePacket(message, user);
            value.AuctionHouseCode = "fixture";
            value.AuctionRound = "418"; // Fake API row[3] represents position, not the real protocol round.
            value.SuppliedFields = AuctionItemFields.All;
            return new AuctionItemUpdate(value, AuctionItemFields.All, "fixture", "418");
        }

        public gValues ParsePacket(string message, UserInfo user)
        {
            var fields = message.Split('|');
            return new gValues { SpaceIndex = fields[3], SipNumber = fields[2], EntityNumber = fields[5], LowestPrice = fields[4],
                AuctionResultStatus = fields[29], ResultBidderNumber = fields[30], Bidder = fields[30], BidPrice = fields[31] };
        }
    }

    private sealed class NoNetworkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Refresh fixture must not access the network.");
    }
}
