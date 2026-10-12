using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Infra.Api;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

internal static class EntityNumberChecks
{
    internal static void Run()
    {
        _ = System.Windows.Application.Current ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var text = new EntityNumberText
        {
            Number = "1234 5678 9", Foreground = Brushes.Lime, FontWeight = FontWeights.Normal,
            HighlightForeground = Brushes.Red, HighlightFontWeight = FontWeights.Bold
        };
        var runs = text.Inlines.OfType<Run>().ToArray();
        Program.Check(runs.Length == 3 && runs[0].Text == "1234 " && runs[1].Text == "5678" && runs[2].Text == " 9",
            "one entity text block renders prefix, highlighted middle four, and suffix in order");
        Program.Check(runs[0].Foreground == Brushes.Lime && runs[1].Foreground == Brushes.Red && runs[2].Foreground == Brushes.Lime,
            "only middle digits use highlight color");
        Program.Check(runs[0].FontWeight == FontWeights.Normal && runs[1].FontWeight == FontWeights.Bold && runs[2].FontWeight == FontWeights.Normal,
            "only middle digits use highlight weight");
        foreach (double size in new[] { 11d, 13d, 20d })
        {
            text.FontSize = size;
            text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            text.Arrange(new Rect(text.DesiredSize));
            Program.Check(text.DesiredSize.Width > 0 && text.DesiredSize.Height > 0 && Read(text) == "1234 5678 9",
                "number remains one measurable text flow at size " + size);
            Program.Check(runs.All(run => run.FontSize == size), "font size inherits across all three number parts");
        }

        text.DisplayMode = EntityNumberDisplayMode.Short;
        Program.Check(Read(text) == "5678", "short cattle number derives middle four without another model property");
        text.CowDistinction = "5";
        Program.Check(Read(text) == "789", "short goat number preserves last three digits");
        text.CowDistinction = "6";
        Program.Check(Read(text) == "5678", "other animal short number preserves existing middle-four rule");
        text.DisplayMode = EntityNumberDisplayMode.Full;
        Program.Check(Read(text) == "1234 5678 9", "switching back restores full number");
        text.Number = "000000123456789";
        Program.Check(Read(text) == "1234 5678 9", "15-digit source supports full presentation");
        text.Number = "000100023";
        Program.Check(Read(text) == "0001 0002 3", "nine-digit number preserves leading zeros");
        text.Number = "1234-5678-9";
        Program.Check(Read(text) == "1234 5678 9", "supported separators normalize to the established spaces");
        foreach (string? invalid in new[] { "", null, "-", "123", "1234567890", "12A4 5678 9" })
        {
            text.Number = invalid!;
            Program.Check(Read(text) == (invalid ?? string.Empty) && runs[1].Text == "" && runs[2].Text == "",
                "empty or malformed number clears previous highlight without truncating data");
        }
        text.Number = "1234 5678 9";
        Program.Check(text.Inlines.OfType<Run>().SequenceEqual(runs), "number changes reuse the same three runs");
        text.HighlightForeground = null; text.HighlightFontWeight = null;
        text.Foreground = Brushes.Cyan; text.FontWeight = FontWeights.SemiBold;
        Program.Check(runs.All(run => run.Foreground == Brushes.Cyan && run.FontWeight == FontWeights.SemiBold),
            "clearing highlight overrides restores current base style inheritance");

        var resourceParent = new Grid();
        resourceParent.Resources["TestAccent"] = Brushes.Red;
        text.SetResourceReference(EntityNumberText.HighlightForegroundProperty, "TestAccent");
        resourceParent.Children.Add(text);
        Pump();
        Program.Check(runs[1].Foreground == Brushes.Red, "highlight accepts dynamic resource colors");
        resourceParent.Resources["TestAccent"] = Brushes.Yellow;
        Pump();
        Program.Check(runs[1].Foreground == Brushes.Yellow && runs[0].Foreground == Brushes.Cyan,
            "changing resource color updates only the highlighted digits");
        resourceParent.Children.Clear();

        var original = new gValues { EntityNumber = "1234 5678 9", CowDistinction = "1" };
        using var model = new AuctionContPanelViewModel(original, null!, new VirtualizingStackPanel(), new List<int> { 5 }, 1);
        var region = new Grid { DataContext = model };
        var number = new EntityNumberText();
        BindingOperations.SetBinding(number, FrameworkElement.DataContextProperty, new Binding(nameof(model.CowInfo)));
        BindingOperations.SetBinding(number, EntityNumberText.NumberProperty, new Binding(nameof(gValues.EntityNumber)));
        BindingOperations.SetBinding(number, EntityNumberText.CowDistinctionProperty, new Binding(nameof(gValues.CowDistinction)));
        region.Children.Add(number); Pump();
        Program.Check(Read(number) == "1234 5678 9", "control inherits item context without replacing the view model");
        original.EntityNumber = "2345 6789 0";
        model.UpdateCowInfo(original); Pump();
        Program.Check(Read(number) == "2345 6789 0", "same source mutation through view model refreshes all number parts");
        number.DisplayMode = EntityNumberDisplayMode.Short;
        original.CowDistinction = "5";
        model.UpdateCowInfo(original); Pump();
        Program.Check(Read(number) == "890", "species-only refresh recalculates short presentation");
        original.EntityNumber = "";
        model.UpdateCowInfo(original); Pump();
        Program.Check(Read(number) == "", "clearing item number clears both normal and highlighted text");
        region.Children.Clear();

        var parser = new AnimalParseData();
        var settings = new UserInfo { Auction = new Auction { AuctionHouseCode = "SYNTHETIC", ChangeSexName = "N" } };
        foreach (string species in new[] { "1", "5" })
        {
            string[] fields = ItemUpdateChecks.Sv(17);
            fields[4] = species;
            fields[5] = "000000123456789";
            var update = parser.ParseUpdate(string.Join('|', fields), settings, true);
            Program.Check(update.Values.EntityNumber == "1234 5678 9" && update.Fields.Contains(nameof(gValues.EntityNumber)),
                "SV parser supplies one formatted number for species " + species);
            var parsedText = new EntityNumberText { Number = update.Values.EntityNumber, CowDistinction = update.Values.CowDistinction, DisplayMode = EntityNumberDisplayMode.Short };
            Program.Check(Read(parsedText) == (species == "5" ? "789" : "5678"), "SV to short view keeps species formatting");
        }
    }

    private static string Read(TextBlock text) => new TextRange(text.ContentStart, text.ContentEnd).Text;

    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}
