using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Application.AnimalHistory;
using CowAuctionSmall.Domain.AnimalHistory;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Infra.Api;
using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views.Common;
using CowAuctionSmall.Views.Size128_128.Running;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

internal static class FarmAddressChecks
{
    private const string Number = "410000000000001";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static AnimalHistoryResult Parse(params string[] items) => AnimalHistoryParser.ParseAnimalHistory(
        "<response><resultCode>00</resultCode><items>" + string.Concat(items) + "</items></response>");
    private static string Item(string date, string owner, string address, string type = "양수") =>
        $"<item><infoType>2</infoType><regYmd>{date}</regYmd><regType>{type}</regType><farmerNm>{owner}</farmerNm><farmAddr>{address}</farmAddr></item>";

    internal static void RunView()
    {
        var application = System.Windows.Application.Current ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        application.Resources["LocationForeground"] = Brushes.Cyan;
        SnapshotChecks.Run();
        HistoryNoteSettingsChecks.Run();
        var latest = AnimalHistoryNoteFormatter.GetLatestOwner(Parse(Item("20260102", "NEW", "NEW_ADDRESS"), Item("20260101", "OLD", "OLD_ADDRESS")));
        Program.Check(latest == new AnimalHistoryOwner("NEW", "NEW_ADDRESS"), "latest date selects a name/address pair regardless of response order");
        Program.Check(AnimalHistoryNoteFormatter.GetLatestOwner(Parse(Item("20260102", "NEW", ""))) == new AnimalHistoryOwner("NEW", ""), "missing address retains its own owner, never an older address");
        Program.Check(AnimalHistoryNoteFormatter.GetLatestOwner(Parse()) == null, "empty history has no inferred owner");
        foreach (bool reverse in new[] { false, true })
        {
            var reports = new[] { Item("20260102", "FIRST", "FIRST_ADDRESS"), Item("20260102", "SECOND", "SECOND_ADDRESS") };
            Program.Check(AnimalHistoryNoteFormatter.GetLatestOwner(Parse(reverse ? reports.Reverse().ToArray() : reports)) == null,
                "equal latest dates never choose an owner based on response order");
        }
        var duplicate = Item("20260102", "NEW", "ADDRESS");
        Program.Check(Parse(duplicate, duplicate).Entries.Count == 1, "identical reports are deduplicated");
        var conflicting = Parse(duplicate, Item("20260102", "NEW", "OTHER_ADDRESS"));
        Program.Check(conflicting.Entries.Count == 2 && AnimalHistoryNoteFormatter.GetLatestOwner(conflicting) == null, "different addresses are not discarded as duplicates");
        var cow = new gValues { OwnerName = "AUCTION_OWNER", Location = "AUCTION_REGION", EntityNumber = "1234 5678 9", MatherEntityNumber = "9876 5432 1" };
        using var model = new AuctionContPanelViewModel(cow, null!, new VirtualizingStackPanel(), new List<int> { 5 }, 1);
        var view = new CowHistorySearchRun { DataContext = model, Width = 128, Height = 128 };
        using var window = new HwndSource(new HwndSourceParameters("Offline farm address checks") { Width = 128, Height = 128, WindowStyle = unchecked((int)0x80000000) });
        window.RootVisual = view;
        Layout();
        var owner = (BidderNameFlow)view.FindName("historyOwner");
        var address = (BidderNameFlow)view.FindName("historyAddress");
        Program.Check(owner.Text == "정보 없음" && address.Text == "정보 없음", "history view never falls back to auction owner or shipping region");
        var updated = cow.Clone();
        updated.HistoryOwner = new AnimalHistoryOwner("가나다라마바사아자차", "전남광주통합특별시 가상군 가상면 테스트주소");
        var tracker = new DisplayUpdateTracker();
        tracker.ShouldUpdate("fixture", cow, 10);
        Program.Check(tracker.ShouldUpdate("fixture", updated, 10), "address-only enrichment triggers a display update");
        model.UpdateCowInfo(updated); Layout();
        Program.Check(owner.Text == updated.HistoryOwner.Name && address.Text == "가상군 가상면 테스트주소"
            && model.ItemDisplay.Snapshot.HistoryOwner!.FarmAddress == updated.HistoryOwner.FarmAddress,
            "live third-page bindings omit an unlisted special city only in display and preserve the source owner/address pair");
        Program.Check(address.ActualWidth > 0 && address.ActualWidth < 128 && Scrolling(address), "long address scrolls inside its allocated width");
        Program.Check(Scrolling(owner), "long history owner name also scrolls");
        var same = updated.Clone();
        Program.Check(same.Equals(updated) && same.GetHashCode() == updated.GetHashCode() && same.UpdateSignature() == updated.UpdateSignature(), "history pair is included consistently in snapshot copying and comparison");
        Program.Check(!tracker.ShouldUpdate("fixture", same, 10), "unchanged history pair avoids redundant rendering");
        same.HistoryOwner = same.HistoryOwner! with { FarmAddress = "가상지" };
        Program.Check(!same.Equals(updated) && tracker.ShouldUpdate("fixture", same, 10), "changed address without changed note still updates the display");
        model.UpdateCowInfo(same); Layout();
        Program.Check(address.Text == "가상지" && !Scrolling(address), "short replacement address stops its animation");
        same = same.Clone(); same.HistoryOwner = new AnimalHistoryOwner("가상농가", "");
        model.UpdateCowInfo(same); Layout();
        Program.Check(owner.Text == "가상농가" && address.Text == "정보 없음", "known owner and absent address are represented separately");
        model.UpdateCowInfo(cow); Layout();
        Program.Check(owner.Text == "정보 없음" && address.Text == "정보 없음" && !Scrolling(address), "removing history or changing animal clears the prior pair");
        Program.Check(!JsonConvert.SerializeObject(updated).Contains("HistoryOwner")
            && !System.Text.Json.JsonSerializer.Serialize(updated).Contains("HistoryOwner"), "derived address is excluded from general auction serialization");
        Program.Check(!AuctionItemFields.All.Contains(nameof(gValues.HistoryOwner)), "API/SV field replacement excludes derived owner/address");
        var itemStore = new CowAuctionSmall.Application.Auction.AuctionItemStore();
        var canonical = updated.Clone(); canonical.SipNumber = "1"; canonical.TraceNumber = Number;
        itemStore.Bind(new List<gValues> { canonical });
        var priceChange = canonical.Clone(); priceChange.LowestPrice = "612"; priceChange.HistoryOwner = null;
        itemStore.Apply(new AuctionItemUpdate(priceChange, new[] { nameof(gValues.LowestPrice) }, "", ""), false);
        Program.Check(canonical.HistoryOwner == updated.HistoryOwner, "partial SV update keeps the same animal's history pair");
        var incoming = priceChange.Clone(); itemStore.MergeAbsentFields(incoming);
        Program.Check(incoming.HistoryOwner == updated.HistoryOwner, "API replacement preserves history for the same animal");
        var replacement = priceChange.Clone(); replacement.TraceNumber = "410000000000002";
        itemStore.Apply(new AuctionItemUpdate(replacement, new[] { nameof(gValues.TraceNumber) }, "", ""), false);
        Program.Check(canonical.HistoryOwner == null, "reusing a slot for a different animal clears its old owner/address");
        window.RootVisual = null; Layout();
        Program.Check(Animation(address) == null && Animation(owner) == null, "leaving the history page releases both animations");

        object? Animation(BidderNameFlow flow) => typeof(BidderNameFlow).GetField("_flowTextAnimation", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(flow);
        bool Scrolling(BidderNameFlow flow) => Animation(flow) is object animation && (bool)animation.GetType().GetField("_isAnimating", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animation)!;
        void Layout()
        {
            view.Measure(new Size(128, 128)); view.Arrange(new Rect(0, 0, 128, 128)); view.UpdateLayout();
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }
    }

    internal static async Task RunAsync()
    {
        var store = new Store();
        var parsed = Parse(Item("20260101", "OLD", "OLD_ADDRESS"), Item("20260102", "NEW", "NEW_ADDRESS"));
        var legacy = JsonConvert.SerializeObject(new AnimalHistoryResult { Version = 1, Entries = parsed.Entries });
        await store.SaveCacheAsync("animal_history", Number, legacy, store.Epoch);
        var client = new Client(parsed);
        using (var service = new AnimalHistoryService(store, client))
        {
            service.Configure(new AnimalTraceSettings { Enabled = true, ShowOwnerChangeNote = false });
            Program.Check(!(await service.GetCachedNoteAsync(Number)).HasValue, "old address-less cache must not prevent an API lookup");
            var result = await service.LookupAsync(Number).WaitAsync(Timeout);
            Program.Check(client.Calls == 1 && result.LatestOwner == new AnimalHistoryOwner("NEW", "NEW_ADDRESS"), "new lookup replaces legacy cache and carries latest owner/address");
            Program.Check(result.Note == "OLD → NEW(양수 2026-01-02)", "existing ownership-change note remains unchanged");
            Program.Check((await service.GetCachedNoteAsync(Number)).LatestOwner == result.LatestOwner && client.Calls == 1, "memory cache carries the same pair without another request");
        }
        var stored = await store.GetCacheAsync("animal_history", Number);
        Program.Check(stored!.Contains("FarmAddress") && stored.Contains("NEW_ADDRESS"), "daily history payload persists structured addresses");
        var failing = new Client(new AnimalHistoryResult { Status = HistoryStatus.InvalidResponse });
        using (var service = new AnimalHistoryService(store, failing))
        {
            service.Configure(new AnimalTraceSettings { Enabled = true, ShowOwnerChangeNote = false });
            var disk = await service.GetCachedNoteAsync(Number);
            Program.Check(disk.LatestOwner?.FarmAddress == "NEW_ADDRESS" && failing.Calls == 0, "restart reads the new address from daily cache without HTTP");
            var fallback = await service.LookupAsync(Number, true).WaitAsync(Timeout);
            Program.Check(fallback.HasValue && fallback.Outcome == HistoryLookupOutcome.Failed && fallback.LatestOwner == disk.LatestOwner,
                "failed refresh preserves the complete previous name/address pair");
            store.Advance();
            Program.Check(!(await service.GetCachedNoteAsync(Number)).HasValue, "day rollover clears owner/address from memory and persistent cache");
        }
        var queue = new ConcurrentQueue<Action>();
        var supplied = new HistoryLookupResult(true, "NOTE", HistoryLookupOutcome.Success) { LatestOwner = new AnimalHistoryOwner("OWNER", "ADDRESS") };
        using var batch = new AnimalHistoryCoordinator((_, _, _) => Task.FromResult(supplied), null, () => store.Epoch, queue.Enqueue, _ => { });
        batch.UpdateTargets(new[] { Number });
        await HistoryTestUi.Until(() => !queue.IsEmpty);
        Program.Check(!batch.TryGetHistory(Number, out _) && !batch.Completion.IsCompleted, "owner/address wait for their item callback on the UI dispatcher");
        await HistoryTestUi.Complete(batch.Completion, queue);
        Program.Check(batch.TryGetHistory(Number, out var committed) && committed.LatestOwner == supplied.LatestOwner && committed.Note == "NOTE", "one item callback publishes its note and owner/address together");
        supplied = new HistoryLookupResult(true, "", HistoryLookupOutcome.Success);
        batch.UpdateTargets(new[] { Number }, true); await HistoryTestUi.Complete(batch.Completion, queue);
        Program.Check(batch.TryGetHistory(Number, out committed) && committed.LatestOwner == null && committed.Note == "", "valid empty or ambiguous result removes previously displayed history");
        batch.UpdateTargets(Array.Empty<string>());
        Program.Check(!batch.TryGetHistory(Number, out _), "removed animals cannot retain visible owner/address");
        batch.UpdateTargets(new[] { Number }); await HistoryTestUi.Until(() => !queue.IsEmpty);
        store.Advance(); await HistoryTestUi.Complete(batch.Completion, queue);
        Program.Check(!batch.TryGetHistory(Number, out _), "late previous-day item cannot restore owner/address");
    }

    private sealed class Client(AnimalHistoryResult result) : IAnimalHistoryClient
    {
        public int Calls;
        public Task<AnimalHistoryResult> FetchAnimalHistoryAsync(AnimalTraceSettings settings, string number, CancellationToken token)
        { Calls++; return Task.FromResult(result); }
        public void Dispose() { }
    }
    private sealed class Store : IDailyCacheStore
    {
        private readonly ConcurrentDictionary<string, string> _values = new();
        private int _day;
        public string Epoch => _day.ToString();
        public event Action? DayChanged;
        public event Action? Unavailable { add { } remove { } }
        public event Action? CleanupIncomplete { add { } remove { } }
        public Task<bool> EnsureTodayAsync() => Task.FromResult(true);
        public Task<string?> GetCacheAsync(string type, string key) => Task.FromResult(_values.TryGetValue(type + key, out var value) ? value : null);
        public Task<bool> SaveCacheAsync(string type, string key, string json, string epoch)
        {
            if (epoch != Epoch) return Task.FromResult(false);
            _values[type + key] = json; return Task.FromResult(true);
        }
        public void Advance() { _values.Clear(); _day++; DayChanged?.Invoke(); }
    }
}
