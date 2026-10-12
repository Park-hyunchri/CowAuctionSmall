using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.ViewModels;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;

internal static class FooterStatusChecks
{
    internal static void Run()
    {
        var status = new MainStatusViewModel();
        Program.Check(status.ServerText.Contains("확인 중") && status.AuctionText.Contains("확인 중"), "footer starts unknown");
        status.SetAuction(new(20, AuctionDisplayPhase.Running, 7));
        Program.Check(status.AuctionText == "단일경매 · 진행 · 7번", "structured single state and item formatting");
        foreach (var pair in new[] { ("2001", "접속 실패"), ("2002", "중복 접속"), ("disconnected", "연결 종료"), ("unknown", "확인 중") })
        {
            status.SetConnection("2000");
            status.SetAuction(new(10, AuctionDisplayPhase.Running, null));
            status.SetConnection(pair.Item1);
            Program.Check(status.ServerText.Contains(pair.Item2) && status.AuctionText.Contains("확인 중"), "connection failure clears stale running: " + pair.Item1);
        }
        status.SetConnection("2000");
        Program.Check(status.ServerBrush == Brushes.LimeGreen && status.AuctionText.Contains("확인 중"), "reconnect alone cannot imply auction progress");
        status.SetConnection("2002");
        status.SetConnection("disconnected");
        status.SetAuction(new(10, AuctionDisplayPhase.Running, null));
        Program.Check(status.ServerText.Contains("중복 접속") && status.AuctionText.Contains("확인 중"), "closed duplicate session preserves reason and ignores queued auction state");
        status.SetConnection("2000");
        foreach (string setting in new[] { "Y", "N", "X" })
        {
            status.SetRootFarm(setting);
            Program.Check(status.RootFarmText == "뿌리농가 " + setting, "root farm setting: " + setting);
        }
        status.SetRootFarm(null);
        Program.Check(status.RootFarmText.Contains("미확인"), "missing setting remains unknown");
        status.SetPage(new(2, 3, true, false, false, isTimerRunning: true));
        Program.Check(status.PageText == "페이지 2/3" && status.RotationText == "전환 중", "actual timer shows rotating");
        var dots = status.PageDots;
        int dotChanges = 0;
        dots.CollectionChanged += (_, _) => dotChanges++;
        status.SetPage(new(2, 3, true, false, false, isTimerRunning: true));
        Program.Check(dotChanges == 0, "duplicate indicator signal does not rebuild dots");
        status.SetPage(new(2, 3, true, false, false, isTimerRunning: false));
        Program.Check(status.RotationText == "정지", "unfrozen stopped timer is not reported rotating");
        status.SetPage(new(2, 3, false, false, false, isTimerRunning: false));
        Program.Check(status.RotationText == "동기화 수신 모드", "normal sub with no timer is not stopped");
        foreach (bool running in new[] { false, true })
        {
            status.SetPage(new(2, 3, false, false, true, isTimerRunning: running));
            Program.Check(status.RotationText == "로컬 복구 · " + (running ? "전환 중" : "정지"), "fallback distinguishes actual timer state");
        }
        status.SetPage(new(1, 1, true, false, false, isTimerRunning: true));
        Program.Check(status.PageText == "페이지 1/1" && status.RotationText == "단일 페이지" && dots.Count == 1, "one page remains visible");
        status.SetPage(new(1, 1, true, true, false, "StopTimer", false));
        Program.Check(status.RotationText == "고정" && status.RotationDetail.Contains("서버 신호"), "single-page freeze reason remains visible");

        var model = (MainWindowViewModel)RuntimeHelpers.GetUninitializedObject(typeof(MainWindowViewModel));
        Field("<StatusBar>k__BackingField").SetValue(model, status);
        Field("_dispatcher").SetValue(model, Dispatcher.CurrentDispatcher);
        Field("_boardContentHeight").SetValue(model, 256);
        model.AuctionSignalText = "최근 수신 11:25:10 · 경매 대기 신호";
        model.AuctionSectionText = "구분 0(전체) · 1구간 · 경매프로그램 선택 구간 1~999번";
        model.HistoryProgressText = "이력 처리 115/115건 · 소유주 변경 비고 87건 · 캐시 115 · 실패 0";
        model.MainWindowTextBox = "경매 정보 동기화를 완료했습니다.\n페이지 상태를 수신했습니다.";
        status.SetRootFarm("Y");
        status.SetAuction(new(10, AuctionDisplayPhase.Waiting, null));
        status.SetPage(new(2, 3, true, false, false, isTimerRunning: true));
        status.VersionText = "버전: 1.0.1.30";

        XNamespace wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var document = XDocument.Load(Path.Combine(Directory.GetCurrentDirectory(), "Views", "MainWindow.xaml"));
        var source = new XElement(document.Descendants(wpf + "Grid").Single(e => (string?)e.Attribute(x + "Name") == "MainStatusFooter"));
        source.SetAttributeValue(XNamespace.Xmlns + "x", x.NamespaceName);
        source.Attribute("SizeChanged")!.Remove();
        foreach (var attribute in source.Descendants().Attributes("TextChanged").ToArray()) attribute.Remove();
        var footer = (Grid)XamlReader.Parse(source.ToString());
        footer.DataContext = model;
        footer.SizeChanged += (_, e) => model.UpdateFooterHeight(e.NewSize.Height);
        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.Children.Add(new Border { Height = 256 });
        Grid.SetRow(footer, 1);
        content.Children.Add(footer);
        var scroll = new ScrollViewer { Content = content, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
        using var host = new HwndSource(new HwndSourceParameters("Offline footer layout")
        { Width = 1200, Height = 700, WindowStyle = unchecked((int)0x80000000) });
        host.RootVisual = scroll;
        double wideHeight = 0;
        foreach (int width in new[] { 1200, 768, 320, 128 })
        {
            model.MainWindowWidth = width;
            scroll.Width = width;
            for (int pass = 0; pass < 3; pass++)
            {
                scroll.Measure(new Size(width, 700));
                scroll.Arrange(new Rect(0, 0, width, 700));
                scroll.UpdateLayout();
                Pump();
            }
            double height = footer.ActualHeight;
            Console.WriteLine($"Footer layout: width={width}, actual={footer.ActualWidth}x{height}, desired={footer.DesiredSize}, window-height={model.MainWindowHeight}");
            if (width == 1200) wideHeight = height;
            Program.Check(height >= wideHeight && height < 500 && model.MainWindowHeight == 256 + (int)Math.Ceiling(height) + 4, "wrapped footer adjusts window height: " + width);
            var log = (TextBox)footer.FindName("MsgTextBox");
            Program.Check(log.ActualHeight >= 69 && log.ContextMenu?.Items.Count == 1, "log height and refresh menu survive: " + width);
            foreach (var cell in Visuals<Border>(footer).Where(b => b.Style != null && b.Parent is WrapPanel && b.Visibility == Visibility.Visible))
            {
                var position = cell.TransformToAncestor(footer).Transform(new Point());
                Program.Check(position.X >= -1 && position.X + cell.ActualWidth <= width + 1, "status cell fits available width: " + width);
            }
            Program.Check(Visuals<TextBlock>(footer).Any(t => t.Text == status.PageText), "actual XAML binds current/total page: " + width);
            if (width is 1200 or 320) SaveImage(source.ToString(), model, width, (int)Math.Ceiling(height));
        }
        host.RootVisual = null;

        Task.Run(() => Handler("OnNettyConnectionResultMsg").Invoke(model, new object[] { model, new NettyConnectionResultMessage("disconnected") })).GetAwaiter().GetResult();
        Pump();
        Program.Check(status.ServerText.Contains("연결 종료"), "background connection event updates UI dispatcher");
        Field("_disposed").SetValue(model, true);
        Handler("OnNettyConnectionResultMsg").Invoke(model, new object[] { model, new NettyConnectionResultMessage("2000") });
        Program.Check(status.ServerText.Contains("연결 종료"), "disposed main model ignores footer events");
    }

    private static FieldInfo Field(string name) => typeof(MainWindowViewModel).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static MethodInfo Handler(string name) => typeof(MainWindowViewModel).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
    private static IEnumerable<T> Visuals<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T value) yield return value;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            foreach (var child in Visuals<T>(VisualTreeHelper.GetChild(parent, i))) yield return child;
    }
    private static void SaveImage(string markup, object model, int width, int height)
    {
        // Render a root at (0,0); the live fixture footer has the board's vertical offset.
        var preview = (Grid)XamlReader.Parse(markup);
        preview.DataContext = model;
        using var host = new HwndSource(new HwndSourceParameters("Offline footer preview")
        { Width = width, Height = height, WindowStyle = unchecked((int)0x80000000) });
        host.RootVisual = preview;
        preview.Measure(new Size(width, height));
        preview.Arrange(new Rect(0, 0, width, height));
        preview.UpdateLayout();
        Pump();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(preview);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(AppContext.BaseDirectory, $"footer-{width}.png"));
        png.Save(stream);
        host.RootVisual = null;
    }
}
