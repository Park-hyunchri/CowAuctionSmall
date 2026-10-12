using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Infra.Api;
using CowAuctionSmall.Views.Common;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

internal static class BidderChecks
{
    internal static void Run()
    {
        CheckParsing();
        CheckFlow();
        CheckCombinedResult();
    }

    private static void CheckParsing()
    {
        var parser = new AnimalParseData();
        foreach (bool isSv in new[] { false, true })
        foreach (string house in new[] { "fixture", "8808990656106", "8808990643625", "8808990817675" })
        foreach (string mode in new[] { "Y", "X", "B", "N" })
        foreach (string name in new[] { "", "가", "가나", "가나다라", "가나다라마바사", "SYNTHETIC & NAME" })
        {
            var user = new UserInfo { Auction = new Auction { AuctionHouseCode = house, BidderName = mode, CowBirth = "N", ChangeSexName = "N" } };
            var packet = isSv ? ItemUpdateChecks.Sv(7) : ItemUpdateChecks.Api(7);
            packet[1] = house;
            packet[29] = "22";
            packet[30] = "901";
            packet[31] = "720";
            packet[isSv ? 36 : 38] = System.Web.HttpUtility.HtmlEncode(name);
            var actual = parser.ParseUpdate(string.Join('|', packet), user, isSv).Values;
            string expected = mode switch
            {
                "N" => "901",
                "X" when name.Length > 2 => name[..1] + "*" + name.Substring(2, 1),
                "X" when name.Length > 0 => name[..1] + "*",
                _ => name
            };
            Program.Check(actual.Bidder == expected, $"SV/API bidder preserves full names and existing mask/number rules: SV={isSv}, mode={mode}, length={name.Length}, actual={actual.Bidder}, expected={expected}");
            if (mode == "B")
                Program.Check(actual.BidderString == name && actual.BidderNum == "901", "combined display preserves full name and participant number");
        }
    }

    private static void CheckFlow()
    {
        _ = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var view = new BidderNameFlow { Text = "가나다라", Width = 100, Height = 22, FontSize = 13 };
        using var host = new HwndSource(new HwndSourceParameters("Offline bidder checks")
        {
            Width = 500, Height = 60, WindowStyle = unchecked((int)0x80000000)
        });
        var root = new Grid();
        root.Children.Add(view);
        host.RootVisual = root;
        Layout();
        Program.Check(!Scrolling(), "four characters fitting the viewport remain stationary");
        view.Width = 25; Layout();
        Program.Check(Scrolling(), "four characters overflow and scroll without a six-character minimum");
        view.Width = 100; Layout();
        Program.Check(!Scrolling() && ((TranslateTransform)((TextBlock)view.FindName("flowText")).RenderTransform).X == 0,
            "expanding viewport stops the clock and resets text position");
        view.Text = "가나다라마바사"; view.Width = 300; Layout();
        Program.Check(!Scrolling(), "long names fitting the viewport do not scroll merely because of length");
        view.Width = 50; Layout();
        Program.Check(Scrolling(), "long names scroll when their rendered width exceeds the viewport");
        view.Text = ""; Layout();
        Program.Check(!Scrolling(), "clearing the name stops scrolling");
        view.Text = "가나다라"; view.Width = 100; view.FontSize = 40; Layout();
        Program.Check(Scrolling(), "font-size changes reevaluate actual width");
        view.FontSize = 13; Layout();
        Program.Check(!Scrolling(), "smaller font fits and stops scrolling");
        for (int pass = 0; pass < 2; pass++)
        {
            root.Children.Clear(); Layout();
            Program.Check(Animation() == null, "unloading releases animation and subscriptions");
            root.Children.Add(view); Layout();
            Program.Check(Animation() != null && !Scrolling(), "reloading reevaluates the stationary name");
        }
        view.Width = 25;
        root.Children.Clear(); Layout();
        Program.Check(Animation() == null, "unloading cancels pending resize initialization");
        host.RootVisual = null;

        object? Animation() => Field(view, "_flowTextAnimation");
        bool Scrolling() => Animation() is object animation && (bool)Field(animation, "_isAnimating")!;
        void Layout()
        {
            root.Measure(new Size(500, 60));
            root.Arrange(new Rect(0, 0, 500, 60));
            root.UpdateLayout();
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }
    }

