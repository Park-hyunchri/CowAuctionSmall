using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views.Size128_128.Running;
using GAMSTest.Models;
using GAMSTest.Services;
using GAMSTest.Views;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.IO;
using Resolution = CowAuctionSmall.Presentation.Display.DisplaySizeParser.DisplaySize;

internal static class ControllerChecks
{
    internal static void Run(bool measure = true)
    {
        var trace = PresentationTraceSources.DataBindingSource;
        var previousLevel = trace.Switch.Level;
        using var errors = new BindingErrors();
        trace.Listeners.Add(errors);
        PresentationTraceSources.Refresh();
        trace.Switch.Level = SourceLevels.Error;
        try
        {
            CheckHaenamPaternity(errors);
            foreach (Resolution size in new[] { Resolution.Size128x64, Resolution.Size128x128, Resolution.Size160x64, Resolution.Size320x64 })
                CheckSize(size, errors);
            CheckSamples();
            CheckSettingsSnapshot();
            CheckLifetime();
            CheckNoteContinuity(errors);
            CheckControlWindow(errors);
            if (measure)
            {
                Measure(100, errors);
                Measure(200, errors);
            }
        }
        finally { trace.Listeners.Remove(errors); trace.Switch.Level = previousLevel; }
    }

    private static void CheckSize(Resolution resolution, BindingErrors errors)
    {
        var canvas = new Canvas();
        using var controller = new DisplayTestController(canvas);
        var settings = new TestSettings { Resolution = resolution, Rows = [[7, 7], [8]], HouseCode = "8808990817675", IsShowOwnerName = "NN" };
        controller.Configure(settings, SampleKind.LongNames);
        using var source = Host(canvas);
        var dimensions = TestSettings.GetDimensions(resolution);
        Program.Check(controller.SlotCount == 3 && canvas.Children.Count == 3, "duplicate board numbers retain three physical slots");
        Program.Check(canvas.Width == dimensions.Width * 2 && canvas.Height == dimensions.Height * 2, "canvas dimensions follow resolution and ragged rows");
        var slots = canvas.Children.Cast<Border>().ToArray();
        Program.Check(slots.All(slot => slot.Width == dimensions.Width && slot.Height == dimensions.Height), "each slot uses selected dimensions");
        Program.Check(Canvas.GetLeft(slots[1]) == dimensions.Width && Canvas.GetTop(slots[2]) == dimensions.Height, "physical row and column placement");
        Program.Check(slots.Select(slot => ((BoardNumberView)slot.Child).Number).SequenceEqual(new[] { "7", "7", "8" }), "duplicate labels remain visible independently");
        ExportScreenshot(canvas, resolution + "_NUMBER");
        var numbers = slots.Select(slot => slot.Child).ToArray();
        controller.SetBrightness(128);
        Program.Check(slots.Select(slot => slot.Child).SequenceEqual(numbers), "brightness does not replace the number pattern");
        foreach (Color color in new[] { Colors.Red, Colors.Lime, Colors.Blue, Colors.White, Colors.Black })
        {
            controller.Fill(color);
            Color expected = Color.FromRgb((byte)(color.R * 128 / 255), (byte)(color.G * 128 / 255), (byte)(color.B * 128 / 255));
            Program.Check(slots.All(slot => slot.Child == null && ((SolidColorBrush)slot.Background).Color == expected), "all slots share requested RGB intensity");
        }

        controller.PrepareAll();
        int cacheCount = controller.CachedViewCount;
        Program.Check(cacheCount == controller.Modes.Count(mode => mode.IsSupported) * 3, "cache contains only supported modes for every physical slot");
        foreach (var mode in controller.Modes)
        {
            if (!mode.IsSupported)
            {
                Program.Throws<ArgumentException>(() => controller.ShowMode(mode), "placeholder cannot be selected as functioning screen");
                continue;
            }
            errors.Clear();
            controller.ShowMode(mode);
            Layout(canvas);
            ExportScreenshot(canvas, resolution + "_" + mode.Key);
            Program.Check(errors.Text.Length == 0, "real screen binding paths: " + resolution + "/" + mode.Key + ": " + errors.Text);
            var current = slots.Select(slot => slot.Child).ToArray();
            var rawViews = current.Select(child => child is RunningNoteHost128 note ? (FrameworkElement)note.PageContent! : (FrameworkElement)child!).ToArray();
            if (mode.Page > 0)
                Program.Check(rawViews.All(view => !((AuctionContPanelViewModel)view.DataContext).IsRunning), "manual page inspection leaves running animation triggers inactive");
            controller.ShowNumbers();
            Program.Check(rawViews.All(view => view.DataContext == null), "inactive cached views detach animation-bound DataContext");
            controller.ShowMode(mode);
            Program.Check(slots.Select(slot => slot.Child).SequenceEqual(current), "reselecting mode reuses actual view objects");
            Program.Check(controller.CachedViewCount == cacheCount, "manual transitions do not grow cache");
            foreach (var slot in slots)
            {
                var view = slot.Child is RunningNoteHost128 host ? (FrameworkElement)host.PageContent! : (FrameworkElement)slot.Child!;
                Program.Check(mode.Page > 0 ? view.DataContext is AuctionContPanelViewModel : view.DataContext is AuctionItemDisplayViewModel, "real running/result context boundary");
                if (slot.Child is RunningNoteHost128 noteHost)
                    Program.Check(noteHost.ItemDisplay != null && noteHost.PageContent is not RunningNoteHost128, "exactly one shared note wrapper");
            }
        }
        if (resolution == Resolution.Size320x64)
            Program.Check(controller.Modes.Count(mode => mode.IsSupported) == 2 && controller.Modes.Count(mode => !mode.IsSupported) == 3, "320 WAIT/RUN1 available; three placeholders excluded from cycle");
        var firstRun = controller.Modes.First(mode => mode.IsSupported && mode.Key.StartsWith("RUN"));
        controller.ShowMode(firstRun);
        var runningViews = slots.Select(slot => slot.Child is RunningNoteHost128 note ? (FrameworkElement)note.PageContent! : (FrameworkElement)slot.Child!).ToArray();
        controller.StartCycle(TimeSpan.FromSeconds(5));
        Program.Check(runningViews.All(view => ((AuctionContPanelViewModel)view.DataContext).IsRunning), "automatic cycle activates borders even when the first page is already selected");
        controller.ShowMode(firstRun);
        Program.Check(!controller.IsCycling && runningViews.All(view => !((AuctionContPanelViewModel)view.DataContext).IsRunning), "manual selection of the same cycling page stops its blinking border");
        source.RootVisual = null;
        controller.Dispose();
        Program.Drain();
        Program.Check(controller.IsDisposed && controller.CachedViewCount == 0 && controller.SlotCount == 0 && canvas.Children.Count == 0, "dispose releases attached views and caches");
    }

