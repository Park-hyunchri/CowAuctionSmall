using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.ViewModels;
using GAMSTest.Models;
using GAMSTest.Services;
using Size = CowAuctionSmall.Presentation.Display.DisplaySizeParser.DisplaySize;

internal static class SokchoChecks
{
    internal static void Run()
    {
        var factory = new AuctionViewFactory();
        foreach (string title in new[] { "최저가", "행사용", " 행사용 " })
        {
            bool isEvent = title.Trim() == "행사용";
            var options = FactoryChecks.Options(Size.Size128x64, "8808990806426") with { LowestPriceTitle = title };
            Program.Check(AuctionViewFactory.GetRunningPageNumbers(options).SequenceEqual(isEvent ? new[] { 1 } : new[] { 1, 2 }), "Sokcho running page list: " + title);
            Program.Check(factory.CreateRunning(options, 1)?.GetType().Name == (isEvent ? "SokchoEventRun_64" : "SokchoRunning1_64"), "Sokcho first page: " + title);
            Program.Check(isEvent ? factory.CreateRunning(options, 2) == null : factory.CreateRunning(options, 2)?.GetType().Name == "SokchoRunning2_64", "Sokcho second page: " + title);
            Program.Check(factory.CreateRunning(options, 3) == null && factory.CreateRunning(options, 4) == null, "Sokcho never falls back to generic pages 3/4");
            Program.Check(factory.CreateSold(options).GetType().Name == (isEvent ? "SokchoEventResult_64" : "SokchoSold_64"), "Sokcho sold selection: " + title);
            Program.Check(factory.CreateUnSold(options).GetType().Name == (isEvent ? "SokchoEventResult_64" : "SokchoUnSold_64"), "Sokcho unsold selection: " + title);
            foreach (int requested in new[] { 0, 1, 2, 3, 4, 9 })
                Program.Check(AuctionViewFactory.LimitRunningPageCount(options, requested) == Math.Clamp(requested, 1, isEvent ? 1 : 2), "Sokcho configured page cap");
        }
        var other = FactoryChecks.Options(Size.Size128x64, "TEST") with { LowestPriceTitle = "행사용" };
        Program.Check(factory.CreateRunning(other, 1)?.GetType().Name == "AuctionRunning1_64" && factory.CreateSold(other).GetType().Name == "AuctionSold_64", "other houses retain ordinary 64-height views even with event title");
        Program.Check(AuctionViewFactory.LimitRunningPageCount(other, 4) == 4, "other houses retain four-page cap");
        var tall = FactoryChecks.Options(Size.Size128x128, "8808990806426") with { LowestPriceTitle = "행사용" };
        Program.Check(AuctionViewFactory.LimitRunningPageCount(tall, 4) == 2 && !AuctionViewFactory.IsSokchoEvent(tall), "Sokcho 128x128 retains two pages without 64-height event selection");
        CheckRuntimePages();
        CheckViews(factory);
        CheckTool();
    }

