using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.Infra.XmlSettings;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Infra.Persistence;
using CowAuctionSmall.Application.AnimalHistory;
using CowAuctionSmall.Infra.Api;
using CowAuctionSmall.Infra.Logging;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Domain.AnimalHistory;
using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.ViewModels;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

static class UiChecks
{
    private static int _checks;
    private static void Check(bool condition, string name) { if (!condition) throw new Exception("UI: " + name); _checks++; }
    private static void Pump(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame);
    }
    private static IEnumerable<TextBlock> TextBlocks(DependencyObject node)
    {
        if (node is TextBlock text) yield return text;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            foreach (var child in TextBlocks(VisualTreeHelper.GetChild(node, i))) yield return child;
    }
    public static void Run()
    {
        // Generated fixtures only in the validation executable's own output directory.
        string root = AppContext.BaseDirectory;
        Environment.CurrentDirectory = root;
        Directory.CreateDirectory(Path.Combine(root, "Config"));
        File.WriteAllText(Path.Combine(root, "NLog.config"), "<nlog xmlns='http://www.nlog-project.org/schemas/NLog.xsd'><targets/><rules/></nlog>");
        File.WriteAllText(Path.Combine(root, "Config/Board.XML"), "<Board><Size>128,128</Size><MultiBoard Name='Test'><RowIdx>1,2</RowIdx></MultiBoard></Board>");
        File.WriteAllText(Path.Combine(root, "Config/users.XML"), "<Users><Authentication><Address>https://example.test/login</Address><UserID>fake</UserID><Password>fake</Password></Authentication><CurrentInfo><Address>https://example.test/api/v1/auction/{AuctionHouseCode}/entry/board?date=</Address><AddressQcn>https://example.test/qcn</AddressQcn></CurrentInfo><Auction><AuctionHouseCode>111</AuctionHouseCode><BoardPage>1</BoardPage><PageTimerPort>59484</PageTimerPort></Auction><AnimalTrace><IsAnimalProductsTraceability>Y</IsAnimalProductsTraceability><ServiceKey>fake</ServiceKey></AnimalTrace></Users>");
        var bmp = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgr24, null, new byte[] { 0, 0, 0 }, 3);
        var encoder = new BmpBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bmp));
        using (var file = File.Create(Path.Combine(root, "Config/logo.bmp"))) encoder.Save(file);
        var app = System.Windows.Application.Current ?? new System.Windows.Application();
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/CowAuctionSmall;component/Views/Common/SexDisplayStyles.xaml", UriKind.Relative) });
        var handler = new UiHandler();
        var cachePath = Path.Combine(Path.GetTempPath(), "UiCache_" + Guid.NewGuid().ToString("N"), "cache.db");
        var date = new DateTimeOffset(2026, 9, 15, 2, 0, 0, TimeSpan.Zero);
        using var cache = new DailyCacheService(cachePath, () => date);
        using var history = new AnimalHistoryService(cache, new AnimalHistoryClient(new HttpClient(handler)));
        var conn = new ServerConn(new HttpClient(handler), cache);
        var parser = new XmlParserCont(new BoardXmlParser(), new UserXmlParser());
        using var service = CreateService(conn, parser);
        using var vm = new MainWindowViewModel(parser, service, history);
        var container = vm.MainContainer;
        Check(container != null && vm.Panels.Count == 2, "panels before login");
        Check(vm.Panels.All(p => p.Children.Count == 1 && p.Children[0] is Image), "logos before login");
        Pump(300);
        Check(handler.TraceCalls == 0, "no history before login");
        handler.Login.SetResult(true); Pump(500);
        Check(ReferenceEquals(container, vm.MainContainer), "no panel rebuild");
        var cow = new gValues { SpaceIndex = "1", SipNumber = "1", TraceNumber = "000000000000001", Note = "ORIGINAL_NOTE", AuctionResultStatus = "22", CowDistinction = "1", Is_Nh_QQuri = "N", Birth = "20260101", Sex = "수", BidPrice = "100", LowestPrice = "100" };
        WeakReferenceMessenger.Default.Send(new DataChangedMessage(new List<gValues> { cow }));
        var window = new Window { Content = container, Width = 280, Height = 180, Left = -10000, Top = -10000, ShowActivated = false };
        window.Show(); Pump(500);
        Check(TextBlocks(vm.Panels[0]).Any(t => t.Text.Contains("ORIGINAL_NOTE")), "existing note immediate");
        handler.History.SetResult(true); Pump(700);
        Check(TextBlocks(vm.Panels[0]).Any(t => t.Text.Contains("OWNER_A → OWNER_B")), "owner note binding after response");
        Check(cow.Note == "ORIGINAL_NOTE" && cow.NoteChangeOwner == "", "source data unmodified");
        var updated = cow.Clone(); updated.BidPrice = "200";
        WeakReferenceMessenger.Default.Send(new DataChangedMessage(new List<gValues> { updated })); Pump(200);
        Check(handler.TraceCalls == 1, "auction refresh uses history cache");
        Check(TextBlocks(vm.Panels[0]).Any(t => t.Text.Contains("OWNER_A → OWNER_B")), "history retained after fresh auction object");
        foreach (string state in new[] { "11", "22", "23" })
        {
            updated.AuctionResultStatus = state;
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(new List<gValues> { updated })); Pump(250);
            Check(TextBlocks(vm.Panels[0]).Any(t => t.Text.Contains("OWNER_A → OWNER_B")), "state " + state);
            var coloredNote = TextBlocks(vm.Panels[0]).First(t => t.Text.Contains("OWNER_A → OWNER_B"));
            var coloredRuns = coloredNote.Inlines.OfType<System.Windows.Documents.Run>().ToArray();
            Check(coloredRuns.Length == 3 && ((SolidColorBrush)coloredRuns[0].Foreground).Color == Colors.Red && ((SolidColorBrush)coloredRuns[2].Foreground).Color == Colors.White, "two note colors state " + state);
        }
        // A delta for another panel must not lose ownership of the first displayed panel.
        var other = cow.Clone(); other.SpaceIndex = "2"; other.TraceNumber = "";
        WeakReferenceMessenger.Default.Send(new DataChangedMessage(new List<gValues> { other })); Pump(100);
        Check(TextBlocks(vm.Panels[0]).Any(t => t.Text.Contains("OWNER_A → OWNER_B")), "other panel delta");
        history.Configure(new AnimalTraceSettings()); date = date.AddDays(1);
        var rollover = cache.EnsureTodayAsync(); while (!rollover.IsCompleted) Pump(50); Pump(200);
        Check(!TextBlocks(vm.Panels[0]).Any(t => t.Text.Contains("OWNER_A → OWNER_B")), "day rollover clears rendered history");
        Check(TextBlocks(vm.Panels[0]).Any(t => t.Text.Contains("ORIGINAL_NOTE")), "rollover retains original note");
        window.Close(); vm.Dispose(); service.Dispose(); history.Dispose(); cache.Dispose();
        string usersPath = Path.Combine(root, "Config/users.XML");
        File.WriteAllText(usersPath, File.ReadAllText(usersPath).Replace("<IsAnimalProductsTraceability>Y</IsAnimalProductsTraceability>", "<IsAnimalProductsTraceability>N</IsAnimalProductsTraceability>"));
        var offHandler = new UiHandler(); offHandler.Login.SetResult(true);
        using (var offCache = new DailyCacheService(Path.Combine(Path.GetTempPath(), "OffCache_" + Guid.NewGuid().ToString("N"), "cache.db")))
        using (var offHistory = new AnimalHistoryService(offCache, new AnimalHistoryClient(new HttpClient(offHandler))))
        using (var offServer = CreateService(new ServerConn(new HttpClient(offHandler), offCache), parser))
        using (var offVm = new MainWindowViewModel(parser, offServer, offHistory))
        {
            Pump(500);
            var offCow = cow.Clone(); offCow.NoteChangeOwner = "SHOULD_NOT_DISPLAY_HISTORY";
            offVm.UpdatePanels(new System.Collections.ObjectModel.ObservableCollection<gValues> { offCow });
            var offWindow = new Window { Content = offVm.MainContainer, Width = 280, Height = 180, Left = -10000, Top = -10000, ShowActivated = false };
            offWindow.Show(); Pump(250);
            Check(TextBlocks(offVm.Panels[0]).Any(t => t.Text.Contains("ORIGINAL_NOTE")), "disabled retains original note");
            Check(!TextBlocks(offVm.Panels[0]).Any(t => t.Text.Contains("SHOULD_NOT_DISPLAY_HISTORY")), "disabled removes derived note");
            offVm.RefreshDisplayedOwnerHistory(); Pump(250);
            Check(offHandler.TraceCalls == 0, "disabled automatic and manual lookup no HTTP");
            offWindow.Close();
        }
        int port = 59500;
        foreach (string size in new[] { "128,64", "160,64", "320,64", "128,128" })
        {
            var user = new UserInfo { Auction = new Auction { AuctionHouseCode = "222", BoardPage = "1", PageTimerPort = (++port).ToString() } };
            using var display = new DisplaySelect(user, new BoardList { Size = size });
            var dimensions = size.Split(',').Select(int.Parse).ToArray();
            var panel = new VirtualizingStackPanel { Name = "Cow_1", Width = dimensions[0], Height = dimensions[1] };
            var host = new Window { Content = panel, Width = dimensions[0] + 20, Height = dimensions[1] + 40, Left = -10000, Top = -10000, ShowActivated = false };
            host.Show();
            var sample = cow.Clone(); sample.Note = ""; sample.NoteChangeOwner = "SYNTHETIC_HISTORY"; sample.AuctionResultStatus = "11";
            display.FindPanel(panel, sample, 20); Pump(250);
            Check(TextBlocks(panel).Any(t => t.Text.Contains("SYNTHETIC_HISTORY")), "history-only running " + size);
            var historyText = TextBlocks(panel).First(t => t.Text.Contains("SYNTHETIC_HISTORY"));
            Check(historyText.Inlines.OfType<System.Windows.Documents.Run>().All(r => ((SolidColorBrush)r.Foreground).Color == Colors.White), "white history at " + size);
            if (size == "128,128")
            {
                foreach (string state in new[] { "22", "23" })
                {
                    sample = sample.Clone(); sample.AuctionResultStatus = state;
                    display.FindPanel(panel, sample, 10); Pump(150);
                    Check(TextBlocks(panel).Any(t => t.Text.Contains("SYNTHETIC_HISTORY")), "history-only result " + state);
                }
            }
            host.Close();
        }
        NoteColorChecks.Run();
        ScrollChecks.Run();
        // The generic Application has no production startup or shutdown behavior.
        Console.WriteLine($"PASS {_checks} WPF checks; synthetic configuration and fake HTTP only");
    }
    private static ServerGetData CreateService(ServerConn connection, XmlParserCont parser) => new(
        new AuctionSessionRuntime(connection, parser), new NettyAsyncMsgProcess(NLogger.Instance),
        new ServerAuctionDataSource(connection), new AuctionSnapshotParser(new AnimalParseData()), NLogger.Instance);

    private sealed class UiHandler : HttpMessageHandler
    {
        public TaskCompletionSource<bool> Login = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> History = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int TraceCalls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            string body;
            if (request.RequestUri!.AbsolutePath == "/login")
            { await Login.Task.WaitAsync(token); body = "{\"success\":true,\"accessToken\":\"fake\",\"naBzplc\":\"222\"}"; }
            else if (request.RequestUri.AbsolutePath == new Uri(new AnimalTraceSettings().Address).AbsolutePath)
            {
                Interlocked.Increment(ref TraceCalls); await History.Task.WaitAsync(token);
                body = "<response><resultCode>00</resultCode><items><item><infoType>2</infoType><regYmd>20260101</regYmd><regType>전산등록</regType><farmerNm>OWNER_A</farmerNm></item><item><infoType>2</infoType><regYmd>20260201</regYmd><regType>양수</regType><farmerNm>OWNER_B</farmerNm></item></items></response>";
            }
            else body = "{\"success\":true,\"data\":[]}";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }
    }
}
