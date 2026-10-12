using CowAuctionSmall.Domain.AnimalHistory;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views.Size128_128.Running;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Threading;

internal static class HistoryItemDisplayChecks
{
    private const string A = "410000000000001";
    private const string B = "410000000000002";
    private const string C = "410000000000003";

    internal static void Run()
    {
        _ = System.Windows.Application.Current ?? new System.Windows.Application
            { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
        SexDisplayChecks.EnsureResources();
        using var fixture = new Fixture();
        fixture.Initialize(Cow("1", A), Cow("2", A, "23"), Cow("3", B));
        fixture.Coordinator.UpdateTargets(new[] { A, B });
        fixture.WaitUntil(() => fixture.Calls == 2);

        var latest = fixture.Visible["1"].Clone();
        latest.LowestPrice = "1200";
        latest.BidPrice = "1500";
        fixture.Model.UpdatePanels(new ObservableCollection<gValues> { latest });
        var originals = fixture.Visible.ToDictionary(pair => pair.Key, pair => pair.Value);
        var hosts = fixture.Panels.SelectMany(pair => pair.Value).Select(Host).ToArray();
        var unrelatedDisplay = hosts[3].ItemDisplay;
        fixture.Respond(A, Result("FIRST"));
        fixture.WaitUntil(() => fixture.Updates.Count == 1);

        Program.Check(!fixture.Coordinator.Completion.IsCompleted,
            "an individual result reaches actual display while another API response is pending");
        Program.Check(fixture.Visible["1"] != originals["1"] && fixture.Visible["2"] != originals["2"]
            && ReferenceEquals(fixture.Visible["3"], originals["3"]),
            "individual history clones only matching current logical slots");
        Program.Check(hosts.Take(3).All(host => host.ItemDisplay!.Snapshot.NoteChangeOwner == "FIRST")
            && ReferenceEquals(hosts[3].ItemDisplay, unrelatedDisplay),
            "duplicate physical slots and the same animal in another result state update without touching unrelated display");
        Program.Check(fixture.Visible["1"].LowestPrice == "1200" && fixture.Visible["1"].BidPrice == "1500"
            && fixture.Visible["1"].AuctionResultStatus == "22" && fixture.Visible["2"].AuctionResultStatus == "23"
            && fixture.Visible.Values.All(cow => cow.Note == "ORIGINAL_NOTE"),
            "lookup completion preserves newer auction prices, sold/unsold state and original notes");
        Program.Check(fixture.Panels.SelectMany(pair => pair.Value).Select(Host).SequenceEqual(hosts),
            "individual history updates reuse existing view hosts");

        var applied = fixture.Visible["1"];
        var display = hosts[0].ItemDisplay;
        fixture.Apply(fixture.Updates[0]);
        Program.Check(ReferenceEquals(fixture.Visible["1"], applied) && ReferenceEquals(hosts[0].ItemDisplay, display),
            "identical history skips item copies and display notifications");
        fixture.Respond(B, Result("OTHER"));
        fixture.Complete();
        Program.Check(fixture.Calls == 2, "history publication does not enqueue additional API lookups");

        fixture.PrepareRefresh();
        fixture.SetNoteVisibility(false);
        fixture.Coordinator.UpdateTargets(new[] { A, B }, refresh: true);
        fixture.WaitUntil(() => fixture.Calls == 4);
        fixture.Respond(A, Result("HIDDEN", "NEW_OWNER", "NEW_ADDRESS"));
        fixture.Respond(B, Result("OTHER"));
        fixture.Complete();
        Program.Check(hosts.Take(3).All(host => host.ItemDisplay!.Snapshot.NoteChangeOwner == ""
            && host.ItemDisplay.Snapshot.Note == "ORIGINAL_NOTE"
            && host.ItemDisplay.HistoryOwnerNameText == "NEW_OWNER"
            && host.ItemDisplay.HistoryFarmAddressText == "NEW_ADDRESS"),
            "hidden owner-change note still updates owner and address through the same single-item path");
        applied = fixture.Visible["1"];
        fixture.Apply(fixture.Updates[0]);
        Program.Check(ReferenceEquals(fixture.Visible["1"], applied),
            "an earlier refresh publication cannot overwrite a newer displayed result");

        fixture.PrepareRefresh();
        fixture.Coordinator.UpdateTargets(new[] { A, B }, refresh: true);
        fixture.WaitUntil(() => fixture.Calls == 6);
        var beforeFailure = fixture.Visible["3"];
        fixture.Respond(A, new HistoryLookupResult(true, "", HistoryLookupOutcome.Success));
        fixture.Respond(B, HistoryLookupResult.Missing);
        fixture.Complete();
        Program.Check(hosts.Take(3).All(host => host.ItemDisplay!.Snapshot.HistoryOwner == null
            && host.ItemDisplay.Snapshot.NoteChangeOwner == ""),
            "a successful empty history clears prior owner and note values");
        Program.Check(ReferenceEquals(fixture.Visible["3"], beforeFailure)
            && hosts[3].ItemDisplay!.HistoryOwnerNameText == "OWNER",
            "a failed lookup retains the existing valid display");

        var currentUpdate = fixture.Updates.Last(update => update.Number == A);
        var replacement = fixture.Visible["1"].Clone();
        replacement.TraceNumber = C;
        replacement.HistoryOwner = new AnimalHistoryOwner("REPLACEMENT", "REPLACEMENT_ADDRESS");
        fixture.Visible["1"] = replacement;
        fixture.Apply(currentUpdate);
        Program.Check(ReferenceEquals(fixture.Visible["1"], replacement)
            && fixture.Visible["1"].HistoryOwner?.Name == "REPLACEMENT",
            "UI publication rechecks actual slot identity even before the target subscription changes");

        fixture.SetEnabled(false);
        fixture.Visible["2"].HistoryOwner = new AnimalHistoryOwner("PRESERVED", "ADDRESS");
        fixture.Apply(currentUpdate);
        Program.Check(fixture.Visible["2"].HistoryOwner?.Name == "PRESERVED",
            "disabled history ignores delayed individual publications");
        CheckRunningPages(false);
        CheckRunningPages(true);
    }

    private static void CheckRunningPages(bool isRunning)
    {
        using var fixture = new Fixture(pages: 3);
        var first = Cow("1", A, "11");
        first.IsRunning = isRunning;
        fixture.Initialize(first, Cow("2", B, "11"));
        fixture.SetNoteVisibility(false);
        var allPanels = fixture.Panels.SelectMany(pair => pair.Value).ToArray();
        for (int i = 0; i < allPanels.Length; i++)
        {
            var panel = allPanels[i];
            fixture.Selector.DisplayRunningPageNum(panel, fixture.Visible[panel.Name.Substring(4)], i == 0 ? 2 : 3);
            ((AuctionContPanelViewModel)panel.DataContext).SetNotePosition("fixture", -12);
        }
        var hosts = allPanels.Select(Host).ToArray();
        var pages = hosts.Select(host => host.PageContent).ToArray();
        var models = allPanels.Select(panel => panel.DataContext).ToArray();
        Set(fixture.Selector, "_rotationIndex", 2);
        Set(fixture.Selector, "_lastRunningStartUtc", DateTime.MinValue);
        Set(fixture.Selector, "_lockFirstPageUntilRefresh", false);
        fixture.Coordinator.UpdateTargets(new[] { A, B });
        fixture.WaitUntil(() => fixture.Calls == 2);
        fixture.Respond(A, Result("HIDDEN_RUNNING_NOTE"));
        fixture.WaitUntil(() => fixture.Updates.Count == 1);
        Program.Check(hosts.Select(host => host.PageContent).SequenceEqual(pages)
            && allPanels.Select(panel => panel.DataContext).SequenceEqual(models),
            (isRunning ? "running" : "ready") + " history keeps the current second/third page and its view model");
        Program.Check(!(bool)Field(fixture.Selector, "_lockFirstPageUntilRefresh")!
            && (int)Field(fixture.Selector, "_rotationIndex")! == 2
            && (DateTime)Field(fixture.Selector, "_lastRunningStartUtc")! == DateTime.MinValue,
            "history publication does not reissue auction-start/page-lock control");
        Program.Check(models.Cast<AuctionContPanelViewModel>().All(model => model.GetNotePosition("fixture") == -12),
            "address-only history update preserves note scroll positions");
        Program.Check(hosts.Take(2).All(host => host.ItemDisplay!.HistoryOwnerNameText == "OWNER")
            && allPanels.Take(2).All(panel => fixture.LastData(panel).HistoryOwner?.Name == "OWNER"),
            "running display models and panel last-data snapshots receive the same individual history");
        fixture.Model.UpdatePanels(new ObservableCollection<gValues> { fixture.Visible["1"].Clone() });
        Program.Check(hosts.Select(host => host.PageContent).SequenceEqual(pages),
            "repeated unchanged auction data after history publication remains suppressed by the display tracker");
        fixture.Respond(B, Result("HIDDEN_OTHER_NOTE"));
        fixture.Complete();
    }

    private static gValues Cow(string slot, string number, string state = "22") => new()
    {
        SpaceIndex = slot, SipNumber = slot, TraceNumber = number, CowDistinction = "1", AuctionResultStatus = state,
        Note = "ORIGINAL_NOTE", Sex = "수", LowestPrice = "1000", BidPrice = "1100"
    };

    private static HistoryLookupResult Result(string note, string owner = "OWNER", string address = "ADDRESS") =>
        new(true, note, HistoryLookupOutcome.Success) { LatestOwner = new AnimalHistoryOwner(owner, address) };

    private static RunningNoteHost128 Host(VirtualizingStackPanel panel) => (RunningNoteHost128)panel.Children[0];
    private static void Set(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    private static object? Field(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(target);

    private sealed class Fixture : IDisposable
    {
        internal readonly MainWindowViewModel Model;
        internal readonly DisplaySelect Selector;
        internal readonly AnimalHistoryCoordinator Coordinator;
        internal readonly Dictionary<string, gValues> Visible = new();
        internal readonly Dictionary<string, List<VirtualizingStackPanel>> Panels = new();
        internal readonly List<HistoryItemUpdate> Updates = new();
        private readonly ConcurrentQueue<Action> _ui = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<HistoryLookupResult>> _responses = new();
        private readonly IDisposable _store;
        private int _calls;
        internal int Calls => Volatile.Read(ref _calls);

        internal Fixture(int pages = 1)
        {
            var running = new ObservableCollection<AuctionContPanelViewModel>();
            var storeType = typeof(DisplaySelect).Assembly.GetType("CowAuctionSmall.Presentation.Display.DisplayPanelStore", true)!;
            _store = (IDisposable)Activator.CreateInstance(storeType, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { running }, null)!;
            var selector = (DisplaySelect)RuntimeHelpers.GetUninitializedObject(typeof(DisplaySelect));
            Selector = selector;
            Set(selector, "_displaySize", DisplaySizeParser.Parse("128,128"));
            Set(selector, "_nhCode", pages == 3 ? "8808990657202" : "fixture");
            Set(selector, "_setCustomDisplay", new SetCustomDisplay());
            Set(selector, "_panelStates", _store);
            Set(selector, "_userInfo", new UserInfo { Auction = new Auction { IsShowQQuri = "N" } });
            Set(selector, "<TodayAuctionItems>k__BackingField", running);
            Set(selector, "pageTime", new List<int> { 5, 5, 5 });
            Set(selector, "_requestedRunningPage", pages);
            Set(selector, "_totalRunningPage", pages);
            Model = (MainWindowViewModel)RuntimeHelpers.GetUninitializedObject(typeof(MainWindowViewModel));
            Set(Model, "_displaySelect", selector);
            Set(Model, "_dispatcher", Dispatcher.CurrentDispatcher);
            Set(Model, "_panelsByName", Panels);
            Set(Model, "_visibleCows", Visible);
            Set(Model, "_displayUpdates", new DisplayUpdateTracker());
            SetNoteVisibility(true);
            Coordinator = new AnimalHistoryCoordinator((number, _, token) =>
            {
                var response = _responses.GetOrAdd(number, key => new(TaskCreationOptions.RunContinuationsAsynchronously));
                Interlocked.Increment(ref _calls);
                return response.Task.WaitAsync(token);
            }, _ => Task.FromResult(HistoryLookupResult.Missing), () => "fixture", _ui.Enqueue,
            update => { Updates.Add(update); Apply(update); });
            Set(Model, "_historyCoordinator", Coordinator);
        }

        internal void Initialize(params gValues[] cows)
        {
            foreach (var cow in cows)
            {
                var name = "Cow_" + cow.SpaceIndex;
                Panels[name] = new() { new VirtualizingStackPanel { Name = name, Width = 128, Height = 128 } };
            }
            Panels["Cow_1"].Add(new VirtualizingStackPanel { Name = "Cow_1", Width = 128, Height = 128 });
            Set(Selector, "_lastRunningStartKey", cows[0].SipNumber);
            Set(Selector, "_lastRunningStartUtc", DateTime.UtcNow);
            Model.UpdatePanels(new ObservableCollection<gValues>(cows));
            SetEnabled(true);
        }

        internal void SetNoteVisibility(bool enabled) => Set(Model, "_showOwnerChangeNote", enabled);
        internal void SetEnabled(bool enabled) => Set(Model, "_historyReady", enabled);
        internal gValues LastData(VirtualizingStackPanel panel)
        {
            var state = _store.GetType().GetMethod("Get", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(_store, new object[] { panel })!;
            return (gValues)Field(state, "LastData")!;
        }
        internal void Apply(HistoryItemUpdate update) => typeof(MainWindowViewModel)
            .GetMethod("ApplyHistoryResult", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Model, new object[] { update });
        internal void Respond(string number, HistoryLookupResult result) => _responses[number].SetResult(result);
        internal void PrepareRefresh() => _responses.Clear();
        internal void Complete()
        {
            WaitUntil(() => Coordinator.Completion.IsCompleted);
            Coordinator.Completion.GetAwaiter().GetResult();
        }
        internal void WaitUntil(Func<bool> predicate)
        {
            var timer = Stopwatch.StartNew();
            while (!predicate())
            {
                while (_ui.TryDequeue(out var action)) action();
                if (timer.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Individual history display fixture stalled.");
                Thread.Sleep(1);
            }
            while (_ui.TryDequeue(out var action)) action();
        }
        public void Dispose()
        {
            Coordinator.Dispose();
            _store.Dispose();
        }
    }
}
