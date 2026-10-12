using CowAuctionSmall.Views.Common;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

static class NoteColorChecks
{
    static void Flush() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
    static void Check(bool value, string label) { if (!value) throw new Exception("Note color: " + label); }
    public static void Run()
    {
        var text = new TextBlock();
        NoteText.SetNormal(text, "ORIGINAL"); NoteText.SetHistory(text, "HISTORY"); Flush();
        var runs = text.Inlines.OfType<Run>().ToArray();
        Check(text.Text == "ORIGINAL / HISTORY" && runs.Length == 3, "combined text preserved");
        Check(((SolidColorBrush)runs[0].Foreground).Color == Colors.Red && ((SolidColorBrush)runs[1].Foreground).Color == Colors.Red, "original and separator red");
        Check(((SolidColorBrush)runs[2].Foreground).Color == Colors.White, "history white");
        NoteText.SetHistory(text, ""); Flush();
        Check(text.Text == "ORIGINAL" && text.Inlines.Count == 1 && ((SolidColorBrush)text.Inlines.FirstInline.Foreground).Color == Colors.Red, "history disabled leaves red original");
        NoteText.SetNormal(text, "-"); NoteText.SetHistory(text, "HISTORY"); Flush();
        Check(text.Text == "HISTORY" && text.Inlines.Count == 1 && ((SolidColorBrush)text.Inlines.FirstInline.Foreground).Color == Colors.White, "placeholder normal leaves white history");
        NoteText.SetNormal(text, " "); NoteText.SetHistory(text, " "); Flush();
        Check(text.Text == "" && text.Inlines.Count == 0, "empty content has no separator");
        Console.WriteLine("PASS 6 note color checks; synthetic text only");
    }
}
