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
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;

internal static class ItemUpdateChecks
{
    internal static async Task Run(bool baseline = false)
    {
        if (!baseline)
        {
            CheckMapping();
            CheckAuctionHouseOverrides();
            CheckStoreRaces();
            await CheckBatcher();
            await CheckUpdates();
        }
        await Measure(baseline);
    }

    private static void CheckMapping()
    {
        var parser = new AnimalParseData();
        foreach (string house in new[] { "fixture", "8808990656885", "8808990227283" })
        foreach (string showBirth in new[] { "Y", "N" })
        {
            var user = new UserInfo { Auction = new Auction { AuctionHouseCode = house, CowBirth = showBirth, BidderName = "B", ChangeSexName = "N" } };
            var sv = Sv(7);
            sv[29] = "22";
            sv[30] = "901";
            sv[31] = "720";
            sv[36] = "SYNTHETIC_BIDDER";
            sv[37] = "9";
            sv[28] = "synthetic%20note";
            var api = Api(7);
            Array.Copy(sv, api, 36);
            api[0] = "SV"; // The API transport may use the same prefix; its explicit format wins.
            api[38] = sv[36]; api[39] = sv[37]; api[40] = sv[38];
            var left = parser.ParseUpdate(string.Join('|', sv), user, true);
            var right = parser.ParseUpdate(string.Join('|', api), user, false);
            Program.Check(left.Fields.All(field => Equals(AuctionItemFields.Get(left.Values, field), AuctionItemFields.Get(right.Values, field))),
                $"SV/API common field parity for house {house}, birth option {showBirth}, and explicit input format");
            Program.Check(left.Values.BidderNum == "901" && left.Values.BidderString == "SYNTHETIC_BIDDER"
                && left.Values.LowestPrice == "412", "39-field SV resolves bidder position 36 and current price position 27");
            Program.Check(!left.Fields.Contains(nameof(gValues.BodyLength)) && right.Fields.Contains(nameof(gValues.BodyLength)),
                "field presence distinguishes SV omission from supplied API extra data");
        }
        var options = new UserInfo { Auction = new Auction { AuctionHouseCode = "fixture", CowBirth = "N", BidderName = "X" } };
        var invalid = Sv(7);
        invalid[27] = "invalid"; invalid[25] = "-10"; invalid[37] = "bad-month";
        var parsed = parser.ParseUpdate(string.Join('|', invalid), options, true);
        Program.Check(!parsed.Fields.Contains(nameof(gValues.LowestPrice)) && !parsed.Fields.Contains(nameof(gValues.Weight))
            && !parsed.Fields.Contains(nameof(gValues.BirthMonth)) && !parsed.Fields.Contains(nameof(gValues.Birth)),
            "invalid price, weight, and month are absent updates rather than destructive default values");
        invalid[29] = "22"; invalid[30] = "901"; invalid[36] = "";
        parsed = parser.ParseUpdate(string.Join('|', invalid), options, true);
        Program.Check(parsed.Values.Bidder == "-" || string.IsNullOrEmpty(parsed.Values.Bidder),
            "empty bidder display name with X masking does not throw or fabricate a name");
        var snapshots = new AuctionSnapshotParser(parser);
        foreach (int field in new[] { 1, 3 })
        {
            var wrong = Api(8); wrong[field] = "different";
            bool rejected = false;
            try { snapshots.Parse(new List<string> { string.Join('|', Api(7)), string.Join('|', wrong) }, options); }
            catch (FormatException) { rejected = true; }
            Program.Check(rejected, "an API snapshot mixing auction houses or rounds is rejected as a whole");
        }
        var duplicate = Api(8); duplicate[2] = "007";
        bool duplicateRejected = false;
        try { snapshots.Parse(new List<string> { string.Join('|', Api(7)), string.Join('|', duplicate) }, options); }
        catch (FormatException) { duplicateRejected = true; }
        Program.Check(duplicateRejected, "duplicate normalized entry numbers reject a full API snapshot");
    }

