using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views.Size128_128.Running;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Controls;

internal static class DisplayLifetimeChecks
{
    internal static void Run()
    {
        CheckPageSelection();
        CheckSoldSelection();
        DateTime epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        int[] durations = { 5, 7, 3 };
        foreach (var sample in new[] { (0, 0, 5), (4, 0, 1), (5, 1, 7), (11, 1, 1), (12, 2, 3), (15, 0, 5) })
        {
            var result = RunningPageSchedule.Calculate(epoch.AddSeconds(sample.Item1), 3, durations);
            Program.Check(result == (sample.Item2, sample.Item3), "UTC page schedule preserves duration boundary: " + sample.Item1);
        }
        Program.Check(RunningPageSchedule.Calculate(epoch.AddSeconds(6), 1, durations) == (0, 4), "one-page schedule uses only first duration");
        Program.Check(RunningPageSchedule.Calculate(epoch.AddSeconds(12), 99, durations) == (2, 3), "page count is clamped to configured durations");

        Type storeType = typeof(DisplaySelect).Assembly.GetType("CowAuctionSmall.Presentation.Display.DisplayPanelStore", true)!;
        var running = new ObservableCollection<AuctionContPanelViewModel>();
        using var store = (IDisposable)Activator.CreateInstance(storeType,
            BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { running }, null)!;
        MethodInfo get = storeType.GetMethod("Get", BindingFlags.Instance | BindingFlags.NonPublic)!;
        MethodInfo clear = storeType.GetMethod("ClearRunning", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var panel = new VirtualizingStackPanel();
        object state = get.Invoke(store, new object[] { panel })!;
        Program.Check(ReferenceEquals(state, get.Invoke(store, new object[] { panel })), "panel store reuses state for same panel");
        var cow = new gValues { Sex = "수", SpaceIndex = "7" };
        using var model = new AuctionContPanelViewModel(cow, null!, panel, new List<int> { 5 }, 1);
        running.Add(model);
        var host = new RunningNoteHost128 { ScrollOwner = model, PageContent = new TextBlock { Text = "fixture" } };
        host.SetBinding(RunningNoteHost128.ItemDisplayProperty,
            new System.Windows.Data.Binding(nameof(model.ItemDisplay)) { Source = model, Mode = System.Windows.Data.BindingMode.OneWay });
        Set(state, "RunningViewModel", model);
        Set(state, "RunningNoteHost", host);
        Set(state, "LastData", cow);
        panel.DataContext = cow;
        clear.Invoke(store, new[] { state, panel });
        Program.Check(running.Count == 0 && panel.DataContext == null && Field(state, "LastData") == null,
            "clearing running state removes item and stale panel data");
        Program.Check(host.DataContext == null && host.PageContent == null && host.ItemDisplay == null && host.ScrollOwner == null
            && !System.Windows.Data.BindingOperations.IsDataBound(host, RunningNoteHost128.ItemDisplayProperty),
            "shared note host releases content, display binding and scroll owner");
        Program.Check((bool)typeof(AuctionContPanelViewModel).GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(model)!,
            "clearing running state disposes its view model");
        panel.Children.Add(new TextBlock { Text = "fixture" });
        store.Dispose();
        Program.Check(panel.Children.Count == 0 && running.Count == 0, "store disposal clears panel children and running collection");
    }

    private static void CheckPageSelection()
    {
        // Bypass the production constructor so page tests never start network sync or timers.
        var selectorType = typeof(DisplaySelect);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var storeType = selectorType.Assembly.GetType("CowAuctionSmall.Presentation.Display.DisplayPanelStore", true)!;
        var updateCount = selectorType.GetMethod("UpdateRunningPageCount", flags)!;
        foreach (var scenario in new[]
        {
            (Size: "128,128", House: "8808990817675", Requested: 3, Count: 3),
            (Size: "128,128", House: "8808990817675", Requested: 4, Count: 3),
            (Size: "128,128", House: "8808990657202", Requested: 3, Count: 2),
            (Size: "128,128", House: "8808990657202", Requested: 4, Count: 2),
            (Size: "128,128", House: "8808990657202", Requested: 1, Count: 1),
            (Size: "128,128", House: "8808990657202", Requested: 2, Count: 2),
            (Size: "128,128", House: "8808990657202", Requested: 0, Count: 1),
            (Size: "128,128", House: "8808990656601", Requested: 3, Count: 2),
            (Size: "128,128", House: "fixture", Requested: 3, Count: 2),
            (Size: "160,64", House: "8808990657202", Requested: 3, Count: 2),
            (Size: "320,64", House: "fixture", Requested: 4, Count: 2),
            (Size: "128,64", House: "fixture", Requested: 4, Count: 4),
            (Size: "128,64", House: "0002", Requested: 4, Count: 4)
        })
        {
            var selector = (DisplaySelect)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(selectorType);
            var running = new ObservableCollection<AuctionContPanelViewModel>();
            using var store = (IDisposable)Activator.CreateInstance(storeType, flags, null, new object[] { running }, null)!;
            selectorType.GetField("_displaySize", flags)!.SetValue(selector, DisplaySizeParser.Parse(scenario.Size));
            selectorType.GetField("_nhCode", flags)!.SetValue(selector, string.Empty);
            selectorType.GetField("_setCustomDisplay", flags)!.SetValue(selector, new SetCustomDisplay());
            selectorType.GetField("_panelStates", flags)!.SetValue(selector, store);
            var totalField = selectorType.GetField("_totalRunningPage", flags)!;
            var requestedField = selectorType.GetField("_requestedRunningPage", flags)!;
            var runtimeUser = new CowAuctionSmall.Domain.Settings.UserInfo
            {
                Auction = new CowAuctionSmall.Domain.Settings.Auction
                {
                    AuctionHouseCode = string.Empty,
                    BoardPage = "1",
                    PageSetting = new CowAuctionSmall.Domain.Settings.PageSetting { BoardPage = scenario.Requested.ToString() }
                }
            };
            selectorType.GetField("_userInfo", flags)!.SetValue(selector, runtimeUser);
            updateCount.Invoke(selector, null);
            Program.Check((int)requestedField.GetValue(selector)! == Math.Clamp(scenario.Requested, 1, 4), "pre-login initialization preserves requested pages");
            runtimeUser.Auction.AuctionHouseCode = scenario.House;
            selector.ApplyAuthenticatedUserInfo(runtimeUser);
            Program.Check((int)requestedField.GetValue(selector)! == Math.Clamp(scenario.Requested, 1, 4), "authentication retains the configured page limit");
            Program.Check(runtimeUser.Auction.PageSetting.BoardPage == scenario.Requested.ToString(), "authentication keeps the configured page count intact");
            runtimeUser.Auction.PageSetting = null;
            runtimeUser.Auction.BoardPage = scenario.Requested.ToString();
            selector.ApplyAuthenticatedUserInfo(runtimeUser);
            Program.Check((int)requestedField.GetValue(selector)! == Math.Clamp(scenario.Requested, 1, 4), "authentication also supports the legacy page count setting");
            var dimensions = scenario.Size.Split(',').Select(int.Parse).ToArray();
            var panel = new VirtualizingStackPanel { Width = dimensions[0], Height = dimensions[1] };
            var cow = new gValues { CowDistinction = "1", Sex = "수", Is_Nh_QQuri = "N", Note = "SYNTHETIC_NOTE" };
            var times = new List<int> { 5, 7, 2, 3 };
            selectorType.GetField("pageTime", flags)!.SetValue(selector, times);
            selectorType.GetProperty("TodayAuctionItems")!.SetValue(selector, running);
            selector.DisplayRunning(panel, cow, 10);
            object state = storeType.GetMethod("Get", flags)!.Invoke(store, new object[] { panel })!;
            var model = (AuctionContPanelViewModel)Field(state, "RunningViewModel")!;
            var pages = (List<UserControl>)Field(state, "RunningPages")!;
            int count = (int)totalField.GetValue(selector)!;
            Program.Check(pages.Count == scenario.Count && count == pages.Count, "registered pages determine the actual rotation count: " + scenario);
            Program.Check(pages.All(page => ReferenceEquals(page.DataContext, model)
                && page.Width == panel.Width && page.Height == panel.Height), "pages preserve model and display dimensions");
            if (scenario.Size == "128,128")
            {
                Program.Check(scenario.House == "8808990817675"
                    ? pages[0] is CowAuctionSmall.Views.Size128_128.Running.CustomAuctionRunning1.Jangseong
                    : pages[0] is Standard_non_QQuri_Run1, "128x128 keeps its house-specific first page");
                if (count >= 2)
                    Program.Check(scenario.House switch
                        {
                            "8808990817675" => pages[1] is CowAuctionSmall.Views.Size128_128.Running.CustomAuctionRunning2.TestRunning2,
                            "8808990657202" => pages[1] is CowAuctionSmall.Views.Size128_128.Running.CustomAuctionRunning2.HoengseongRun2,
                            _ => pages[1] is CowAuctionSmall.Views.AuctionRunning2
                        },
                        "128x128 keeps its house-specific second page");
                if (count == 3)
                    Program.Check(pages[2] is CowHistorySearchRun && pages[2].Name == "RunPage3", "third page selects the imported history view");
            }
            else if (scenario.Size == "128,64")
            {
                string[] expected = scenario.House == "0002"
                    ? new[] { "AuctionRunning1_64", "AuctionRunning3_64", "AuctionRunning4_64", "AuctionRunning2_64" }
                    : new[] { "AuctionRunning1_64", "AuctionRunning2_64", "AuctionRunning3_64", "AuctionRunning4_64" };
                Program.Check(pages.Select(page => page.GetType().Name).SequenceEqual(expected), "128x64 retains normal and special four-page order");
            }
            else
            {
                string expectedNamespace = scenario.Size == "160,64" ? "SIze_160_64" : "Size_320_64";
                Program.Check(pages.All(page => page.GetType().Namespace!.Contains(expectedNamespace)), "wide display retains matching page resolution");
            }
            var originals = pages.ToArray();
            selector.DisplayRunning(panel, cow, 10);
            Program.Check(ReferenceEquals(model, Field(state, "RunningViewModel")) && ReferenceEquals(pages, Field(state, "RunningPages")), "repeated item display does not recreate unavailable pages");
            selector.InitializePages(panel, model, scenario.Requested, "N", "N", "N", "1");
            Program.Check(originals.SequenceEqual((List<UserControl>)Field(state, "RunningPages")!), "initialization reuses all cached pages including page three");
            int elapsed = 0;
            for (int i = 0; i <= count; i++)
            {
                var (index, _) = RunningPageSchedule.Calculate(DateTime.UnixEpoch.AddSeconds(elapsed), count, times);
                Program.Check(index == i % count, "rotation follows supported pages and wraps to first");
                selector.DisplayRunningPageNum(panel, cow, index + 1);
                var displayed = panel.Children[0] is RunningNoteHost128 host ? host.PageContent : panel.Children[0];
                Program.Check(ReferenceEquals(displayed, pages[index]), "rotation displays the corresponding cached page");
                elapsed += times[index];
            }
        }
    }

    private static void CheckSoldSelection()
    {
        // Exercise the real display/cache path without starting network connections or timers.
        var selectorType = typeof(DisplaySelect);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var storeType = selectorType.Assembly.GetType("CowAuctionSmall.Presentation.Display.DisplayPanelStore", true)!;
        var selector = (DisplaySelect)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(selectorType);
        using var store = (IDisposable)Activator.CreateInstance(storeType, flags, null,
            new object[] { new ObservableCollection<AuctionContPanelViewModel>() }, null)!;
        var user = new CowAuctionSmall.Domain.Settings.UserInfo
        {
            Auction = new CowAuctionSmall.Domain.Settings.Auction { IsShowQQuri = "N", BidderName = "B" }
        };
        selectorType.GetField("_displaySize", flags)!.SetValue(selector, DisplaySizeParser.Parse("128,128"));
        selectorType.GetField("_nhCode", flags)!.SetValue(selector, "8808990837314");
        selectorType.GetField("_userInfo", flags)!.SetValue(selector, user);
        selectorType.GetField("_setCustomDisplay", flags)!.SetValue(selector, new SetCustomDisplay());
        selectorType.GetField("_panelStates", flags)!.SetValue(selector, store);
        var panel = new VirtualizingStackPanel { Width = 128, Height = 128 };
        var cow = new gValues { CowDistinction = "1", Is_Nh_QQuri = "X", BidderName = "B", Sex = "수" };
        object state = storeType.GetMethod("Get", flags)!.Invoke(store, new object[] { panel })!;
        UserControl Show(string expected)
        {
            selector.DisplaySold(panel, cow);
            var view = (UserControl)Field(state, "SoldView")!;
            Program.Check(view.GetType().Name == expected, "actual sold display selects " + expected);
            return view;
        }
        var initial = Show("QQuriSold");
        Program.Check(ReferenceEquals(initial, Show("QQuriSold")), "unchanged sold display reuses its view");
        cow = cow.Clone(); cow.CowDistinction = "2";
        Show("QQuriSold_Weight");
        user.Auction.IsShowQQuri = "X";
        Show("Standard_non_X_Sold");
        user.Auction.IsShowQQuri = "Y";
        Show("QQuriSold_Weight");
        cow = cow.Clone(); cow.CowDistinction = "3";
        Show("QQuriSold_Weight");
        cow = cow.Clone(); cow.CowDistinction = "1";
        Show("QQuriSold");
        foreach (var entry in new[]
        {
            ("8808990817675", "JangseongSold"), ("8808990643625", "YangpyeongSold"),
            ("8808990656557", "YecheonSold"), ("8808990656106", "HaenamJindoSold"),
            ("8808990661315", "HwasunSold")
        })
        {
            selectorType.GetField("_nhCode", flags)!.SetValue(selector, entry.Item1);
            Show(entry.Item2);
        }
        selectorType.GetField("_nhCode", flags)!.SetValue(selector, "8808990656885");
        Show("QQuriSold");
        user.Auction.LowestPriceTitle = "행사용";
        Show("HoengseongBunyangSold");
        user.Auction.LowestPriceTitle = "최저가";
        Show("QQuriSold");
        cow = cow.Clone(); cow.CowDistinction = "6";
        Show("GoatSold");
    }

    private static object? Field(object target, string name) => target.GetType().GetField(name)!.GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name)!.SetValue(target, value);
}