    private static void CheckCombinedResult()
    {
        var application = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        application.Resources["EntityNumberForeground"] = Brushes.Yellow;
        application.Resources["EntityNumberShortForeground"] = Brushes.White;
        SexDisplayChecks.EnsureResources();
        var selector = new CowAuctionSmall.Presentation.Display.SetCustomDisplay();
        var factory = new CowAuctionSmall.Presentation.Display.AuctionViewFactory(selector);
        foreach (string mode in new[] { "X", "Y", "N" })
        foreach (string category in new[] { "1", "2", "3" })
        foreach (string title in new[] { "최저가", "행사용" })
        {
            var expected = title == "행사용"
                ? typeof(CowAuctionSmall.Views.Size128_128.CustomAuctionSold.HoengseongBunyangSold)
                : typeof(CowAuctionSmall.Views.Size128_128.CustomAuctionSold.HoengseongSold);
            var options = new CowAuctionSmall.Presentation.Display.AuctionViewOptions
            {
                NhCode = "8808990656885", IsShowQquri = mode, CowDistinction = category, LowestPriceTitle = title
            };
            Program.Check(selector.CustomAuctionSold_128(options.NhCode, mode, category, title).GetType() == expected &&
                factory.CreateSold(options).GetType() == expected,
                $"Hoengseong dedicated sold view through selector and shared factory: {mode}/{category}/{title}");
        }
        foreach (string house in new[] { "fixture", "8808990656687", "8808990684321", "8808990656717", "8808990660783", "8808990656229", "8808990227283" })
        foreach (string mode in new[] { "X", "Y", "N" })
        foreach (string category in new[] { "1", "2", "3" })
        foreach (string title in new[] { "최저가", "행사용" })
        {
            var expected = mode == "X" ? typeof(CowAuctionSmall.Views.Size128_128.Standard_non_X_Sold)
                : category is "2" or "3" ? typeof(CowAuctionSmall.Views.Size128_128.QQuriSold_Weight)
                : typeof(CowAuctionSmall.Views.Size128_128.QQuriSold);
            var options = new CowAuctionSmall.Presentation.Display.AuctionViewOptions
            {
                NhCode = house, IsShowQquri = mode, CowDistinction = category, LowestPriceTitle = title
            };
            Program.Check(selector.CustomAuctionSold_128(house, mode, category, title).GetType() == expected &&
                factory.CreateSold(options).GetType() == expected,
                $"other houses use common sold views through selector and shared factory: {house}/{mode}/{category}/{title}");
        }
        foreach (var dedicated in new[]
        {
            (House: "8808990817675", View: typeof(CowAuctionSmall.Views.Size128_128.CustomAuctionSold.JangseongSold)),
            (House: "8808990643625", View: typeof(CowAuctionSmall.Views.Size128_128.CustomAuctionSold.YangpyeongSold)),
            (House: "8808990656557", View: typeof(CowAuctionSmall.Views.Size128_128.CustomAuctionSold.YecheonSold)),
            (House: "8808990656106", View: typeof(CowAuctionSmall.Views.Size128_128.CustomAuctionSold.HaenamJindoSold)),
            (House: "8808990661315", View: typeof(CowAuctionSmall.Views.Size128_128.CustomAuctionSold.HwasunSold))
        })
            Program.Check(selector.CustomAuctionSold_128(dedicated.House, "X", "2", "행사용").GetType() == dedicated.View,
                $"existing dedicated sold view retains priority: {dedicated.House}");
        foreach (string house in new[] { "fixture", "8808990656885" })
        foreach (string category in new[] { "5", "6" })
            Program.Check(selector.CustomAuctionSold_128(house, "X", category, "행사용") is CowAuctionSmall.Views.Size128_128.GoatSold,
                $"goat and horse selection retains priority: {house}/{category}");
        foreach (string mode in new[] { "Y", "N" })
        {
            Program.Check(selector.CustomAuctionSold_128("fixture", mode, "1", "") is CowAuctionSmall.Views.Size128_128.QQuriSold,
                $"ordinary result uses the unified view for {mode}");
            Program.Check(selector.CustomAuctionSold_128("8808990656717", mode, "1", "") is CowAuctionSmall.Views.Size128_128.QQuriSold,
                $"Gokseong result uses the unified view for {mode}");
            Program.Check(selector.CustomAuctionSold_128("fixture", mode, "2", "") is CowAuctionSmall.Views.Size128_128.QQuriSold_Weight,
                $"weight view is selected for category 2 under {mode}");
            Program.Check(selector.CustomAuctionSold_128("fixture", mode, "3", "") is CowAuctionSmall.Views.Size128_128.QQuriSold_Weight,
                $"category 3 uses the configured weight view under {mode}");
        }
        Program.Check(selector.CustomAuctionSold_128("8808990817675", "N", "1", "") is CowAuctionSmall.Views.Size128_128.CustomAuctionSold.JangseongSold,
            "Jangseong retains its dedicated result view");
        Program.Check(selector.CustomAuctionSold_128("8808990660783", "N", "1", "") is CowAuctionSmall.Views.Size128_128.QQuriSold,
            "Imsil now follows the configured common result instead of always showing weight");
        Program.Check(selector.CustomAuctionSold_128("fixture", "X", "2", "") is CowAuctionSmall.Views.Size128_128.Standard_non_X_Sold,
            "root-farm X selection retains its separate result view");
        Program.Check(selector.CustomAuctionSold_128("fixture", "N", "5", "") is CowAuctionSmall.Views.Size128_128.GoatSold,
            "goat selection retains priority over bidder mode");
        var view = selector.CustomAuctionSold_128("fixture", "N", "1", "");
        Program.Check(view is CowAuctionSmall.Views.Size128_128.QQuriSold,
            "ordinary B selection reaches the combined result view");
        view.Width = 128; view.Height = 128;
        using var host = new HwndSource(new HwndSourceParameters("Offline combined bidder checks")
        {
            Width = 400, Height = 160, WindowStyle = unchecked((int)0x80000000)
        });
        host.RootVisual = view;
        Publish("가나", "901");
        var stack = (StackPanel)view.FindName("spBidderNumberAndName");
        var flow = stack.Children.OfType<BidderNameFlow>().Single();
        var number = stack.Children.OfType<TextBlock>().Single();
        Program.Check(flow.Text == "가나" && !Scrolling(), "B view keeps a fitting short name stationary");
        Publish("가나다라마바사아", "901");
        Program.Check(flow.Text == "가나다라마바사아" && flow.ActualWidth > 0 && flow.ActualWidth < 128 && Scrolling(),
            "B view scrolls the full long name within its finite allocated width");
        Program.Check(NumberText() == "(901)" && number.IsVisible && !number.HasAnimatedProperties
            && number.RenderTransform.Value.IsIdentity, $"participant number remains fixed and visible below the scrolling name: text={NumberText()}, visible={number.IsVisible}, animated={number.HasAnimatedProperties}, transform={number.RenderTransform.Value}");
        view.Width = 320; Layout();
        Program.Check(!Scrolling(), "wider combined result view stops scrolling when the same name fits");
        view.Width = 128; Layout();
        Program.Check(Scrolling(), "shrinking the combined result view resumes name scrolling");
        Publish("다라", "902");
        Program.Check(flow.Text == "다라" && NumberText() == "(902)" && !Scrolling(),
            "new bidder snapshot updates both rows and stops scrolling for a short name");
        Publish("", "903");
        Program.Check(flow.Text == "" && NumberText() == "(903)" && !Scrolling(), "empty name clears previous display without hiding the number");
        Publish("가나다라마바사아", "904");
        host.RootVisual = null; Layout();
        Program.Check(Field(flow, "_flowTextAnimation") == null, "combined result unload releases name animation");
        host.RootVisual = view; Layout();
        Program.Check(flow.Text == "가나다라마바사아" && Scrolling() && NumberText() == "(904)",
            "combined result reload restores name flow and fixed number");
        var numberOnly = (TextBlock)view.FindName("tbBidderNumberOnly");
        var nameOnly = ((Grid)stack.Parent).Children.OfType<BidderNameFlow>().Single();
        foreach (string mode in new[] { "Y", "N", "X", "B" })
        {
            string text = mode switch { "N" => "905", "X" => "가*다", _ => "가나다라마바사아" };
            Publish(text, "905", mode);
            Program.Check(numberOnly.IsVisible == (mode == "N") && nameOnly.IsVisible == (mode is "Y" or "X")
                && stack.IsVisible == (mode == "B"), $"switching to {mode} shows only its bidder presentation");
            if (mode is "Y" or "X")
                Program.Check(nameOnly.Text == text, $"{mode} renders the supplied full or masked name unchanged");
        }
        host.RootVisual = null; Layout();

        string NumberText() => new System.Windows.Documents.TextRange(number.ContentStart, number.ContentEnd).Text.Replace(" ", "");
        bool Scrolling() => Field(flow, "_flowTextAnimation") is object animation && (bool)Field(animation, "_isAnimating")!;
        void Publish(string name, string participant, string mode = "B")
        {
            view.DataContext = new CowAuctionSmall.ViewModels.AuctionItemDisplayViewModel(new CowAuctionSmall.Domain.Auction.gValues
            {
                CowDistinction = "1", BidderName = mode, Bidder = name, BidderString = name, BidderNum = participant,
                EntityNumber = "1234 5678 9", LowestPrice = "100", BidPrice = "200"
            });
            Layout();
        }
        void Layout()
        {
            view.Measure(new Size(view.Width, 128));
            view.Arrange(new Rect(0, 0, view.Width, 128));
            view.UpdateLayout();
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }
    }

    private static object? Field(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(target);
}