    private static void CheckAuctionHouseOverrides()
    {
        var parser = new AnimalParseData();

        gValues Parse(string house, Action<string[]> configure, string changeSexName = "N")
        {
            var fields = Api(7);
            fields[1] = house;
            configure(fields);
            var user = new UserInfo
            {
                Auction = new Auction
                {
                    AuctionHouseCode = house,
                    ChangeSexName = changeSexName,
                    IsPaternityMatch = "Y"
                }
            };
            return parser.ParsePacket(string.Join('|', fields), user);
        }

        var iksan = Parse("8808990227283", fields =>
        {
            fields[22] = "전북 익산시";
            fields[28] = "친자 확인, 비고";
        });
        Program.Check(iksan.Location == "관내" && iksan.PaternityMatch == "친자일치" &&
            iksan.FrontNoteWord == "친자" && iksan.Note == "비고",
            "Iksan-Gunsan derives paternity from its note and removes the matched phrase");

        var sangju = Parse("8808990657639", fields => fields[4] = "1");
        Program.Check(sangju.Location == "축주", "Sangju replaces the location for cattle auction items");

        var yeongcheon = Parse("8808990656687", fields =>
        {
            fields[4] = "3";
            fields[17] = "0";
        });
        var boryeong = Parse("8808990683973", fields =>
        {
            fields[4] = "3";
            fields[17] = "4";
        });
        Program.Check(yeongcheon.Pregnant == "X" && boryeong.Pregnant == "미정",
            "Yeongcheon and Boryeong retain their breeding-cow pregnancy overrides");

        var hoengseong = Parse("8808990656885", fields =>
        {
            fields[4] = "1";
            fields[13] = "암";
            fields[39] = "10";
        });
        Program.Check(hoengseong.Sex == "암소", "Hoengseong retains its month-aware sex override");

        var partialHoengseong = Sv(7).Take(36).ToArray();
        partialHoengseong[1] = "8808990656885";
        var partialUser = new UserInfo
        {
            Auction = new Auction { AuctionHouseCode = "8808990656885", ChangeSexName = "N" }
        };
        var partialUpdate = parser.ParseUpdate(string.Join('|', partialHoengseong), partialUser, true);
        Program.Check(!partialUpdate.Fields.Contains(nameof(gValues.Sex)),
            "Hoengseong SV without a month does not overwrite its month-aware sex display");

        var jangseong = Parse("8808990817675", fields => fields[28] = "테스트 으뜸 비고");
        Program.Check(jangseong.FrontNoteWord == "으뜸" && jangseong.FrontNoteWordBrush == "Aqua" &&
            jangseong.Note == "테스트  비고",
            "Jangseong extracts its first front-note keyword after common parsing");

        var hiddenUser = new UserInfo
        {
            Auction = new Auction { AuctionHouseCode = "8808990817675", IsPaternityMatch = "N" }
        };
        var hiddenFields = Api(7);
        hiddenFields[1] = "8808990817675";
        hiddenFields[28] = "테스트 친자 비고";
        var hiddenPaternity = parser.ParsePacket(string.Join('|', hiddenFields), hiddenUser);
        Program.Check(hiddenPaternity.PaternityMatch == "-" && string.IsNullOrEmpty(hiddenPaternity.FrontNoteWord) &&
            string.IsNullOrEmpty(hiddenPaternity.FrontNoteWordBrush),
            "the common IsPaternityMatch setting hides auction-house-specific paternity display");
    }

    private static void CheckStoreRaces()
    {
        var original = new gValues { SipNumber = "7", SpaceIndex = "7", LowestPrice = "412", Note = "old", TraceNumber = "999999000000007", AuctionRound = "418" };
        var store = new AuctionItemStore();
        store.Bind(new List<gValues> { original });
        long started = store.Revision;
        var newer = original.Clone(); newer.LowestPrice = "612";
        store.Apply(new AuctionItemUpdate(newer, new[] { nameof(gValues.LowestPrice) }, "fixture", "418"), false);
        var prematureMatch = original.Clone();
        store.Protect(prematureMatch, started);
        var stale = original.Clone(); stale.LowestPrice = "412";
        store.Protect(stale, store.Revision);
        Program.Check(stale.LowestPrice == "612", "an API request started before SV cannot acknowledge away its new-field protection");
        long request = store.Revision;
        newer.LowestPrice = "712";
        store.Apply(new AuctionItemUpdate(newer, new[] { nameof(gValues.LowestPrice) }, "fixture", "418"), false);
        var intermediate = original.Clone(); intermediate.LowestPrice = "612";
        store.Protect(intermediate, request);
        Program.Check(intermediate.LowestPrice == "712", "a newer SV between API request and acknowledgement remains protected");
        var oldEmpty = new List<gValues>();
        store.ProtectMissing(oldEmpty, request);
        Program.Check(oldEmpty.Single().LowestPrice == "712", "a pending old API cannot delete an entry modified after its request began");
        var currentEmpty = new List<gValues>();
        store.ProtectMissing(currentEmpty, store.Revision);
        Program.Check(currentEmpty.Count == 0, "a current valid empty full snapshot may delete the previous entry");
        var replacement = newer.Clone(); replacement.TraceNumber = "999999000000999"; replacement.LowestPrice = "100";
        replacement.Note = "replacement";
        long beforeReplacement = store.Revision;
        store.Apply(new AuctionItemUpdate(replacement, new[] { nameof(gValues.TraceNumber), nameof(gValues.LowestPrice), nameof(gValues.Note) }, "fixture", "418"), false);
        Program.Check(original.TraceNumber == replacement.TraceNumber && original.LowestPrice == "100", "same entry with another animal replaces its previous SV protection");
        Program.Check(store.PreserveNewerIdentity(newer, beforeReplacement).TraceNumber == replacement.TraceNumber,
            "an API started before SV animal replacement cannot restore the previous animal at the same entry number");
        Program.Check(ReferenceEquals(store.PreserveNewerIdentity(newer, store.Revision), newer),
            "a current API identity remains eligible for normal full-snapshot reconciliation");
    }

