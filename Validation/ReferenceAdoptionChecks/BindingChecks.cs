using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Views.Common;
using CowAuctionSmall.Views.Size128_128.Running;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

internal static class BindingChecks
{
    private static readonly string[] DisplayFields = { "EntityNumber", "LowestPrice", "BidPrice", "OwnerName", "Location", "Weight", "KPN", "Nh_ability_1_num", "Reproduction_Sujung_KPN", "bodyWeightInColdNum", "Child_EntityNumber", "brucellosisTestDate", "Birth", "BodyLength", "BodyHeight", "BodyWidth", "CrossSectionalArea" };

    internal static void Run(bool noteHostsOnly = false)
    {
        var application = System.Windows.Application.Current ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        SexDisplayChecks.EnsureResources();
        application.Resources["EntityNumberForeground"] = Brushes.Yellow;
        application.Resources["EntityNumberShortForeground"] = Brushes.White;
        application.Resources["EntityNumberShortBackground"] = Brushes.Black;
        application.Resources["LocationForeground"] = Brushes.Lime;
        application.Resources["EpdGradeForeground"] = Brushes.Yellow;
        var trace = PresentationTraceSources.DataBindingSource;
        var previousLevel = trace.Switch.Level;
        using var errors = new BindingErrors();
        trace.Listeners.Add(errors);
        PresentationTraceSources.Refresh();
        trace.Switch.Level = SourceLevels.Error;
        try
        {
            var invalid = new TextBlock { DataContext = Sample() };
            invalid.SetBinding(TextBlock.TextProperty, new Binding("SyntheticMissingProperty"));
            using (var probe = Host(invalid))
            {
                Program.Check(errors.Text.Contains("SyntheticMissingProperty", StringComparison.Ordinal), "binding diagnostic listener detects an intentionally invalid path");
                probe.RootVisual = null;
            }
            errors.Clear();
            CheckHaenamPaternity(errors);
            CheckHaenamNoteIntegration(errors);
            if (noteHostsOnly)
            {
                CheckSharedControls(errors);
                CheckResultBoundary(errors);
                CheckBorderRepeat(errors);
                CheckPilotBorders(errors);
                return;
            }
            ItemDisplayChecks.Run();
            SexDisplayChecks.Run();
            HistoryProgressUiChecks.Run();
            CheckModel();
            CheckBatchNotifications();
            Type[] controls = typeof(AuctionContPanelViewModel).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && typeof(UserControl).IsAssignableFrom(type)
                    && type.Namespace?.StartsWith("CowAuctionSmall.Views", StringComparison.Ordinal) == true).ToArray();
            Type[] running = controls.Where(type => (type.Namespace!.Contains(".Running", StringComparison.Ordinal) || type.Name is "AuctionRunning2" or "AuctionRunning3")
                && !type.Name.StartsWith("RunningNoteHost", StringComparison.Ordinal) && type.Name != "AuctionRunning2_320_64").OrderBy(type => type.FullName).ToArray();
            string[] requiredRunning = { "CowHistorySearchRun", "SokchoRunning1_64", "SokchoRunning2_64", "SokchoEventRun_64" };
            string runningNames = string.Join(", ", running.Select(type => type.FullName));
            string missingRunning = string.Join(", ", requiredRunning.Where(name => !running.Any(type => type.Name == name)));
            Console.WriteLine($"Running reflection inventory: {running.Length}; {runningNames}; missing required: {missingRunning}");
            Program.Check(running.Length == 36 && missingRunning.Length == 0,
                $"binding coverage expected 36 running pages and required views; actual={running.Length}; missing=[{missingRunning}]; actual views=[{runningNames}]");
            var failures = new List<Exception>();
            foreach (Type type in running)
            {
                try { CheckRunning(type, errors); }
                catch (Exception error) { failures.Add(new InvalidOperationException(type.FullName, error)); }
            }
            Type[] results = controls.Where(type => !type.Namespace!.Contains(".Running", StringComparison.Ordinal)
                && (type.Name.Contains("Sold", StringComparison.OrdinalIgnoreCase))).OrderBy(type => type.FullName).ToArray();
            Program.Check(results.Length >= 25, "binding coverage includes all result view families");
            foreach (Type type in results)
            {
                try { CheckResult(type, errors); }
                catch (Exception error) { failures.Add(new InvalidOperationException(type.FullName, error)); }
            }
            try { CheckResult(typeof(CowAuctionSmall.Views.Size128_64.SokchoEventResult_64), errors); }
            catch (Exception error) { failures.Add(new InvalidOperationException("SokchoEventResult_64", error)); }
            try { CheckSharedControls(errors); }
            catch (Exception error) { failures.Add(new InvalidOperationException("Shared controls", error)); }
            try { CheckResultBoundary(errors); }
            catch (Exception error) { failures.Add(new InvalidOperationException("Result boundary", error)); }
            Console.WriteLine($"Binding coverage: {running.Length} running pages, {results.Length} result views, shared note host in both modes and common badge; generic Application, synthetic data, no product App or DisplaySelect/network startup.");
            if (failures.Count > 0) throw new AggregateException("Binding failures", failures);
        }
        finally { trace.Listeners.Remove(errors); trace.Switch.Level = previousLevel; }
    }

    private static gValues Sample() => new()
    {
        TraceNumber = "000000000000001", SipNumber = "17", SpaceIndex = "1", EntityNumber = "1234 5678 9",
        LowestPrice = "612", BidPrice = "777", Weight = "444", KPN = "KPN1234",
        OwnerName = "SYNTHETIC_OWNER", Location = "SYNTHETIC_AREA", Sex = "암", CowDistinction = "1",
        Birth = "26.01.02", BirthMonth = "8", MotherLevel = "혈통", RegistrationCategory = "혈통", Blood = "2",
        CalvingNumber = "1", Pregnant = "0", Bidder = "SYNTHETIC_BIDDER", BidderName = "Y", BidderNum = "42",
        Note = "SYNTHETIC_NOTE", NoteChangeOwner = "SYNTHETIC_HISTORY", AuctionResultStatus = "11",
        Nh_ability_1_num = "123", Reproduction_Sujung_KPN = "KPN4567", Is_Nh_QQuri = "Y", bodyWeightInColdNum = "234", brucellosisTestDate = "2026-01-02",
        PaternityMatch = "MATCH", FrontNoteWord = "MATCH", FrontNoteWordBrush = "Yellow", BodyLength = "121", BodyHeight = "122", BodyWidth = "123", CrossSectionalArea = "124"
    };

    private static AuctionContPanelViewModel Model(gValues cow) => new(cow, null!, new VirtualizingStackPanel(), new List<int> { 5 }, 1);

    private static void CheckModel()
    {
        var source = Sample();
        using var model = Model(source);
        Program.Check(!ReferenceEquals(source, model.CowInfo), "view model owns a display snapshot");
        var notifications = new List<string?>();
        model.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        var snapshot = model.CowInfo;
        var itemDisplay = model.ItemDisplay;
        model.UpdateCowInfo(source);
        Program.Check(ReferenceEquals(snapshot, model.CowInfo) && notifications.Count == 0, "equal source avoids snapshot replacement and notifications");
        Program.Check(ReferenceEquals(itemDisplay, model.ItemDisplay), "equal refresh reuses precomputed display values");
        source.LowestPrice = "912";
        Program.Check(model.CowInfo.LowestPrice == "612", "source mutation cannot silently modify rendered snapshot");
        model.UpdateCowInfo(source);
        Program.Check(model.CowInfo.LowestPrice == "912" && !ReferenceEquals(snapshot, model.CowInfo), "same source refresh replaces snapshot");
        Program.Check(model.ItemDisplay.LowestPriceText == "912" && notifications.Contains(nameof(model.ItemDisplay)), "price refresh publishes display values and notifies bindings");
        model.SetNotePosition("page", -30);
        model.UpdateCowInfo(source.Clone());
        Program.Check(model.GetNotePosition("page") == -30, "equal refresh preserves note position");
        notifications.Clear();
        source.NoteChangeOwner = "CHANGED_HISTORY";
        model.UpdateCowInfo(source);
        Program.Check(notifications.Contains(nameof(model.NoteChangeOwner)) && notifications.Contains(nameof(model.DisplayNote))
            && model.GetNotePosition("page") == null, "same source history mutation notifies and resets note scroll");
        model.Note = "SETTER_NOTE";
        Program.Check(model.CowInfo.Note == "SETTER_NOTE" && source.Note == "SYNTHETIC_NOTE", "note setter updates owned snapshot without mutating source");
        var exposed = model.CowInfo;
        exposed.LowestPrice = "913";
        model.UpdateCowInfo(exposed);
        Program.Check(!ReferenceEquals(exposed, model.CowInfo) && model.CowInfo.LowestPrice == "913", "explicit exposed-reference refresh publishes a new binding source");
        model.SetNotePosition("page", -40);
        notifications.Clear();
        exposed = model.CowInfo;
        exposed.Note = "EXPOSED_NOTE"; exposed.NoteChangeOwner = "EXPOSED_HISTORY";
        model.UpdateCowInfo(exposed);
        Program.Check(notifications.Contains(nameof(model.Note)) && notifications.Contains(nameof(model.NoteChangeOwner))
            && notifications.Contains(nameof(model.DisplayNote)) && model.GetNotePosition("page") == null,
            "exposed-reference note mutation compares last notified text and resets scroll");
        notifications.Clear();
        model.IsRunning = true;
        Program.Check(notifications.Contains(nameof(model.IsRunning)), "state-only change notifies independently of item data");
    }

    private static void CheckBatchNotifications()
    {
        gValues[] items = Enumerable.Range(0, 501).Select(_ => Sample()).ToArray();
        AuctionContPanelViewModel[] models = items.Select(Model).ToArray();
        int notifications = 0;
        foreach (var model in models) model.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(model.CowInfo)) notifications++; };
        try
        {
            for (int scenario = 0; scenario < 3; scenario++)
            {
                if (scenario == 1) items[0].LowestPrice = "813";
                if (scenario == 2) foreach (var item in items) item.LowestPrice = "914";
                notifications = 0;
                var prior = models.Select(model => model.CowInfo).ToArray();
                long allocated = GC.GetAllocatedBytesForCurrentThread();
                var timer = Stopwatch.StartNew();
                for (int index = 0; index < models.Length; index++) models[index].UpdateCowInfo(items[index]);
                double elapsed = timer.Elapsed.TotalMilliseconds;
                allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
                int replacements = models.Where((model, index) => !ReferenceEquals(model.CowInfo, prior[index])).Count();
                int expected = scenario == 0 ? 0 : scenario == 1 ? 1 : 501;
                Program.Check(notifications == expected && replacements == expected, $"501 display snapshots scenario {scenario}: only actual changes notify and replace");
                Console.WriteLine($"Binding VM comparison: scenario={scenario}; inputs=501; notifications={notifications}; replacements={replacements}; elapsed_ms={elapsed:F2}; allocated_bytes={allocated}; excludes actual screen rendering.");
            }
        }
        finally { foreach (var model in models) model.Dispose(); }
    }

    private static void CheckRunning(Type type, BindingErrors errors)
    {
        errors.Clear();
        var source = Sample();
        using var model = Model(source);
        var view = (UserControl)Activator.CreateInstance(type)!;
        if (type.Name.StartsWith("Sokcho", StringComparison.Ordinal)) { view.Width = 128; view.Height = 64; }
        view.DataContext = model;
        using var host = Host(view);
        Program.Check(ReferenceEquals(view.DataContext, model), "running root keeps VM: " + type.Name);
        AssertValues(view, model.CowInfo, type.Name);
        AssertNoErrors(errors, type.Name + " initial");
        var changed = source.Clone();
        changed.EntityNumber = "2345 6789 0"; changed.LowestPrice = "1812"; changed.Weight = "1555"; changed.Pregnant = "4";
        changed.OwnerName = "CHANGED_OWNER"; changed.Location = "CHANGED_AREA"; changed.KPN = "KPN9876";
        changed.Nh_ability_1_num = "234"; changed.Reproduction_Sujung_KPN = "KPN7654";
        changed.bodyWeightInColdNum = "345"; changed.brucellosisTestDate = "2026-02-03";
        changed.BodyLength = "221"; changed.BodyHeight = "222"; changed.BodyWidth = "223"; changed.CrossSectionalArea = "224";
        changed.Birth = "26.02.03";
        model.UpdateCowInfo(changed); Pump();
        AssertValues(view, model.CowInfo, type.Name + " replaced");
        changed.EntityNumber = "3456 7890 1"; changed.LowestPrice = "2913"; changed.Weight = "1666"; changed.Pregnant = "X";
        changed.Note = "CHANGED_NOTE"; changed.NoteChangeOwner = "CHANGED_HISTORY";
        changed.bodyWeightInColdNum = "456"; changed.Nh_ability_1_num = "345";
        changed.BodyLength = "321"; changed.BodyHeight = "322"; changed.BodyWidth = "323"; changed.CrossSectionalArea = "324";
        model.UpdateCowInfo(changed); Pump();
        AssertValues(view, model.CowInfo, type.Name + " same source");
        changed.LowestPrice = "결장"; changed.Weight = ""; changed.Pregnant = "0";
        model.UpdateCowInfo(changed); Pump();
        AssertValues(view, model.CowInfo, type.Name + " absent price/empty weight/pregnancy cleared");
        foreach (TextBlock note in Nodes(view).OfType<TextBlock>().Where(text => BindingOperations.IsDataBound(text, NoteText.NormalProperty)))
            Program.Check(note.Text.Contains("CHANGED_NOTE") && note.Text.Contains("CHANGED_HISTORY"), "embedded note/history refresh: " + type.Name);
        var itemSnapshot = model.CowInfo;
        model.IsRunning = true;
        Program.Check(ReferenceEquals(itemSnapshot, model.CowInfo), "running state-only does not replace item: " + type.Name);
        Border[] borders = Nodes(view).OfType<Border>().Where(border => BorderStoryboards(border).Any()).ToArray();
        if (borders.Length > 0)
        {
            CheckBorderState(view, model, borders, true, type.Name);
            model.IsRunning = false;
            CheckBorderState(view, model, borders, false, type.Name);
        }
        host.RootVisual = null; Pump();
        Program.Check(!view.IsLoaded, "running page unloads: " + type.Name);
        host.RootVisual = view; Pump();
        AssertValues(view, model.CowInfo, type.Name + " reload");
        SexDisplayChecks.CheckView(view, source, model.UpdateCowInfo);
        if (type.Name.StartsWith("Sokcho", StringComparison.Ordinal))
            CheckSokchoNumber(view, source, model.UpdateCowInfo);
        host.RootVisual = null; Pump();
        AssertNoErrors(errors, type.Name + " update/reload");
    }

    private static void CheckResult(Type type, BindingErrors errors)
    {
        errors.Clear();
        var view = (UserControl)Activator.CreateInstance(type)!;
        if (type.Name.StartsWith("Sokcho", StringComparison.Ordinal)) { view.Width = 128; view.Height = 64; }
        var item = Sample(); item.AuctionResultStatus = type.Name.Contains("UnSold", StringComparison.OrdinalIgnoreCase) ? "23" : "22";
        view.DataContext = new AuctionItemDisplayViewModel(item);
        using var host = Host(view);
        bool placeholder = type.Name is "AuctionSold_320_64" or "AuctionUnSold_320_64";
        if (!placeholder) AssertValues(view, item, type.Name);
        var updated = item.Clone(); updated.EntityNumber = "4567 8901 2"; updated.BidPrice = "1888"; updated.LowestPrice = "1712"; updated.Weight = "1444";
        updated.Note = "RESULT_NOTE"; updated.NoteChangeOwner = "RESULT_HISTORY";
        view.DataContext = new AuctionItemDisplayViewModel(updated); Pump();
        if (!placeholder) AssertValues(view, updated, type.Name + " replaced");
        SexDisplayChecks.CheckView(view, item, value => view.DataContext = new AuctionItemDisplayViewModel(value));
        if (type.Name.StartsWith("Sokcho", StringComparison.Ordinal))
            CheckSokchoNumber(view, item, value => view.DataContext = new AuctionItemDisplayViewModel(value));
        host.RootVisual = null; Pump();
        Program.Check(!view.IsLoaded, "result page unloads: " + type.Name);
        AssertNoErrors(errors, type.Name);
    }

    private static void CheckSokchoNumber(UserControl view, gValues source, Action<gValues> update)
    {
        var number = Nodes(view).OfType<EntityNumberText>().Single();
        var item = source.Clone();
        item.EntityNumber = "1234 5678 9";
        update(item); Pump();
        var runs = number.Inlines.OfType<Run>().ToArray();
        Program.Check(string.Concat(runs.Select(run => run.Text)) == "1234 5678 9" && runs[1].Text == "5678"
            && runs[1].Foreground.ToString() == number.HighlightForeground?.ToString(), "Sokcho number and highlight: " + view.GetType().Name);
        var bounds = number.TransformToAncestor(view).TransformBounds(new Rect(number.RenderSize));
        Program.Check(bounds.Left >= 0 && bounds.Right <= 128, "Sokcho number fits 128 width: " + view.GetType().Name);
        item = item.Clone(); item.EntityNumber = ""; update(item); Pump();
        Program.Check(string.Concat(runs.Select(run => run.Text)) == "", "Sokcho empty number: " + view.GetType().Name);
        item = item.Clone(); item.EntityNumber = "AB-12"; update(item); Pump();
        Program.Check(string.Concat(runs.Select(run => run.Text)) == "AB-12" && runs[1].Text == "", "Sokcho invalid number: " + view.GetType().Name);
        item = item.Clone(); item.EntityNumber = "2345 6789 0"; update(item); Pump();
        Program.Check(string.Concat(runs.Select(run => run.Text)) == "2345 6789 0" && runs[1].Text == "6789", "Sokcho updated number: " + view.GetType().Name);
    }

    private static void CheckResultBoundary(BindingErrors errors)
    {
        errors.Clear();
        Type storeType = typeof(DisplaySelect).Assembly.GetType("CowAuctionSmall.Presentation.Display.DisplayPanelStore", true)!;
        var running = new ObservableCollection<AuctionContPanelViewModel>();
        using var store = (IDisposable)Activator.CreateInstance(storeType, BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { running }, null)!;
        // Bypass only the constructor that starts UDP/page timers; exercise the real result entry points.
        var display = (DisplaySelect)RuntimeHelpers.GetUninitializedObject(typeof(DisplaySelect));
        void Set(string field, object value) => typeof(DisplaySelect).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(display, value);
        Set("_panelStates", store);
        Set("_displaySize", DisplaySizeParser.DisplaySize.Size128x128);
        Set("_userInfo", new UserInfo { Auction = new Auction { AuctionHouseCode = "SYNTHETIC", BidderName = "Y" } });
        Set("_nhCode", "SYNTHETIC");
        Set("_setCustomDisplay", new SetCustomDisplay());
        var panel = new VirtualizingStackPanel { Width = 128, Height = 128 };
        using var host = Host(panel);
        object state = storeType.GetMethod("Get", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(store, new object[] { panel })!;
        void SetState(string field, object value) => state.GetType().GetField(field)!.SetValue(state, value);
        void SeedRunning(AuctionContPanelViewModel model)
        {
            storeType.GetMethod("ClearRunning", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(store, new[] { state, panel });
            panel.Children.Clear();
            panel.DataContext = model;
            running.Add(model);
            SetState("RunningViewModel", model);
            SetState("UseRunningNoteHost", true);
            var mode = state.GetType().GetField("Mode")!;
            mode.SetValue(state, Enum.Parse(mode.FieldType, "Running"));
            SetState("RunningPages", new List<UserControl>
            {
                new Standard_non_QQuri_Run1 { DataContext = model },
                new AuctionRunning4 { DataContext = model }
            });
        }
        var initial = Sample();
        using var runningModel = Model(initial);
        SeedRunning(runningModel);
        display.DisplayRunningPageNum(panel, initial, 1); Pump();
        var runningHost = (RunningNoteHost128)panel.Children[0];
        Program.Check(ReferenceEquals(runningHost.ScrollOwner, runningModel) && ReferenceEquals(runningHost.ItemDisplay, runningModel.ItemDisplay),
            "real running page selection connects shared display and scroll owner");
        var cachedPage = runningHost.PageContent as Standard_non_QQuri_Run1 ?? throw new InvalidOperationException("Expected cached running page");
        var cachedBorders = Nodes(cachedPage).OfType<Border>().Where(border => BorderStoryboards(border).Any()).ToArray();
        runningModel.IsRunning = true;
        CheckBorderState(cachedPage, runningModel, cachedBorders, true, "real cached page first");
        display.DisplayRunningPageNum(panel, initial, 2); Pump();
        CheckBorderState(cachedPage, runningModel, cachedBorders, false, "real cached page detached");
        Program.Check(runningModel.IsRunning && !cachedPage.IsLoaded, "real cache retains running state while page is detached");
        display.DisplayRunningPageNum(panel, initial, 1); Pump();
        Program.Check(ReferenceEquals(runningHost.PageContent, cachedPage), "real cache reuses same running page");
        CheckBorderState(cachedPage, runningModel, cachedBorders, true, "real cached page restored");
        runningModel.IsRunning = false;
        CheckBorderState(cachedPage, runningModel, cachedBorders, false, "real cached page stopped");
        runningModel.Note = "RUNNING_NOTE_CHANGED"; Pump();
        Program.Check(((TextBlock)runningHost.FindName("note")).Text.Contains("RUNNING_NOTE_CHANGED"), "running publication updates host via ItemDisplay binding");
        display.DisplayRunningPageNum(panel, initial, 2); Pump();
        Program.Check(panel.Children[0] is AuctionRunning4 && !runningHost.IsLoaded, "excluded running page bypasses note host");
        display.DisplayRunningPageNum(panel, initial, 1); Pump();
        Program.Check(ReferenceEquals(runningHost, panel.Children[0]), "return from excluded page reuses shared host");
        foreach (bool sold in new[] { true, false, true })
        {
            var source = Sample(); source.AuctionResultStatus = sold ? "22" : "23";
            void Publish() { if (sold) display.DisplaySold(panel, source); else display.DisplayUnSold(panel, source); Pump(); }
            Publish();
            Program.Check(runningHost.ItemDisplay == null && runningHost.ScrollOwner == null && runningHost.PageContent == null
                && !BindingOperations.IsDataBound(runningHost, RunningNoteHost128.ItemDisplayProperty),
                "result transition releases the previous running host and binding source");
            var noteHost = (RunningNoteHost128)panel.Children[0];
            var page = noteHost.PageContent as FrameworkElement ?? throw new InvalidOperationException("Result page is missing.");
            var itemDisplay = (AuctionItemDisplayViewModel)page.DataContext;
            Program.Check(ReferenceEquals(noteHost.ItemDisplay, itemDisplay) && noteHost.ScrollOwner == null,
                "result page and note host share the display snapshot without a running owner");
            var snapshot = itemDisplay.Snapshot;
            Program.Check(!ReferenceEquals(source, snapshot), "result boundary owns snapshot: " + sold);
            source.EntityNumber = "3456 7890 1"; source.BidPrice = "1987"; source.LowestPrice = "1813"; source.Weight = "1555";
            source.Note = "BOUNDARY_NOTE"; source.NoteChangeOwner = "BOUNDARY_HISTORY";
            Program.Check(snapshot.EntityNumber == "1234 5678 9", "result source mutation leaves previous display snapshot intact: " + sold);
            Publish();
            Program.Check(ReferenceEquals(noteHost, panel.Children[0]) && ReferenceEquals(page, noteHost.PageContent)
                && !ReferenceEquals(itemDisplay, page.DataContext), "result refresh reuses controls but replaces changed display model: " + sold);
            AssertValues(page, source, "result source boundary " + sold);
            Program.Check(((TextBlock)noteHost.FindName("note")).Text.Contains("BOUNDARY_HISTORY"), "result source history mutation reaches host: " + sold);
            itemDisplay = (AuctionItemDisplayViewModel)page.DataContext;
            Program.Check(ReferenceEquals(noteHost.ItemDisplay, itemDisplay), "result refresh shares the new display snapshot");
            Publish();
            Program.Check(ReferenceEquals(itemDisplay, page.DataContext), "equal result source preserves binding context: " + sold);
        }
        using var nextModel = Model(Sample());
        SeedRunning(nextModel);
        display.DisplayRunningPageNum(panel, nextModel.CowInfo, 1); Pump();
        Program.Check(panel.Children[0] is RunningNoteHost128 nextHost && ReferenceEquals(nextHost.ScrollOwner, nextModel)
            && nextHost.PageContent is FrameworkElement nextPage && ReferenceEquals(nextPage.DataContext, nextModel), "result-to-running transition restores the concrete page view model");
        host.RootVisual = null; Pump();
        AssertNoErrors(errors, "result publication boundary/state switches");
    }

    private static void CheckHaenamPaternity(BindingErrors errors)
    {
        errors.Clear();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Type storeType = typeof(DisplaySelect).Assembly.GetType("CowAuctionSmall.Presentation.Display.DisplayPanelStore", true)!;
        var running = new ObservableCollection<AuctionContPanelViewModel>();
        using var store = (IDisposable)Activator.CreateInstance(storeType, flags, null, new object[] { running }, null)!;
        var display = (DisplaySelect)RuntimeHelpers.GetUninitializedObject(typeof(DisplaySelect));
        void Set(string field, object value) => typeof(DisplaySelect).GetField(field, flags)!.SetValue(display, value);
        Set("_panelStates", store);
        Set("_displaySize", DisplaySizeParser.DisplaySize.Size128x128);
        Set("_userInfo", new UserInfo { Auction = new Auction { AuctionHouseCode = "8808990656106", BidderName = "Y" } });
        Set("_nhCode", "8808990656106");
        Set("_setCustomDisplay", new SetCustomDisplay());
        var panel = new VirtualizingStackPanel { Width = 128, Height = 128 };
        using var window = Host(panel);
        object state = storeType.GetMethod("Get", flags)!.Invoke(store, new object[] { panel })!;
        void SetState(string field, object value) => state.GetType().GetField(field)!.SetValue(state, value);
        var item = Sample();
        item.MatherEntityNumber = "1736 8570 5";
        item.PaternityMatch = "친자일치";
        item.FrontNoteWord = "친자";
        item.FrontNoteWordBrush = "Lime";
        using var model = Model(item);
        running.Add(model);
        SetState("RunningViewModel", model);
        SetState("UseRunningNoteHost", true);
        var mode = state.GetType().GetField("Mode")!;
        mode.SetValue(state, Enum.Parse(mode.FieldType, "Running"));
        var page = new CowAuctionSmall.Views.Size128_128.Running.CustomAuctionRunning1.HaenamJindo { DataContext = model };
        SetState("RunningPages", new List<UserControl> { page, new StandardQQuri_Run1 { DataContext = model } });
        display.DisplayRunningPageNum(panel, item, 1); Pump();
        var host = (RunningNoteHost128)panel.Children[0];
        var badge = (Border)host.FindName("borderPaternityMatch");
        var badgeText = (TextBlock)badge.Child;
        var noteCanvas = (Canvas)host.FindName("canvasNote");
        Canvas Row(string label) => (Canvas)Nodes(page).OfType<TextBlock>().Single(text => text.Text == label).Parent;
        Canvas mother = Row("어미 : ");
        Canvas brucellosis = Row("브루셀라:");
        Canvas vaccine = Row("구 제 역");
        Canvas lowest = Row("최저");
        Canvas calving = Row("산차");
        foreach (string kind in new[] { "1", "2", "3", "1", "3", "2", "1" })
        {
            item.CowDistinction = kind;
            model.UpdateCowInfo(item);
            display.DisplayRunningPageNum(panel, item, 1); Pump();
            bool calf = kind == "1";
            Program.Check(host.UseCalfPaternityBadge && ReferenceEquals(host, panel.Children[0]), "Haenam running selection reuses calf-policy host: " + kind);
            Program.Check(mother.Visibility == (calf ? Visibility.Collapsed : Visibility.Visible), "Haenam mother row is removed only for calves: " + kind);
            Program.Check(brucellosis.Margin.Top == (calf ? 59 : 71) && vaccine.Margin.Top == (calf ? 74 : 83)
                && lowest.Margin.Top == (calf ? 89 : 95) && calving.Margin.Top == (calf ? 91 : 97), "Haenam calf rows shift and adult layout remains unchanged: " + kind);
            Program.Check(badge.Visibility == (calf ? Visibility.Visible : Visibility.Collapsed)
                && Canvas.GetLeft(noteCanvas) == (calf ? 32 : 2) && noteCanvas.Width == (calf ? 94 : 124), "Haenam badge and note offset follow animal type: " + kind);
            Program.Check(badge.BorderBrush.ToString() == "#FFFFFF00" && badgeText.Foreground.ToString() == "#FFFFFF00"
                && badgeText.Text == "친자", "Haenam uses yellow badge text and frame even with a cached lime brush");
            Program.Check(model.CowInfo.PaternityMatch == "친자일치" && model.CowInfo.FrontNoteWordBrush == "Lime"
                && model.CowInfo.MatherEntityNumber == "1736 8570 5", "Haenam presentation never modifies source paternity/mother data");
            if (calf)
            {
                var badgeBounds = badge.TransformToAncestor(host).TransformBounds(new Rect(badge.RenderSize));
                foreach (var row in new[] { lowest, calving })
                foreach (var text in row.Children.OfType<TextBlock>())
                {
                    var bounds = text.TransformToAncestor(host).TransformBounds(new Rect(text.RenderSize));
                    Program.Check(bounds.Bottom <= badgeBounds.Top, "Haenam actual price/calving glyph box does not overlap badge");
                }
            }
        }
        foreach (string match in new[] { "", "-", "친자일치" })
        {
            item.PaternityMatch = match;
            model.UpdateCowInfo(item); Pump();
            bool visible = match == "친자일치";
            Program.Check(badge.Visibility == (visible ? Visibility.Visible : Visibility.Collapsed)
                && Canvas.GetLeft(noteCanvas) == (visible ? 32 : 2) && noteCanvas.Width == (visible ? 94 : 124), "Haenam absent/disabled/restored paternity resets note position");
        }
        item.CowDistinction = "2"; model.UpdateCowInfo(item); Pump();
        display.DisplayRunningPageNum(panel, item, 2); Pump();
        Program.Check(badge.Visibility == Visibility.Collapsed, "Haenam adult badge also remains hidden on another hosted running page");
        Set("_nhCode", "SYNTHETIC");
        display.DisplayRunningPageNum(panel, item, 2); Pump();
        Program.Check(!host.UseCalfPaternityBadge && badge.Visibility == Visibility.Visible && Canvas.GetLeft(noteCanvas) == 32
            && badge.BorderBrush.ToString() == "#FF00FF00" && badgeText.Foreground.ToString() == "#FF00FF00", "other house restores original adult badge and brush");
        Set("_nhCode", "8808990656106");
        display.DisplayRunningPageNum(panel, item, 1); Pump();
        Program.Check(host.UseCalfPaternityBadge && badge.Visibility == Visibility.Collapsed, "returning to Haenam clears an adult badge");
        foreach (bool sold in new[] { true, false })
        {
            item.AuctionResultStatus = sold ? "22" : "23";
            if (sold) display.DisplaySold(panel, item); else display.DisplayUnSold(panel, item);
            Pump();
            var resultHost = (RunningNoteHost128)panel.Children[0];
            var resultBadge = (Border)resultHost.FindName("borderPaternityMatch");
            Program.Check(!resultHost.UseCalfPaternityBadge && resultBadge.Visibility == Visibility.Visible
                && resultBadge.BorderBrush.ToString() == "#FF00FF00", "Haenam result retains the original adult badge policy: " + sold);
        }
        window.RootVisual = null; Pump();
        Program.Check(host.GetType().GetField("_flowTextAnimation", flags)!.GetValue(host) == null, "Haenam running host releases note animation on state change");
        AssertNoErrors(errors, "Haenam calf-only paternity layout and shared host regression");
    }
    private static void CheckHaenamNoteIntegration(BindingErrors errors)
    {
        const string house = "8808990656106";
        const string shortOverflow = "가나다라마바사아";
        const string longNote = "긴 비고 내용이 화면을 넘어가는 통합 확인입니다";
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Type storeType = typeof(DisplaySelect).Assembly.GetType("CowAuctionSmall.Presentation.Display.DisplayPanelStore", true)!;
        Type flowType = typeof(CowAuctionSmall.Presentation.Animation.FlowTextAnimation);
        void Drain()
        {
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
            timer.Start(); Dispatcher.PushFrame(frame);
        }
        foreach (string kind in new[] { "1", "2", "3" })
        foreach (string result in new[] { "sold", "unsold-x", "unsold-q" })
        {
            errors.Clear();
            var running = new ObservableCollection<AuctionContPanelViewModel>();
            using var store = (IDisposable)Activator.CreateInstance(storeType, flags, null, new object[] { running }, null)!;
            var display = (DisplaySelect)RuntimeHelpers.GetUninitializedObject(typeof(DisplaySelect));
            void Set(string name, object value) => typeof(DisplaySelect).GetField(name, flags)!.SetValue(display, value);
            Set("_panelStates", store); Set("_displaySize", DisplaySizeParser.DisplaySize.Size128x128);
            Set("_userInfo", new UserInfo { Auction = new Auction { AuctionHouseCode = house, BidderName = "Y" } });
            Set("_nhCode", house); Set("_setCustomDisplay", new SetCustomDisplay());
            Set("pageTime", new List<int> { 5, 5 }); Set("_requestedRunningPage", 1); Set("_totalRunningPage", 1);
            typeof(DisplaySelect).GetProperty("TodayAuctionItems")!.SetValue(display, running);
            var panel = new VirtualizingStackPanel { Width = 128, Height = 128 };
            using var window = Host(panel);
            var item = Sample();
            item.CowDistinction = kind; item.TraceNumber = "000000000000001"; item.SipNumber = "1"; item.SpaceIndex = "1";
            item.Is_Nh_QQuri = result == "unsold-x" ? "X" : "Y"; item.Nh_ability_1_num = "";
            item.PaternityMatch = "친자일치"; item.FrontNoteWord = "친자"; item.Note = shortOverflow; item.NoteChangeOwner = "";
            void PublishRunning() { display.DisplayRunning(panel, item, 10); Drain(); }
            void PublishResult() { if (result == "sold") display.DisplaySold(panel, item); else display.DisplayUnSold(panel, item); Drain(); }
            RunningNoteHost128 Current() => (RunningNoteHost128)panel.Children[0];
            object? Animation(RunningNoteHost128 host) => host.GetType().GetField("_flowTextAnimation", flags)!.GetValue(host);
            AnimationClock? Clock(RunningNoteHost128 host) => Animation(host) is object animation ? (AnimationClock?)flowType.GetField("_animationClock", flags)!.GetValue(animation) : null;
            void AssertNote(RunningNoteHost128 host, bool badgeVisible, bool scroll, string label)
            {
                var badge = (Border)host.FindName("borderPaternityMatch");
                var canvas = (Canvas)host.FindName("canvasNote");
                var text = (TextBlock)host.FindName("note");
                Program.Check(badge.Visibility == (badgeVisible ? Visibility.Visible : Visibility.Collapsed)
                    && Canvas.GetLeft(canvas) == (badgeVisible ? 32 : 2) && canvas.ActualWidth == (badgeVisible ? 94 : 124), label + " badge and actual viewport");
                Program.Check(text.Text == item.Note && ReferenceEquals(host.ItemDisplay!.Snapshot, canvas.DataContext), label + " note snapshot binding");
                text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Program.Check((text.DesiredSize.Width > canvas.ActualWidth) == scroll && (Clock(host) != null) == scroll, label + $" measured width/clock: text={text.DesiredSize.Width}, viewport={canvas.ActualWidth}");
            }
            PublishRunning();
            var first = Current();
            Program.Check(first.PageContent is CowAuctionSmall.Views.Size128_128.Running.CustomAuctionRunning1.HaenamJindo, "actual Haenam running selection " + kind);
            bool runningBadge = kind == "1";
            AssertNote(first, runningBadge, runningBadge, "running short " + kind);
            item = item.Clone(); item.PaternityMatch = ""; item.FrontNoteWord = ""; PublishRunning();
            AssertNote(first, false, false, "running badge removed " + kind);
            item = item.Clone(); item.Note = longNote; PublishRunning();
            AssertNote(first, false, true, "running long without badge " + kind);
            var runningClock = Clock(first);
            item = item.Clone(); item.LowestPrice = "998"; PublishRunning();
            Program.Check(ReferenceEquals(runningClock, Clock(first)), "same running text and width preserve clock " + kind);
            item = item.Clone(); item.PaternityMatch = "친자일치"; item.FrontNoteWord = "친자"; PublishRunning();
            AssertNote(first, runningBadge, true, "running long with paternity " + kind);
            item = item.Clone(); item.Note = "짧음"; PublishRunning();
            AssertNote(first, runningBadge, false, "running fitting short " + kind);
            item = item.Clone(); item.Note = ""; PublishRunning();
            AssertNote(first, runningBadge, false, "running empty " + kind);
            item = item.Clone(); item.Note = shortOverflow; PublishRunning();
            AssertNote(first, runningBadge, runningBadge, "running restored short " + kind);
            var oldRunningClock = Clock(first);
            item.AuctionResultStatus = result == "sold" ? "22" : "23";
            PublishResult();
            var resultHost = Current();
            string expected = result == "sold" ? "HaenamJindoSold" : result == "unsold-x" ? "Standard_non_X_UnSold" : "QQuriUnSold";
            Program.Check(resultHost.PageContent?.GetType().Name == expected && !ReferenceEquals(first, resultHost) && Animation(first) == null,
                "actual result selection and old host cleanup " + kind + "/" + result);
            Program.Check(oldRunningClock == null || oldRunningClock.CurrentState == ClockState.Stopped, "old running clock stopped " + kind + "/" + result);
            AssertNote(resultHost, true, true, "result short " + kind + "/" + result);
            var clock = Clock(resultHost);
            item = item.Clone(); item.LowestPrice = "999"; PublishResult();
            Program.Check(ReferenceEquals(clock, Clock(resultHost)), "same result text and width preserve clock " + kind + "/" + result);
            item = item.Clone(); item.PaternityMatch = ""; item.FrontNoteWord = ""; PublishResult();
            AssertNote(resultHost, false, false, "result badge removed " + kind + "/" + result);
            item = item.Clone(); item.Note = longNote; PublishResult();
            AssertNote(resultHost, false, true, "result long without badge " + kind + "/" + result);
            item = item.Clone(); item.PaternityMatch = "친자일치"; item.FrontNoteWord = "친자"; PublishResult();
            AssertNote(resultHost, true, true, "result long with badge " + kind + "/" + result);
            item = item.Clone(); item.Note = "짧음"; PublishResult();
            AssertNote(resultHost, true, false, "result fitting short " + kind + "/" + result);
            item = item.Clone(); item.Note = ""; PublishResult();
            AssertNote(resultHost, true, false, "result empty " + kind + "/" + result);
            item = item.Clone(); item.Note = longNote; PublishResult();
            AssertNote(resultHost, true, true, "result resumes before next animal " + kind + "/" + result);
            var oldResultClock = Clock(resultHost)!;
            item = item.Clone(); item.TraceNumber = "000000000000002"; item.SipNumber = "2"; item.AuctionResultStatus = "11"; item.Note = "다음 개체의 새로운 긴 비고를 표시합니다";
            PublishRunning();
            var next = Current();
            Program.Check(!ReferenceEquals(resultHost, next) && Animation(resultHost) == null && next.ItemDisplay!.Snapshot.TraceNumber == item.TraceNumber,
                "next animal releases result host " + kind + "/" + result);
            Program.Check(oldResultClock.CurrentState == ClockState.Stopped, "next animal stops result clock " + kind + "/" + result);
            AssertNote(next, runningBadge, true, "next running long " + kind + "/" + result);
            window.RootVisual = null; Drain();
            AssertNoErrors(errors, "Haenam note integration " + kind + "/" + result);
        }
        Console.WriteLine("Haenam note integration: 3 animal types x sold and both unsold selection paths");
    }

    private static void CheckSharedControls(BindingErrors errors)
    {
        errors.Clear();
        var badge = new NhQquriBadge { DataContext = Sample() };
        using (var host = Host(badge))
        {
            var canvas = (Canvas)badge.Content;
            Program.Check(canvas.Visibility == Visibility.Visible, "common badge receives direct gValues Y");
            var hidden = Sample(); hidden.Is_Nh_QQuri = "N"; badge.DataContext = hidden; Pump();
            Program.Check(canvas.Visibility == Visibility.Collapsed, "common badge refresh hides N");
            host.RootVisual = null; Pump();
        }
        foreach (bool running in new[] { false, true })
        {
            var source = Sample();
            using var model = Model(source);
            var view = new RunningNoteHost128 { DataContext = new object() };
            if (running)
            {
                view.ScrollOwner = model;
                view.SetBinding(RunningNoteHost128.ItemDisplayProperty, new Binding(nameof(model.ItemDisplay)) { Source = model, Mode = BindingMode.OneWay });
                view.PageContent = new Standard_non_QQuri_Run1 { DataContext = model };
            }
            else view.ItemDisplay = new AuctionItemDisplayViewModel(source);
            using var host = Host(view);
            TextBlock note = (TextBlock)view.FindName("note");
            Program.Check(note.Text.Contains("SYNTHETIC_NOTE") && note.Text.Contains("SYNTHETIC_HISTORY"), "note host binds normal/history: " + running);
            Border badgeBorder = (Border)view.FindName("borderPaternityMatch");
            Program.Check(badgeBorder.Visibility == Visibility.Visible, "note host shows paternity: " + running);
            if (running)
            {
                model.IsRunning = true;
                var page = view.PageContent as FrameworkElement ?? throw new InvalidOperationException("Running page is missing.");
                Border[] borders = Nodes(page).OfType<Border>().ToArray();
                CheckBorderState(page, model, borders, true, "nested " + page.GetType().Name);
                AssertValues(page, model.CowInfo, "nested running page");
                model.Note = "SETTER_NOTE"; Pump();
                Program.Check(note.Text.Contains("SETTER_NOTE"), "root note setter refreshes actual note host binding");
            }
            source.Note = ""; source.NoteChangeOwner = "HISTORY_ONLY"; source.PaternityMatch = ""; source.FrontNoteWord = "";
            if (running) model.UpdateCowInfo(source); else view.ItemDisplay = new AuctionItemDisplayViewModel(source);
            Pump();
            Program.Check(note.Text == "HISTORY_ONLY" && badgeBorder.Visibility == Visibility.Collapsed, "history-only and paternity removal update: " + running);
            source.NoteChangeOwner = "";
            if (running) model.UpdateCowInfo(source); else view.ItemDisplay = new AuctionItemDisplayViewModel(source);
            Pump();
            Program.Check(note.Text == "", "note/history deletion clears host: " + running);
            host.RootVisual = null; Pump();
            Program.Check(view.GetType().GetField("_flowTextAnimation", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(view) == null,
                "note host unload releases animation: " + running);
        }
        AssertNoErrors(errors, "common controls");
    }

    private static void AssertValues(DependencyObject view, gValues expected, string label)
    {
        int checkedFields = 0;
        var display = new AuctionItemDisplayViewModel(expected);
        foreach (DependencyObject node in Nodes(view))
        {
            if (node is Canvas canvas && canvas.Style?.Triggers.OfType<DataTrigger>().Any(trigger =>
                trigger.Binding is Binding triggerBinding && triggerBinding.Path.Path.EndsWith(".ShowPregnancy", StringComparison.Ordinal)) == true)
                Program.Check(canvas.Visibility == (display.ShowPregnancy ? Visibility.Visible : Visibility.Collapsed), "pregnancy canvas updates: " + label);
            DependencyProperty? property = node switch { EntityNumberText => EntityNumberText.NumberProperty, TextBlock => TextBlock.TextProperty, OutlinedTextBlock => OutlinedTextBlock.TextProperty, System.Windows.Documents.Run => System.Windows.Documents.Run.TextProperty, _ => null };
            if (property == null) continue;
            Binding? binding = BindingOperations.GetBinding(node, property);
            string? path = binding?.Path?.Path;
            if (path?.StartsWith("DataContext.", StringComparison.Ordinal) == true)
            {
                var displayProperty = typeof(AuctionItemDisplayViewModel).GetProperty(path.Split('.').Last());
                if (displayProperty?.PropertyType == typeof(string))
                {
                    string expectedDisplay = (string)displayProperty.GetValue(display)!;
                    if (binding!.StringFormat != null)
                        expectedDisplay = string.Format(System.Globalization.CultureInfo.CurrentCulture, binding.StringFormat, expectedDisplay);
                    Program.Check((string)node.GetValue(property) == expectedDisplay, "actual formatted binding " + label + "." + path);
                    Program.Check(binding!.Converter == null, "migrated display binding no longer invokes a converter: " + label);
                    checkedFields++;
                    continue;
                }
            }
            if (path?.StartsWith("Snapshot.", StringComparison.Ordinal) == true) path = path.Substring("Snapshot.".Length);
            if (path?.StartsWith("CowInfo.", StringComparison.Ordinal) == true) path = path.Substring("CowInfo.".Length);
            if (path == null || !DisplayFields.Contains(path)) continue;
            string expectedText = (string)typeof(gValues).GetProperty(path)!.GetValue(expected)!;
            string actual = (node is EntityNumberText number ? new TextRange(number.ContentStart, number.ContentEnd).Text : node.GetValue(property)?.ToString() ?? "").Replace(",", "");
            if (node is EntityNumberText { DisplayMode: EntityNumberDisplayMode.Short } shortNumber)
                expectedText = shortNumber.CowDistinction == "5" ? expectedText.Replace(" ", "")[^3..] : expectedText.Split(' ')[1];
            Program.Check(actual.Contains(expectedText, StringComparison.Ordinal), $"actual bound value {label}.{path}: expected {expectedText}, got {actual}; inlines={string.Join("/", (node as TextBlock)?.Inlines.OfType<System.Windows.Documents.Run>().Select(run => run.Text) ?? Array.Empty<string>())}");
            checkedFields++;
        }
        Program.Check(checkedFields > 0, "at least one actual item field checked: " + label);
    }

    private static IEnumerable<DependencyObject> Nodes(DependencyObject node)
    {
        yield return node;
        if (node is TextBlock text)
            foreach (Inline inline in text.Inlines) yield return inline;
        if (node is not Visual && node is not System.Windows.Media.Media3D.Visual3D) yield break;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
            foreach (var child in Nodes(VisualTreeHelper.GetChild(node, index))) yield return child;
    }

    private static HwndSource Host(FrameworkElement view)
    {
        var source = new HwndSource(new HwndSourceParameters("Offline binding checks")
        { Width = 320, Height = 128, WindowStyle = unchecked((int)0x80000000) });
        source.RootVisual = view;
        view.Measure(new Size(320, 128)); view.Arrange(new Rect(0, 0, 320, 128)); view.UpdateLayout(); Pump();
        return source;
    }

    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static void CheckBorderState(FrameworkElement view, AuctionContPanelViewModel model, Border[] borders, bool running, string label)
    {
        const int timeoutMs = 2500;
        var targets = borders.SelectMany(border => BorderStoryboards(border).Select(item => (Border: border, item.Storyboard, item.Name, item.Removes))).ToArray();
        Program.Check(targets.Length > 0, "named border storyboard lookup failed: " + label);
        var stopwatch = Stopwatch.StartNew();
        string? lookupError = null;
        var initialColors = targets.Select(target => (target.Border.BorderBrush as SolidColorBrush)?.Color).ToArray();
        var sawChange = new bool[targets.Length];
        bool Ready()
        {
            try
            {
                bool[] states = targets.Select((target, index) =>
                {
                    ClockState state;
                    try { state = target.Storyboard.GetCurrentState(target.Border); }
                    catch (InvalidOperationException) when (!running && target.Removes && !DependencyPropertyHelper.GetValueSource(target.Border, Border.BorderBrushProperty).IsAnimated)
                    { return target.Border.BorderBrush is SolidColorBrush black && black.Color == Colors.Black; }
                    var color = (target.Border.BorderBrush as SolidColorBrush)?.Color;
                    if (color.HasValue && initialColors[index].HasValue && color != initialColors[index]) sawChange[index] = true;
                    return running ? state == ClockState.Active && sawChange[index] && DependencyPropertyHelper.GetValueSource(target.Border, Border.BorderBrushProperty).IsAnimated
                        : (target.Removes ? !DependencyPropertyHelper.GetValueSource(target.Border, Border.BorderBrushProperty).IsAnimated : state == ClockState.Stopped) && color == Colors.Black;
                }).ToArray();
                lookupError = null;
                return running ? states.Any(state => state) : states.All(state => state);
            }
            catch (InvalidOperationException ex) { lookupError = ex.Message; return false; }
        }
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) => { if (Ready() || stopwatch.ElapsedMilliseconds >= timeoutMs) { timer.Stop(); frame.Continue = false; } };
        if (!Ready()) { timer.Start(); Dispatcher.PushFrame(frame); }
        stopwatch.Stop();
        string borderStates = string.Join("; ", borders.Select((border, index) =>
        {
            var state = DependencyPropertyHelper.GetValueSource(border, Border.BorderBrushProperty);
            string paths = string.Join(",", border.Style?.Triggers.OfType<DataTrigger>().Select(trigger => (trigger.Binding as Binding)?.Path?.Path) ?? Array.Empty<string?>());
            string clocks = string.Join(",", targets.Where(target => ReferenceEquals(target.Border, border)).Select(target =>
            {
                try { return $"{target.Name}:{target.Storyboard.GetCurrentState(border)}"; }
                catch (InvalidOperationException ex) { return $"{target.Name}:조회실패({ex.Message})"; }
            }));
            return $"{index}:animated={state.IsAnimated},source={state.BaseValueSource},brush={border.BorderBrush},loaded={border.IsLoaded},visible={border.IsVisible},context={border.DataContext?.GetType().Name},trigger=[{paths}],clocks=[{clocks}]";
        }));
        string diagnostic = $"view={label}, target={running}, IsRunning={model.IsRunning}, dataContextMatches={ReferenceEquals(view.DataContext, model)}, context={view.DataContext?.GetType().Name}, loaded={view.IsLoaded}, visible={view.IsVisible}, borders={borders.Length}, elapsedMs={stopwatch.ElapsedMilliseconds}, timeoutMs={timeoutMs}, lookupError={lookupError}, {borderStates}";
        Console.WriteLine("Border animation: " + diagnostic);
        Program.Check(borders.Length > 0 && Ready(), "border animation state: " + diagnostic);
    }

    private static IEnumerable<(Storyboard Storyboard, string Name, bool Removes)> BorderStoryboards(Border border)
    {
        if (border.Style == null) yield break;
        foreach (TriggerBase trigger in border.Style.Triggers)
        {
            bool matches = trigger switch
            {
                DataTrigger single => (single.Binding as Binding)?.Path?.Path?.EndsWith("IsRunning", StringComparison.Ordinal) == true,
                MultiDataTrigger multi => multi.Conditions.Any(condition => (condition.Binding as Binding)?.Path?.Path?.EndsWith("IsRunning", StringComparison.Ordinal) == true),
                _ => false
            };
            if (!matches) continue;
            TriggerActionCollection enter = trigger is DataTrigger data ? data.EnterActions : ((MultiDataTrigger)trigger).EnterActions;
            TriggerActionCollection exit = trigger is DataTrigger dataExit ? dataExit.ExitActions : ((MultiDataTrigger)trigger).ExitActions;
            foreach (var begin in enter.OfType<BeginStoryboard>())
            {
                bool removes = exit.OfType<RemoveStoryboard>().Any(action => action.BeginStoryboardName == begin.Name);
                if (removes || exit.OfType<StopStoryboard>().Any(action => action.BeginStoryboardName == begin.Name))
                    yield return (begin.Storyboard, begin.Name, removes);
            }
        }
    }

    private static void CheckBorderRepeat(BindingErrors errors)
    {
        for (int i = 0; i < 5; i++)
        {
            errors.Clear();
            using var model = Model(Sample());
            var view = new Standard_non_QQuri_Run1 { DataContext = model };
            using var host = Host(view);
            Border[] borders = Nodes(view).OfType<Border>().Where(border => BorderStoryboards(border).Any()).ToArray();
            model.IsRunning = true; CheckBorderState(view, model, borders, true, "standalone " + i);
            model.IsRunning = false; CheckBorderState(view, model, borders, false, "standalone stopped " + i);
            model.IsRunning = true; CheckBorderState(view, model, borders, true, "standalone restarted " + i);
            host.RootVisual = null; Pump();
            Program.Check(!view.IsLoaded, "standalone unload: " + i);
            CheckBorderState(view, model, borders, false, "standalone unloaded " + i);
            host.RootVisual = view; Pump();
            CheckBorderState(view, model, borders, true, "standalone reload " + i);
            model.IsRunning = false; CheckBorderState(view, model, borders, false, "standalone reload stopped " + i);
            host.RootVisual = null; Pump();
            AssertNoErrors(errors, "standalone border " + i);
        }
    }

    private static void CheckPilotBorders(BindingErrors errors)
    {
        var views = new UserControl[]
        {
            new Standard_non_QQuri_Run1(),
            new CowAuctionSmall.Views.Size128_64.Running.SokchoRunning1_64(),
            new CowAuctionSmall.Views.SIze_160_64.Running.AuctionRunning1_160_64(),
            new CowAuctionSmall.Views.Size_320_64.Running.AuctionRunning1_320_64(),
            new Standard_Goat_Run()
        };
        foreach (var view in views)
        {
            errors.Clear();
            using var model = Model(Sample());
            view.DataContext = model;
            using var host = Host(view);
            var borders = Nodes(view).OfType<Border>().Where(border => BorderStoryboards(border).Any()).ToArray();
            Program.Check(borders.Length == 1 && BorderStoryboards(borders[0]).Single().Removes, "pilot target and removal: " + view.GetType().Name);
            model.IsRunning = true; CheckBorderState(view, model, borders, true, "pilot " + view.GetType().Name);
            host.RootVisual = null; Pump();
            CheckBorderState(view, model, borders, false, "pilot unloaded " + view.GetType().Name);
            Program.Check(model.IsRunning && borders.All(border => !DependencyPropertyHelper.GetValueSource(border, Border.BorderBrushProperty).IsAnimated), "pilot removes clock without changing running state: " + view.GetType().Name);
            host.RootVisual = view; Pump();
            CheckBorderState(view, model, borders, true, "pilot reload " + view.GetType().Name);
            for (int i = 0; i < 3; i++) { host.RootVisual = null; Pump(); host.RootVisual = view; Pump(); }
            CheckBorderState(view, model, borders, true, "pilot rapid reload " + view.GetType().Name);
            model.IsRunning = false; CheckBorderState(view, model, borders, false, "pilot stopped " + view.GetType().Name);
            if (view is Standard_Goat_Run)
            {
                model.IsBatchGoatBorderHighlighted = true; model.IsRunning = true; Pump();
                Program.Check(borders[0].BorderBrush is SolidColorBrush brush && brush.Color == Color.FromRgb(255, 165, 0)
                    && !DependencyPropertyHelper.GetValueSource(borders[0], Border.BorderBrushProperty).IsAnimated, "goat batch orange overrides blinking");
                model.IsBatchGoatBorderHighlighted = false;
                CheckBorderState(view, model, borders, true, "goat batch cleared");
                model.IsRunning = false; CheckBorderState(view, model, borders, false, "goat stopped");
            }
            host.RootVisual = null; Pump();
            AssertNoErrors(errors, "pilot " + view.GetType().Name);
        }
        using var wrapperModel = Model(Sample());
        var page = new Standard_non_QQuri_Run1 { DataContext = wrapperModel };
        var wrapper = new RunningNoteHost128 { PageContent = page, ScrollOwner = wrapperModel, ItemDisplay = wrapperModel.ItemDisplay };
        using var wrapperHost = Host(wrapper);
        var wrapperBorders = Nodes(page).OfType<Border>().Where(border => BorderStoryboards(border).Any()).ToArray();
        wrapperModel.IsRunning = true; CheckBorderState(page, wrapperModel, wrapperBorders, true, "pilot note host");
        wrapper.PageContent = new Grid(); Pump();
        CheckBorderState(page, wrapperModel, wrapperBorders, false, "pilot note host detached page");
        wrapper.PageContent = page; Pump();
        CheckBorderState(page, wrapperModel, wrapperBorders, true, "pilot note host reattached page");
        wrapperModel.IsRunning = false; CheckBorderState(page, wrapperModel, wrapperBorders, false, "pilot note host stopped");
        wrapperHost.RootVisual = null; Pump();
    }

    private static void AssertNoErrors(BindingErrors errors, string label) => Program.Check(errors.Text.Length == 0, "WPF binding errors in " + label + ": " + errors.Text);

    private sealed class BindingErrors : TraceListener
    {
        private readonly StringBuilder _text = new();
        internal string Text => _text.ToString();
        internal void Clear() => _text.Clear();
        public override void Write(string? message) => _text.Append(message);
        public override void WriteLine(string? message) => _text.AppendLine(message);
    }
}
