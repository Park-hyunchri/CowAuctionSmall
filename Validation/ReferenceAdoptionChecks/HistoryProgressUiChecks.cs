using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.ViewModels;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

internal static class HistoryProgressUiChecks
{
    internal static void Run()
    {
        // Load just the actual status panel; the product Window constructor starts application services/timers.
        var document = XDocument.Load(Path.Combine(Directory.GetCurrentDirectory(), "Views", "MainWindow.xaml"));
        XNamespace wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var source = new XElement(document.Descendants(wpf + "Grid").Single(e => (string?)e.Attribute(x + "Name") == "MainStatusFooter"));
        source.Attribute("SizeChanged")?.Remove();
        foreach (var attribute in source.Descendants().Attributes("TextChanged").ToArray()) attribute.Remove();
        source.SetAttributeValue(XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml");
        var panel = (Grid)XamlReader.Parse(source.ToString());
        var model = (MainWindowViewModel)RuntimeHelpers.GetUninitializedObject(typeof(MainWindowViewModel));
        typeof(MainWindowViewModel).GetField("<StatusBar>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(model, new MainStatusViewModel());
        model.AuctionSignalText = "SYNTHETIC_SIGNAL"; model.MainWindowTextBox = "SYNTHETIC_LOG";
        typeof(MainWindowViewModel).GetField("_historyReady", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, true);
        var apply = typeof(MainWindowViewModel).GetMethod("OnHistoryProgress", BindingFlags.Instance | BindingFlags.NonPublic)!;
        panel.DataContext = model;
        using var host = new HwndSource(new HwndSourceParameters("Offline history status")
            { Width = 1000, Height = 100, WindowStyle = unchecked((int)0x80000000) });
        host.RootVisual = panel;
        var text = panel.Children.OfType<TextBlock>().Single(t => BindingOperations.GetBinding(t, TextBlock.TextProperty)?.Path.Path == "HistoryProgressText");
        foreach (var stage in new[] { HistoryBatchStage.Preparing, HistoryBatchStage.Loading, HistoryBatchStage.Applying, HistoryBatchStage.Complete, HistoryBatchStage.Empty, HistoryBatchStage.Failed })
        {
            var progress = new HistoryBatchProgress(1, "day", stage, 143, 20, 5, 1, 2, 6);
            apply.Invoke(model, new object[] { progress }); Pump();
            panel.Measure(new Size(1000, 100)); panel.Arrange(new Rect(0, 0, 1000, 100)); panel.UpdateLayout();
            Program.Check(text.Text == progress.Text && text.ActualHeight > 0 && text.Visibility == Visibility.Visible,
                "actual status panel displays " + stage);
            Program.Check(model.AuctionSignalText == "SYNTHETIC_SIGNAL" && model.MainWindowTextBox == "SYNTHETIC_LOG",
                "history progress does not overwrite auction signal or append log entries");
        }
        Program.Check(BindingOperations.GetBinding(text, TextBlock.TextProperty)!.Mode == BindingMode.OneWay,
            "history status has one-way view binding");
        string before = model.HistoryProgressText;
        typeof(MainWindowViewModel).GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, true);
        apply.Invoke(model, new object[] { new HistoryBatchProgress(2, "next", HistoryBatchStage.Empty) }); Pump();
        Program.Check(model.HistoryProgressText == before, "disposed view model ignores progress callbacks");
        host.RootVisual = null;
    }
    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}