    private static async Task CheckBatcher()
    {
        var callbacks = new ConcurrentQueue<Action>();
        var messages = new List<IReadOnlyList<gValues>>();
        using var batcher = new AuctionDisplayUpdateBatcher(callbacks.Enqueue, rows => messages.Add(rows), TimeSpan.FromMilliseconds(20));
        var cow = new gValues { SpaceIndex = "7", SipNumber = "7", LowestPrice = "100" };
        batcher.Add(new[] { cow });
        Program.Check(messages.Count == 1, "first isolated change is delivered immediately");
        cow.LowestPrice = "200"; batcher.Add(new[] { cow });
        cow.LowestPrice = "300"; batcher.Add(new[] { cow });
        cow.LowestPrice = "400";
        batcher.Flush();
        Program.Check(messages.Count == 2 && messages.Last().Single().LowestPrice == "300"
            && messages.First().Single().LowestPrice == "100", "batched notifications own their data and retain only the last queued slot value");
        cow.LowestPrice = "500"; batcher.Add(new[] { cow });
        await Task.Delay(40);
        batcher.Clear();
        while (callbacks.TryDequeue(out var callback)) callback();
        Program.Check(messages.Count == 2, "clearing a session invalidates already posted timer callbacks");
        batcher.Add(new[] { cow });
        cow.LowestPrice = "600"; batcher.Add(new[] { cow });
        await Task.Delay(40);
        batcher.Dispose();
        while (callbacks.TryDequeue(out var callback)) callback();
        Program.Check(messages.Count == 3, "dispose prevents delayed notification and retains the last immediate delivery only");
    }

    private static async Task CheckUpdates()
    {
        await using var fixture = new Fixture();
        var api = Api(7);
        api[28] = "original-note";
        await fixture.Apply(api);
        fixture.ClearMessages();
        var sv = Sv(7);
        sv[27] = "612";
        sv[28] = "changed-note";
        sv[25] = "450";
        sv[37] = "9";
        await fixture.Send(sv);
        await fixture.Flush();
        var cow = fixture.Current.Single();
        Program.Check(cow.LowestPrice == "612" && cow.Weight == "450" && cow.Note == "changed-note",
            "39-field SV updates price, weight, and note in the same entry");
        Program.Check(cow.bodyWeightInColdNum == "19.5" && cow.BodyLength == "120" && cow.Child_Kpn == "KPN9999",
            "SV preserves absent API-only EPD, body, and child fields");
        Program.Check(cow.Birth.Contains("9") || cow.BirthMonth.Contains("9"),
            "SV month field 37 recalculates month display without reading API field 39");
        int count = fixture.Messages.Count;
        await fixture.Send(sv);
        await fixture.Flush();
        Program.Check(fixture.Messages.Count == count, "identical SV emits no display notification");
        sv[28] = "";
        await fixture.Send(sv);
        await fixture.Flush();
        Program.Check(string.IsNullOrEmpty(fixture.Current.Single().DisplayNote), "an explicitly empty SV note clears the previous note");
        foreach (string invalid in new[] { "bad", "-1" })
        {
            sv[27] = invalid;
            await fixture.Send(sv);
            Program.Check(fixture.Current.Single().LowestPrice == "612", "invalid SV price does not overwrite the last valid price");
        }
        sv[27] = "612";
        var stale = Api(7);
        stale[41] = "125";
        await fixture.Apply(stale);
        Program.Check(fixture.Current.Single().LowestPrice == "612" && fixture.Current.Single().DisplayNote == ""
            && fixture.Current.Single().BodyLength == "125", "stale API preserves SV fields while updating unprotected API-only data");
        var acknowledged = Api(7);
        acknowledged[27] = "612";
        acknowledged[25] = "450";
        acknowledged[28] = "";
        acknowledged[39] = "9";
        await fixture.Apply(acknowledged);
        acknowledged[28] = "new-api-note";
        await fixture.Apply(acknowledged);
        Program.Check(fixture.Current.Single().Note == "new-api-note", "API may change a field after acknowledging its latest SV value");
        await fixture.Send(new[] { "SV", "fixture", "7" });
        Program.Check(fixture.Current.Count == 1 && fixture.Current.Single().LowestPrice == "612", "truncated SV preserves the existing entry");
        sv[27] = "0";
        await fixture.Send(sv);
        await fixture.Flush();
        Program.Check(fixture.Current.Single().LowestPrice == "결장", "SV zero retains the existing absent-entry display meaning");

        await fixture.Apply(Api(7), Api(8));
        var moved = Sv(8);
        moved[34] = "99";
        await fixture.Send(moved);
        await fixture.Flush();
        Program.Check(fixture.Current.Count == 2 && fixture.Current.Single(item => item.SipNumber == "8").SpaceIndex == "99",
            "SV moves its entry without deleting entries omitted from the partial update");
        await fixture.Apply(Api(7));
        Program.Check(fixture.Current.Count == 1, "a valid complete API snapshot removes an omitted entry");
    }