    private static void CheckHaenamPaternity(BindingErrors errors)
    {
        string directory = Path.Combine(Path.GetTempPath(), "GAMSTest-Haenam-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string board = Path.Combine(directory, "Board.XML"), users = Path.Combine(directory, "users.XML");
        File.WriteAllText(board, "<Boards><Size>128,128</Size><MultiBoard><RowIdx>1,2</RowIdx></MultiBoard></Boards>");
        var canvas = new Canvas();
        using var controller = new DisplayTestController(canvas);
        static IEnumerable<DependencyObject> Nodes(DependencyObject node)
        {
            yield return node;
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
                foreach (var child in Nodes(VisualTreeHelper.GetChild(node, index))) yield return child;
        }
        foreach (string enabled in new[] { "Y", "N" })
        foreach (string animal in new[] { "1", "2", "3" })
        {
            File.WriteAllText(users, $"<Users><Auction><AuctionHouseCode>8808990656106</AuctionHouseCode><IsShowQQuri>Y</IsShowQQuri><IsPaternityMatch>{enabled}</IsPaternityMatch></Auction></Users>");
            var settings = TestSettingsLoader.Load(board, users);
            settings.CowDistinction = animal;
            Program.Check(settings.HouseCode == "8808990656106" && settings.Resolution == Resolution.Size128x128
                && settings.Rows[0].SequenceEqual(new[] { 1, 2 }), "Haenam tool reads house and physical layout from synthetic XML");
            var sample = MockDataFactory.Create(1, settings, SampleKind.Normal);
            Program.Check(sample.CowDistinction == animal && sample.FrontNoteWordBrush == "Yellow", "Haenam tool sample preserves animal type and yellow paternity brush");
            controller.Configure(settings, SampleKind.Normal);
            using var source = Host(canvas);
            int cached = 0;
            foreach (string key in new[] { "RUN1", "RUN2", "RUN1", "UNSOLD", "RUN1", "SOLD", "RUN1", "WAIT", "RUN1" })
            {
                errors.Clear();
                var mode = controller.Modes.Single(value => value.Key == key);
                controller.ShowMode(mode); Layout(canvas);
                ExportScreenshot(canvas, $"Haenam_{animal}_{enabled}_{key}");
                foreach (Border slot in canvas.Children)
                {
                    var host = slot.Child as RunningNoteHost128;
                    var page = (FrameworkElement)(host?.PageContent ?? slot.Child);
                    var item = mode.Page > 0 ? ((AuctionContPanelViewModel)page.DataContext).CowInfo
                        : ((AuctionItemDisplayViewModel)page.DataContext).Snapshot;
                    Program.Check(item.CowDistinction == animal, "GAMS real page receives the selected animal kind: " + key);
                    if (host != null)
                    {
                        bool running = mode.Page > 0;
                        bool visible = enabled == "Y" && (!running || animal == "1");
                        var badge = (Border)host.FindName("borderPaternityMatch");
                        var text = (TextBlock)badge.Child;
                        var note = (Canvas)host.FindName("canvasNote");
                        Program.Check(host.UseCalfPaternityBadge == running && badge.Visibility == (visible ? Visibility.Visible : Visibility.Collapsed),
                            "GAMS Haenam running/result badge policy: " + animal + "/" + enabled + "/" + key);
                        Program.Check(Canvas.GetLeft(note) == (visible ? 32 : 2), "GAMS Haenam note left offset follows actual badge visibility");
                        if (visible)
                            Program.Check(badge.BorderBrush.ToString() == "#FFFFFF00" && text.Foreground.ToString() == "#FFFFFF00"
                                && text.Text == "친자", "GAMS Haenam real badge has yellow text and frame");
                    }
                    if (key is "RUN1" or "WAIT")
                    {
                        Program.Check(page.GetType().Name == "HaenamJindo", "GAMS selects the actual Haenam progress page");
                        var mother = Nodes(page).OfType<TextBlock>().Single(text => text.Text == "어미 : ");
                        var lowest = Nodes(page).OfType<TextBlock>().Single(text => text.Text == "최저");
                        Program.Check(((Canvas)mother.Parent).Visibility == (animal == "1" ? Visibility.Collapsed : Visibility.Visible),
                            "GAMS actual Haenam mother row is hidden only for calves");
                        Program.Check(((Canvas)lowest.Parent).Margin.Top == (animal == "1" ? 89 : 95), "GAMS uses the latest calf row spacing while preserving adult coordinates");
                        if (animal == "1" && enabled == "Y")
                        {
                            var badge = (Border)host!.FindName("borderPaternityMatch");
                            var textBounds = lowest.TransformToAncestor(host).TransformBounds(new Rect(lowest.RenderSize));
                            var badgeBounds = badge.TransformToAncestor(host).TransformBounds(new Rect(badge.RenderSize));
                            Program.Check(textBounds.Bottom <= badgeBounds.Top, "GAMS actual calf price does not overlap paternity badge");
                        }
                    }
                }
                Program.Check(errors.Text.Length == 0, "GAMS Haenam real view binding trace: " + errors.Text);
                if (key == "SOLD") cached = controller.CachedViewCount;
            }
            Program.Check(controller.CachedViewCount == cached + 2, "GAMS revisiting modes only adds the distinct WAIT view, not duplicate cached pages");
            source.RootVisual = null; Program.Drain();
        }
        foreach (string house in new[] { "SYNTHETIC", "8808990817675" })
        {
            var settings = new TestSettings { HouseCode = house, Rows = [[1]], CowDistinction = "2" };
            controller.Configure(settings, SampleKind.Normal);
            using var source = Host(canvas);
            controller.ShowMode(controller.Modes.Single(mode => mode.Key == "RUN1")); Layout(canvas);
            var host = (RunningNoteHost128)((Border)canvas.Children[0]).Child;
            var badge = (Border)host.FindName("borderPaternityMatch");
            Program.Check(!host.UseCalfPaternityBadge && badge.Visibility == Visibility.Visible, "other houses retain adult paternity badge in GAMS");
            Program.Check(badge.BorderBrush.ToString() == (house == "8808990817675" ? "#FF00FF00" : "#FFFFFF00"), "GAMS preserves Jangseong lime and ordinary yellow sample colors");
            source.RootVisual = null; Program.Drain();
        }
    }
    private static void CheckSamples()
    {
        var settings = new TestSettings { IsShowOwnerName = "NN" };
        var normal = new AuctionItemDisplayViewModel(MockDataFactory.Create(1, settings, SampleKind.Normal));
        Program.Check(normal.LowestPriceText.Contains(',') && normal.BidPriceText.Contains(','), "synthetic prices exercise real comma formatting");
        var sex = new AuctionItemDisplayViewModel(MockDataFactory.Create(1, settings, SampleKind.ThreeCharacterSex));
        Program.Check(sex.SexText == "비거세" && sex.IsThreeCharacterSex, "three-character sex fixture uses real compression flag");
        var empty = MockDataFactory.Create(1, settings, SampleKind.Empty);
        Program.Check(empty.Note.Length == 0 && empty.NoteChangeOwner.Length == 0 && empty.HistoryOwner == null && empty.EntityNumber.Length == 0, "empty fixture clears optional history/note/entity fields");
        var longNames = MockDataFactory.Create(1, settings, SampleKind.LongNames);
        Program.Check(longNames.OwnerName.Length > 3 && longNames.Bidder.Length > 3, "long unmasked names retained for clipping and scrolling inspection");
        foreach (string bidderMode in new[] { "Y", "N", "B", "X" })
        {
            settings.BidderName = bidderMode;
            var value = MockDataFactory.Create(1, settings, SampleKind.LongNames);
            Program.Check(value.BidderName == bidderMode && value.BidderNum.Length > 0, "each bidder mode preserved with a separate bidder number");
            Program.Check(bidderMode == "X" ? value.Bidder.Contains('*') : bidderMode == "N" ? value.Bidder.All(char.IsDigit) : value.Bidder.Length > 3, "bidder display mode sample matches product input contract");
        }
        settings.MaskOwner = true;
        Program.Check(MockDataFactory.Create(1, settings, SampleKind.LongNames).OwnerName is { Length: 3 } masked && masked.Contains('*'), "masked owner fixture retains three-character mask");
    }

    private static void CheckLifetime()
    {
        var canvas = new Canvas();
        using var controller = new DisplayTestController(canvas);
        controller.Configure(new TestSettings { Rows = [[1]], Resolution = Resolution.Size128x64 }, SampleKind.Normal);
        using var source = Host(canvas);
        Program.Throws<ArgumentException>(() => controller.StartCycle(TimeSpan.Zero), "zero interval rejected");
        controller.StartCycle(TimeSpan.FromSeconds(1));
        string first = controller.CurrentMode;
        Program.Check(controller.IsCycling, "cycle starts");
        PumpFor(TimeSpan.FromMilliseconds(1150));
        Program.Check(controller.IsCycling && controller.CurrentMode != first, "single dispatcher timer advances actual page");
        controller.ShowMode(controller.Modes.First(mode => mode.IsSupported));
        Program.Check(!controller.IsCycling, "manual page stops cycle");
        controller.StartCycle(TimeSpan.FromSeconds(1));
        controller.Fill(Colors.Red);
        Program.Check(!controller.IsCycling, "manual color stops cycle");
        controller.StartCycle(TimeSpan.FromSeconds(1));
        controller.ShowNumbers();
        Program.Check(!controller.IsCycling, "number mode stops cycle");
        controller.StartCycle(TimeSpan.FromSeconds(1));
        controller.Configure(new TestSettings { Resolution = Resolution.Size320x64, Rows = [[9, 9]] }, SampleKind.Empty);
        Program.Check(!controller.IsCycling && controller.CachedViewCount == 0 && controller.SlotCount == 2 && canvas.Width == 640, "reconfigure stops timer and discards previous resolution cache");
        controller.StartCycle(TimeSpan.FromSeconds(1));
        source.RootVisual = null;
        controller.Dispose();
        Program.Check(!controller.IsCycling && controller.IsDisposed, "dispose stops active timer");
        Program.Throws<ObjectDisposedException>(() => controller.ShowNumbers(), "disposed controller rejects new display work");
        controller.Dispose();
    }

    private static void CheckNoteContinuity(BindingErrors errors)
    {
        static object? Field(object target, string name) => target.GetType()
            .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(target);
        var canvas = new Canvas();
        using var controller = new DisplayTestController(canvas);
        controller.Configure(new TestSettings { Rows = [[1, 1]], PageCount = 2 }, SampleKind.LongNames);
        using var source = Host(canvas);
        errors.Clear();
        var run1 = controller.Modes.Single(mode => mode.Key == "RUN1");
        var run2 = controller.Modes.Single(mode => mode.Key == "RUN2");
        controller.ShowMode(run1);
        Layout(canvas);
        var slots = canvas.Children.Cast<Border>().ToArray();
        var hosts = slots.Select(slot => (RunningNoteHost128)slot.Child).ToArray();
        var pages = hosts.Select(host => (FrameworkElement)host.PageContent!).ToArray();
        var animations = hosts.Select(host => Field(host, "_flowTextAnimation")!).ToArray();
        var clocks = animations.Select(animation => Field(animation, "_animationClock")).ToArray();
        Program.Check(animations.All(animation => animation != null) && clocks.All(clock => clock != null), "shared note animations have active WPF clocks");
        int loaded = 0, unloaded = 0, runningChanges = 0;
        foreach (var host in hosts)
        {
            host.Loaded += (_, _) => loaded++;
            host.Unloaded += (_, _) => unloaded++;
            host.ScrollOwner!.PropertyChanged += (_, args) => { if (args.PropertyName == "IsRunning") runningChanges++; };
        }
        for (int repeat = 0; repeat < 3; repeat++)
        {
            controller.ShowMode(run1);
            Layout(canvas);
            Program.Check(hosts.Select(host => host.PageContent).SequenceEqual(pages), "same-mode click leaves page and bindings attached");
        }
        for (int repeat = 0; repeat < 3; repeat++)
        {
            controller.ShowMode(run2);
            Layout(canvas);
            Program.Check(pages.All(page => page.DataContext == null), "inactive running body releases DataContext during shared-note switch");
            var secondPages = hosts.Select(host => (FrameworkElement)host.PageContent!).ToArray();
            controller.ShowMode(run1);
            Layout(canvas);
            Program.Check(secondPages.All(page => page.DataContext == null), "returning to first page stops hidden second-page triggers");
        }
        for (int i = 0; i < hosts.Length; i++)
            Program.Check(ReferenceEquals(slots[i].Child, hosts[i]) && ReferenceEquals(Field(hosts[i], "_flowTextAnimation"), animations[i]) && ReferenceEquals(Field(animations[i], "_animationClock"), clocks[i]), "running page changes retain exact note host, animation and clock");
        Program.Check(loaded == 0 && unloaded == 0 && runningChanges == 0, "reselect and running transitions do not reload note or toggle running state");
        controller.StartCycle(TimeSpan.FromSeconds(5));
        var tick = typeof(DisplayTestController).GetMethod("OnTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        tick.Invoke(controller, [null, EventArgs.Empty]);
        Layout(canvas);
        Program.Check(controller.CurrentMode == run2.Label && hosts.Select(host => Field(host, "_flowTextAnimation")).SequenceEqual(animations), "automatic running-page transition also preserves note animation");
        Program.Check(hosts.All(host => host.ScrollOwner!.IsRunning), "automatic page transitions keep running borders active");
        controller.ShowMode(run2);
        Program.Check(!controller.IsCycling, "same-mode manual selection still stops automatic cycle");
        Program.Check(hosts.All(host => !host.ScrollOwner!.IsRunning), "manual page selection disables borders without restarting shared notes");
        var clock = (System.Windows.Media.Animation.AnimationClock)clocks[0]!;
        var timeBefore = clock.CurrentTime;
        PumpFor(TimeSpan.FromMilliseconds(100));
        Program.Check(clock.CurrentTime > timeBefore, "retained note clock keeps advancing after repeated page changes");
        controller.ShowMode(controller.Modes.Single(mode => mode.Key == "SOLD"));
        Layout(canvas);
        Program.Check(hosts.All(host => Field(host, "_flowTextAnimation") == null && host.PageContent == null), "result transition stops detached running-note animation");
        controller.ShowMode(run1);
        Layout(canvas);
        Program.Check(slots.Select(slot => slot.Child).SequenceEqual(hosts), "return from result reuses per-slot running note host");
        controller.Fill(Colors.Black);
        Layout(canvas);
        Program.Check(hosts.All(host => Field(host, "_flowTextAnimation") == null), "solid color stops running-note animations");
        controller.ShowMode(run1);
        Layout(canvas);
        controller.Configure(new TestSettings { Rows = [[9]] }, SampleKind.Empty);
        Layout(canvas);
        Program.Check(hosts.All(host => host.ItemDisplay == null && host.ScrollOwner == null && host.PageContent == null && Field(host, "_flowTextAnimation") == null), "board replacement releases shared note references and animation");
        controller.Configure(new TestSettings { Rows = [[1]], CowDistinction = "3" }, SampleKind.LongNames);
        run1 = controller.Modes.Single(mode => mode.Key == "RUN1");
        run2 = controller.Modes.Single(mode => mode.Key == "RUN2");
        controller.ShowMode(run1);
        Layout(canvas);
        var breedingSlot = (Border)canvas.Children[0];
        var breedingHost = (RunningNoteHost128)breedingSlot.Child;
        controller.ShowMode(run2);
        Layout(canvas);
        Program.Check(breedingSlot.Child is AuctionRunning2_3 && Field(breedingHost, "_flowTextAnimation") == null, "page with its own note remains unwrapped and stops hidden common note");
        controller.ShowMode(run1);
        Layout(canvas);
        Program.Check(ReferenceEquals(breedingSlot.Child, breedingHost) && Field(breedingHost, "_flowTextAnimation") != null, "return from standalone note page resumes shared host");
        Program.Check(errors.Text.Length == 0, "shared-note transitions have no binding errors: " + errors.Text);
    }

    private static void CheckSettingsSnapshot()
    {
        var canvas = new Canvas();
        using var controller = new DisplayTestController(canvas);
        var settings = new TestSettings { Resolution = Resolution.Size128x64, Rows = [[1]], HouseCode = "TEST" };
        controller.Configure(settings, SampleKind.Normal);
        settings.Resolution = Resolution.Size160x64;
        settings.HouseCode = "CHANGED_IN_EDITOR";
        settings.Rows[0][0] = 99;
        controller.ShowMode(controller.Modes.First(mode => mode.Page == 1));
        var border = (Border)canvas.Children[0];
        var view = (FrameworkElement)border.Child;
        Program.Check(view.GetType().Name == "AuctionRunning1_64", "editing pending settings does not change unprepared views before Apply");
        Program.Check(((AuctionContPanelViewModel)view.DataContext).CowInfo.SpaceIndex == "1", "pending layout edit does not change active slot snapshot");
        controller.Configure(settings, SampleKind.Normal);
        controller.ShowMode(controller.Modes.First(mode => mode.Page == 1));
        Program.Check(((Border)canvas.Children[0]).Child.GetType().Name == "AuctionRunning1_160_64", "Configure applies changed settings as a new cache generation");
    }

    private static void Measure(int count, BindingErrors errors)
    {
        var canvas = new Canvas();
        using var controller = new DisplayTestController(canvas);
        var settings = new TestSettings
        {
            Resolution = Resolution.Size128x128,
            Rows = Enumerable.Range(0, count / 20).Select(row => Enumerable.Range(row * 20 + 1, 20).ToList()).ToList()
        };
        controller.Configure(settings, SampleKind.LongNames);
        using var source = Host(canvas);
        using var process = Process.GetCurrentProcess();
        long memoryBefore = GC.GetTotalMemory(true);
        TimeSpan cpuBefore = process.TotalProcessorTime;
        var preparation = Stopwatch.StartNew();
        controller.PrepareAll();
        preparation.Stop();
        int cacheCount = controller.CachedViewCount;
        long memoryPrepared = GC.GetTotalMemory(true);
        var watches = new List<double>();
        var transitions = new List<double>();
        errors.Clear();
        for (int cycle = 0; cycle < 3; cycle++)
        foreach (var mode in controller.Modes.Where(mode => mode.IsSupported))
        {
            var watch = Stopwatch.StartNew();
            controller.ShowMode(mode);
            Layout(canvas);
            watches.Add(watch.Elapsed.TotalMilliseconds);
            transitions.Add(controller.LastTransitionMs);
            Program.Check(controller.CachedViewCount == cacheCount, "100/200 repeated transition retains bounded cache");
        }
        long memoryAfter = GC.GetTotalMemory(true);
        for (int cycle = 0; cycle < 3; cycle++)
        foreach (var mode in controller.Modes.Where(mode => mode.IsSupported))
        {
            controller.ShowMode(mode);
            Layout(canvas);
            Program.Check(controller.CachedViewCount == cacheCount, "second 100/200 cycle block retains the same cache count");
        }
        long memoryAfterMore = GC.GetTotalMemory(true);
        TimeSpan cpu = process.TotalProcessorTime - cpuBefore;
        Program.Check(errors.Text.Length == 0, "100/200 rendered binding trace is clean: " + errors.Text);
        source.RootVisual = null;
        controller.Dispose();
        Program.Drain();
        long memoryReleased = GC.GetTotalMemory(true);
        Console.WriteLine($"Synthetic WPF {count} slots: preparation={preparation.Elapsed.TotalMilliseconds:F1} ms; cachedViews={cacheCount}; first {watches.Count} transitions with layout/dispatcher mean={watches.Average():F1} ms max={watches.Max():F1} ms; attachment mean={transitions.Average():F1} ms; process CPU for six cycles={cpu.TotalMilliseconds:F1} ms; managed MiB baseline={memoryBefore / 1048576d:F1}, prepared={memoryPrepared / 1048576d:F1}, after3Cycles={memoryAfter / 1048576d:F1}, after6Cycles={memoryAfterMore / 1048576d:F1}, afterDispose={memoryReleased / 1048576d:F1}. Hidden HWND; no physical board or baseline comparison. Memory observations include WPF caches, not a long-duration leak proof.");
    }

    private static void CheckControlWindow(BindingErrors errors)
    {
        errors.Clear();
        var window = new ControlWindow(new TestSettings(), commandPipeName: "GAMSTest.Checks." + Guid.NewGuid().ToString("N"));
        try
        {
            // No Show(): inspect the actual control window without opening its display window.
            var content = (FrameworkElement)window.Content;
            content.Measure(new System.Windows.Size(444, 171));
            content.Arrange(new Rect(0, 0, 444, 171));
            content.UpdateLayout();
            Program.Drain();
            Program.Check(window.Width == 460 && window.Height == 210 && content.ActualWidth <= 444, "reference compact control window dimensions preserved");
            var selector = (ComboBox)window.FindName("BoardSizeSelector");
            Program.Check(selector.Items.Count == 4 && (Resolution)selector.SelectedValue == Resolution.Size128x128, "size selector shows current XML/default resolution");
            selector.SelectedValue = Resolution.Size128x64;
            Program.Drain();
            Program.Check(((GAMSTest.ViewModels.ControlWindowViewModel)window.DataContext).SelectedResolution == Resolution.Size128x64, "size selector updates view model through two-way binding");
            ((GAMSTest.ViewModels.ControlWindowViewModel)window.DataContext).SelectedResolution = Resolution.Size128x128;
            Program.Drain();
            Program.Check((Resolution)selector.SelectedValue == Resolution.Size128x128, "view model size updates selection");
            var openBoard = (Button)window.FindName("OpenBoardButton");
            var buttonOrigin = openBoard.TranslatePoint(new Point(0, 0), content);
            Program.Check(buttonOrigin.Y >= 0 && buttonOrigin.Y + openBoard.ActualHeight <= 151 && buttonOrigin.X + openBoard.ActualWidth <= 424, "Board.XML picker button fits compact window client area");
            Program.Check(errors.Text.Length == 0, "actual control window bindings are valid: " + errors.Text);
            ExportScreenshot(content, "ControlWindow", (int)Math.Ceiling(content.ActualWidth), (int)Math.Ceiling(content.ActualHeight));
        }
        finally { window.Close(); Program.Drain(); }
    }

    private static HwndSource Host(Canvas canvas)
    {
        var source = new HwndSource(new HwndSourceParameters("Offline GAMSTest validation")
        { Width = Math.Min((int)canvas.Width, 2560), Height = Math.Min((int)canvas.Height, 1440), WindowStyle = unchecked((int)0x80000000) });
        source.RootVisual = canvas;
        Layout(canvas);
        return source;
    }

    private static void Layout(Canvas canvas)
    {
        canvas.Measure(new System.Windows.Size(canvas.Width, canvas.Height));
        canvas.Arrange(new Rect(0, 0, canvas.Width, canvas.Height));
        canvas.UpdateLayout();
        Program.Drain();
    }

    private static void ExportScreenshot(Canvas canvas, string name)
        => ExportScreenshot(canvas, name, (int)canvas.Width, (int)canvas.Height);

    private static void ExportScreenshot(FrameworkElement view, string name, int width, int height)
    {
        if (Program.ScreenshotsDirectory == null) return;
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(view);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(Program.ScreenshotsDirectory, name + ".png"));
        encoder.Save(stream);
    }

    private static void PumpFor(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = duration };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    private sealed class BindingErrors : TraceListener
    {
        private readonly StringBuilder _messages = new();
        public string Text => _messages.ToString();
        public override void Write(string? message) => _messages.Append(message);
        public override void WriteLine(string? message) => _messages.AppendLine(message);
        public void Clear() => _messages.Clear();
    }
}