    private static void CheckRuntimePages()
    {
        Type storeType = typeof(DisplaySelect).Assembly.GetType("CowAuctionSmall.Presentation.Display.DisplayPanelStore", true)!;
        using var store = (IDisposable)Activator.CreateInstance(storeType, BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { new ObservableCollection<AuctionContPanelViewModel>() }, null)!;
        var display = (DisplaySelect)RuntimeHelpers.GetUninitializedObject(typeof(DisplaySelect));
        void Set(string field, object value) => typeof(DisplaySelect).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(display, value);
        Set("_panelStates", store);
        Set("pageTime", new List<int> { 25, 7, 5, 5 });
        Set("_displaySize", Size.Size128x64);
        Set("_setCustomDisplay", new SetCustomDisplay());
        Set("_userInfo", new UserInfo { Auction = new Auction { AuctionHouseCode = "TEST", BoardPage = "4" } });
        Set("_nhCode", "TEST");
        Set("_requestedRunningPage", 4);
        Set("_totalRunningPage", 4);
        var panel = new VirtualizingStackPanel { Width = 128, Height = 64 };
        using var vm = new AuctionContPanelViewModel(new gValues { CowDistinction = "1" }, panel, 4);
        display.InitializePages(panel, vm, 4, "N", "N", "N", "1");
        foreach (string title in new[] { "최저가", "행사용", "최저가" })
        {
            var user = new UserInfo { Auction = new Auction { AuctionHouseCode = "8808990806426", BoardPage = "4", LowestPriceTitle = title } };
            display.ApplyAuthenticatedUserInfo(user);
            int requested = (int)typeof(DisplaySelect).GetField("_requestedRunningPage", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(display)!;
            Program.Check(requested == (title == "행사용" ? 1 : 2), "authenticated runtime house update applies Sokcho page cap");
            display.InitializePages(panel, vm, requested, "N", "N", "N", "1");
            object state = storeType.GetMethod("Get", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(store, new object[] { panel })!;
            var pages = (List<UserControl>)state.GetType().GetField("RunningPages")!.GetValue(state)!;
            Program.Check(pages.Count == requested && pages.All(page => page.GetType().Name.StartsWith("Sokcho")), "product registration uses only Sokcho dedicated pages");
            Program.Check(pages[0].GetType().Name == (title == "행사용" ? "SokchoEventRun_64" : "SokchoRunning1_64"), "house/event transitions do not reuse the previous generic or Sokcho mode's cached page");
        }
    }

    private static void CheckViews(AuctionViewFactory factory)
    {
        using var errors = new Errors();
        var trace = PresentationTraceSources.DataBindingSource;
        var previous = trace.Switch.Level;
        trace.Switch.Level = SourceLevels.Error;
        trace.Listeners.Add(errors);
        try
        {
            var options = FactoryChecks.Options(Size.Size128x64, "8808990806426");
            var eventOptions = options with { LowestPriceTitle = "행사용" };
            var views = new[] { factory.CreateRunning(options, 1)!, factory.CreateRunning(options, 2)!, factory.CreateRunning(eventOptions, 1)!, factory.CreateSold(options), factory.CreateUnSold(options), factory.CreateSold(eventOptions) };
            foreach (var view in views)
            {
                var panel = new VirtualizingStackPanel();
                var source = new gValues
                {
                    CowDistinction = "1", Sex = "암", EntityNumber = "1234 5678 9", OwnerName = "가상농가", Location = "가상지역",
                    LowestPrice = "12345", BidPrice = "23456", Bidder = "901", Birth = "26.01.01", KPN = "1234", BloodEntityNumber = "TEST",
                    Note = "가상 비고가 길게 표시되는 속초 전용 화면 검사입니다", CalvingNumber = "2", Blood = "3", MotherLevel = "혈통", Weight = "123", Pregnant = "미정"
                };
                using var vm = new AuctionContPanelViewModel(source, panel, 2);
                bool running = view.GetType().Namespace!.EndsWith("Running");
                view.Width = 128; view.Height = 64;
                view.DataContext = running ? vm : vm.ItemDisplay;
                using var host = new HwndSource(new HwndSourceParameters("Offline Sokcho view") { Width = 128, Height = 64, WindowStyle = unchecked((int)0x80000000) });
                for (int repeat = 0; repeat < 2; repeat++)
                {
                    errors.Text.Clear();
                    host.RootVisual = view;
                    view.Measure(new System.Windows.Size(128, 64)); view.Arrange(new Rect(0, 0, 128, 64)); view.UpdateLayout(); Program.Drain();
                    Program.Check(errors.Text.Length == 0, "Sokcho real XAML bindings: " + view.GetType().Name + ": " + errors.Text);
                    var text = Descendants(view).OfType<TextBlock>().Select(block => block.Text).ToArray();
                    Program.Check(text.Any(value => value.Contains("1234 5678 9")), "Sokcho view displays the restored entity number");
                    if (view.GetType().Name is "SokchoRunning1_64" or "SokchoSold_64" or "SokchoUnSold_64")
                        Program.Check(text.Contains("12,345"), "Sokcho migrated price binding retains comma formatting");
                    host.RootVisual = null; Program.Drain();
                    if (view.GetType().Name is "SokchoRunning1_64" or "SokchoRunning2_64")
                        Program.Check(view.GetType().GetField("_scrollingText", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view) == null, "Sokcho unload releases note animation");
                }
                view.DataContext = null;
            }
        }
        finally { trace.Listeners.Remove(errors); trace.Switch.Level = previous; }
    }

    private static void CheckTool()
    {
        foreach (string title in new[] { "최저가", "행사용" })
        {
            var canvas = new Canvas();
            using var controller = new DisplayTestController(canvas);
            controller.Configure(new TestSettings { Resolution = Size.Size128x64, HouseCode = "8808990806426", LowestPriceTitle = title, PageCount = 4, Rows = [[7]] }, SampleKind.Normal);
            Program.Check(controller.Modes.Count(mode => mode.Key.StartsWith("RUN")) == (title == "행사용" ? 1 : 2), "GAMSTest shares Sokcho event/auction page list");
            var run = controller.Modes.First(mode => mode.Key == "RUN1");
            controller.ShowMode(run);
            var view = (UserControl)((Border)canvas.Children[0]).Child;
            Program.Check(view.GetType().Name == (title == "행사용" ? "SokchoEventRun_64" : "SokchoRunning1_64") && !((AuctionContPanelViewModel)view.DataContext).IsRunning, "manual Sokcho inspection uses dedicated view without blinking");
            controller.StartCycle(TimeSpan.FromSeconds(5));
            Program.Check(((AuctionContPanelViewModel)view.DataContext).IsRunning, "automatic Sokcho inspection retains running state");
            controller.ShowMode(run);
            Program.Check(!controller.IsCycling && !((AuctionContPanelViewModel)view.DataContext).IsRunning, "manual same-page Sokcho inspection disables running state");
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(root, index))) yield return child;
    }

    private sealed class Errors : TraceListener
    {
        internal StringBuilder Text { get; } = new();
        public override void Write(string? message) => Text.Append(message);
        public override void WriteLine(string? message) => Text.AppendLine(message);
    }
}