    private static async Task Measure(bool baseline)
    {
        foreach (string scenario in new[] { "unchanged", "three", "all", "repeated" })
        {
            await using var fixture = new Fixture();
            var api = Enumerable.Range(1, 501).Select(Api).ToArray();
            await fixture.Apply(api);
            fixture.ClearMessages();
            var packets = Enumerable.Range(1, 501).Select(Sv).ToArray();
            if (scenario == "three") foreach (int index in new[] { 0, 250, 500 }) packets[index][27] = "612";
            if (scenario == "all") foreach (var packet in packets) packet[27] = "612";
            if (scenario == "repeated")
            {
                packets = Enumerable.Range(1, 501).Select(_ => Sv(7)).ToArray();
                for (int i = 0; i < packets.Length; i++) packets[i][27] = (400 + i).ToString();
            }
            var elapsed = Stopwatch.StartNew();
            long allocated = GC.GetTotalAllocatedBytes(false);
            await fixture.Burst(packets);
            double stateMs = elapsed.Elapsed.TotalMilliseconds;
            await fixture.Flush();
            elapsed.Stop();
            allocated = GC.GetTotalAllocatedBytes(false) - allocated;
            int messages = fixture.Messages.Count;
            int rows = fixture.Messages.Sum(message => message.Data.Count);
            Console.WriteLine($"SV update measurement {(baseline ? "before" : "after")}: {scenario}; input=501; notifications={messages}; rows={rows}; state_ms={stateMs:F2}; flushed_ms={elapsed.Elapsed.TotalMilliseconds:F2}; allocated_bytes={allocated}; debugger={Debugger.IsAttached}; ingress direct business messages, no decoder/raw log or rendering.");
            if (baseline) continue;
            if (scenario == "unchanged") Program.Check(messages == 0, "501 identical SV rows emit no display notifications");
            if (scenario == "three") Program.Check(rows == 3 && messages <= 3, "501 SV rows with three changes deliver only three changed entries");
            if (scenario == "all") Program.Check(rows == 501 && messages < 501, "501 changed SV rows preserve every final entry while coalescing display messages");
            if (scenario == "repeated") Program.Check(fixture.Current.Single(item => item.SipNumber == "7").LowestPrice == "900"
                && rows < 501, "repeated updates retain the final price while reducing display deliveries");
            fixture.ClearMessages();
            await fixture.Apply(api);
            await fixture.Flush();
            Program.Check(!fixture.Messages.Any(message => message.IsFullSnapshot), "same-session API reuse avoids a redundant full display snapshot");
        }
    }

