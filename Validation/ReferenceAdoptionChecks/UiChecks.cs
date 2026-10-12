using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views.Common;
using CowAuctionSmall.Views.Size128_128.Running;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

internal static class UiChecks
{
    internal static void Run()
    {
        _ = System.Windows.Application.Current ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        SexDisplayChecks.EnsureResources();
        WarningBoundaryChecks.Run();
        var cow = new gValues { Note = "SYNTHETIC_NOTE", NoteChangeOwner = "SYNTHETIC_HISTORY", Sex = "수", CowDistinction = "1", BidPrice = "100", BidderName = "Y", Bidder = "SYNTHETIC_BIDDER" };
        foreach (var entry in new (Func<FrameworkElement> Create, int Width, int Height)[]
        {
            (() => new CowAuctionSmall.Views.Size128_64.Running.AuctionRunning1_64(), 128, 64),
            (() => new CowAuctionSmall.Views.SIze_160_64.Running.AuctionRunning1_160_64(), 160, 64),
            (() => new CowAuctionSmall.Views.Size_320_64.Running.AuctionRunning1_320_64(), 320, 64),
            (() => new RunningNoteHost128(), 128, 128),
            (() => new CowAuctionSmall.Views.Size128_128.Standard_non_X_Sold(), 128, 128),
            (() => new CowAuctionSmall.Views.Size128_128.CustomAuctionSold.JangseongSold(), 128, 128)
        })
        {
            var view = entry.Create();
            using var model = new AuctionContPanelViewModel(cow, null!, new VirtualizingStackPanel(), new List<int> { 5 }, 1);
            view.DataContext = view.GetType().Name.StartsWith("AuctionRunning", StringComparison.Ordinal) ? model
                : view.GetType().Name.Contains("Sold", StringComparison.Ordinal) ? new AuctionItemDisplayViewModel(cow) : cow;
            if (view is RunningNoteHost128 noteHost) noteHost.ItemDisplay = model.ItemDisplay;
            for (int pass = 0; pass < 2; pass++)
            {
                using var host = Host(view, entry.Width, entry.Height);
                Program.Check(view.IsLoaded, "direct view loads: " + view.GetType().Name);
                string expected = view.GetType().Name.Contains("Sold") ? "SYNTHETIC_BIDDER" : "SYNTHETIC_HISTORY";
                Program.Check(TextBlocks(view).Any(text => text.Text.Contains(expected)),
                    "note or bidder binding on direct view: " + view.GetType().Name);
                host.RootVisual = null;
                Pump();
                Program.Check(!view.IsLoaded, "direct view unloads: " + view.GetType().Name);
            }
        }

        var bidder = new BidderNameFlow { Text = "SYNTHETIC_LONG_BIDDER", Width = 100, Height = 22 };
        for (int pass = 0; pass < 3; pass++)
        {
            using var host = Host(bidder, 100, 22);
            Program.Check(TextBlocks(bidder).Any(text => text.Text == bidder.Text), "bidder text binding after repeated load");
            Program.Check(Animation(bidder) != null, "bidder animation created on load");
            host.RootVisual = null;
            Pump();
            Program.Check(Animation(bidder) == null, "bidder animation released on unload");
        }

        var note = new TextBlock();
        NoteText.SetNormal(note, "-"); NoteText.SetHistory(note, "HISTORY_ONLY"); Pump();
        Program.Check(note.Text == "HISTORY_ONLY", "blank original note supports history-only display");
        NoteText.SetHistory(note, ""); Pump();
        Program.Check(note.Text == "", "clearing both notes removes stale text");
    }

    private static object? Animation(BidderNameFlow view) => typeof(BidderNameFlow)
        .GetField("_flowTextAnimation", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view);

    private static HwndSource Host(FrameworkElement view, int width, int height)
    {
        var source = new HwndSource(new HwndSourceParameters("Offline reference checks")
        {
            Width = width, Height = height, WindowStyle = unchecked((int)0x80000000)
        });
        source.RootVisual = view;
        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
        view.UpdateLayout();
        Pump();
        return source;
    }

    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static IEnumerable<TextBlock> TextBlocks(DependencyObject root)
    {
        if (root is TextBlock text) yield return text;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in TextBlocks(VisualTreeHelper.GetChild(root, index))) yield return child;
    }
}