    internal static string[] Sv(int sip)
    {
        var fields = Enumerable.Repeat("", 39).ToArray();
        fields[0] = "SV";
        fields[1] = "fixture";
        fields[2] = sip.ToString();
        fields[3] = "418";
        fields[4] = "1";
        fields[5] = "999999" + sip.ToString("D9");
        fields[6] = "01";
        fields[9] = "FIXTURE";
        fields[11] = "26.01.01";
        fields[12] = "KPN1234";
        fields[13] = "암";
        fields[14] = "혈통";
        fields[16] = "2";
        fields[17] = "0";
        fields[18] = "2";
        fields[21] = "02";
        fields[22] = "FIX";
        fields[23] = "1";
        fields[24] = "1";
        fields[25] = "312";
        fields[26] = "312";
        fields[27] = "412";
        fields[29] = "11";
        fields[31] = "0";
        fields[33] = "N";
        fields[34] = sip.ToString();
        fields[35] = "N";
        fields[37] = "8";
        fields[38] = "혈통";
        return fields;
    }

    internal static string[] Api(int sip)
    {
        var sv = Sv(sip);
        var fields = Enumerable.Repeat("", 75).ToArray();
        Array.Copy(sv, fields, 36);
        fields[0] = "SC";
        fields[36] = "0";
        fields[37] = "20000101000000";
        fields[38] = sv[36];
        fields[39] = sv[37];
        fields[40] = sv[38];
        fields[41] = "120";
        fields[42] = "130";
        fields[45] = "A";
        fields[46] = "19.5";
        fields[60] = "KPN9999";
        return fields;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly HttpClient _http = new(new NoNetworkHandler());
        private readonly ServerConn _connection;
        private readonly NettyAsyncMsgProcess _netty = new(NLogger.Instance);
        private readonly ServerGetData _service;
        private readonly AuctionSnapshotParser _parser = new(new AnimalParseData());
        private readonly UserInfo _user = new() { Auction = new Auction { AuctionHouseCode = "fixture", CowBirth = "N" },
            CurrentInfo = new CurrentInfo { Date = "20000101" } };
        private readonly object _capture = new();
        private static readonly FieldInfo Previous = typeof(ServerGetData).GetField("_beforeAuctionDataList", BindingFlags.Static | BindingFlags.NonPublic)!;
        internal ConcurrentQueue<DataChangedMessage> Messages { get; } = new();
        internal List<gValues> Current => (List<gValues>)Previous.GetValue(null)!;

        internal Fixture()
        {
            _connection = new ServerConn(_http);
            _service = new ServerGetData(new AuctionSessionRuntime(_connection, new XmlParserCont(new BoardXmlParser(), new UserXmlParser())),
                _netty, new NoQueries(), _parser, NLogger.Instance);
            Set("_userInfo", _user);
            Set("_token", "fixture-token");
            Previous.SetValue(null, null);
            ServerGetData._runRunSipNumber = -1;
            ServerGetData._batchRunningState = false;
            WeakReferenceMessenger.Default.Register<DataChangedMessage>(_capture, (_, message) => Messages.Enqueue(message));
        }

        internal async Task Apply(params string[][] packets)
        {
            var parsed = _parser.Parse(packets.Select(fields => string.Join('|', fields)).ToList(), _user);
            await Post(() => typeof(ServerGetData).GetMethod("ApplySnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(_service, new object?[] { parsed, "20000101" }));
        }

        internal Task Send(string[] packet) => Burst(new[] { packet });
        internal Task Burst(string[][] packets) => Post(() =>
        {
            foreach (var packet in packets)
                WeakReferenceMessenger.Default.Send(new DataToServerGetArrMsg(new[] { "20" }.Concat(packet).ToArray()));
        });
        internal void ClearMessages() => Messages.Clear();
        internal async Task Flush()
        {
            var flush = typeof(ServerGetData).GetMethod("FlushDisplayUpdates", BindingFlags.Instance | BindingFlags.NonPublic);
            if (flush != null) await Post(() => flush.Invoke(_service, null));
            else await Task.Delay(60);
            await Post(() => { });
        }
        private async Task Post(Action action)
        {
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _netty.Ingress.TryPost(() =>
            {
                try { action(); done.TrySetResult(); }
                catch (Exception exception) { done.TrySetException(exception); }
            });
            await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
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
    private sealed class NoQueries : IAuctionDataSource
    {
        public Task<List<string>> ReadSnapshotAsync(UserInfo user, string token, string? date) => throw new InvalidOperationException("Unexpected synthetic snapshot query");
        public Task<AuctionRoundInfo?> ReadQcnAsync(UserInfo user, string token, string? date, string? qcn = null) => Task.FromResult<AuctionRoundInfo?>(null);
    }
    private sealed class NoNetworkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Item update checks must not access the network.");
    }
}
