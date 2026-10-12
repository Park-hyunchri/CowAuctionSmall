using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views.Common;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

internal static class SexDisplayChecks
{
    private static readonly Dictionary<string, string> Profiles = new()
    {
        ["128"] = "SexText128x64", ["160"] = "SexText160x64", ["V3"] = "SexTextV3", ["Fallback"] = "SexTextV3Fallback"
    };
    private static readonly List<Baseline> Baselines = JsonSerializer.Deserialize<List<Baseline>>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SexStyleBaseline.json")))!;

    internal static void EnsureResources()
    {
        var app = System.Windows.Application.Current!;
        if (!app.Resources.Contains("SexTextV3")) app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/CowAuctionSmall;component/Views/Common/SexDisplayStyles.xaml", UriKind.Relative)
        });
    }

    internal static void Run()
    {
        EnsureResources();
        foreach (var raw in new[] { "암", "수", "거세", "비거세", "미경산", "프리마틴", "프", "프리", "거", "비거", "새", "", "-", " 암 " })
        {
            var source = new gValues { Sex = raw };
            var vm = new AuctionItemDisplayViewModel(source);
            Program.Check(vm.SexText == (raw == "프리마틴" ? "프리" : raw) && vm.SexStyleKey == raw && source.Sex == raw,
                "sex display preserves source and only abbreviates freemartin: " + raw);
        }
        Program.Check(new AuctionItemDisplayViewModel(new gValues { Sex = null! }).SexText == "", "null sex is safe");
        foreach (var row in Baselines)
        {
            var block = new TextBlock { FontFamily = new FontFamily("굴림"),
                DataContext = new AuctionItemDisplayViewModel(new gValues { Sex = row.Sex }),
                Style = (Style)System.Windows.Application.Current.FindResource(Profiles[row.Profile]) };
            using var host = Host(block);
            Program.Check(block.FontSize == row.FontSize && block.FontWeight.ToOpenTypeWeight() == row.Weight &&
                block.Foreground.ToString() == row.Foreground && block.Margin == (Thickness)new ThicknessConverter().ConvertFromInvariantString(row.Margin)!,
                "pre-migration style contract: " + row.Profile + "/" + row.Sex);
            double expectedRenderX = row.Profile == "160" && row.Sex == "프리마틴" ? .7 : row.ScaleX;
            Program.Check(Near(block.RenderTransform.Value.M11, expectedRenderX) && Near(block.RenderTransform.Value.M22, row.ScaleY),
                "profile compression and height: " + row.Profile + "/" + row.Sex);
            AssertText(block, row.Sex, row.Profile);
        }
        var outline = new SexOutlineDisplay();
        using var outlineHost = Host(outline);
        var frame = (Border)outline.FindName("Frame");
        var glyph = (OutlinedTextBlock)outline.FindName("OutlinedText");
        foreach (var category in new[] { "혈통", "일치", "기타" })
        foreach (var sex in new[] { "암", "수", "거세", "미경산", "비거세", "프리마틴", "프", "", "정보없음", "부 불일치" })
        {
            outline.DataContext = new AuctionItemDisplayViewModel(new gValues { Sex = sex, RegistrationCategory = category });
            Layout(outline);
            AssertOutline(outline, sex, category);
            Program.Check(ReferenceEquals(frame, outline.FindName("Frame")) && ReferenceEquals(glyph, outline.FindName("OutlinedText")),
                "outline refresh retains its visual elements");
        }
        Console.WriteLine("Sex styles: 76 pre-migration reference cases; ordinary and outlined text/scale/source checks.");
    }

    internal static void CheckView(FrameworkElement view, gValues source, Action<gValues> publish)
    {
        var texts = Nodes(view).OfType<TextBlock>().Where(text => BindingOperations.GetBinding(text, TextBlock.TextProperty)?.Path.Path == "SexText").ToArray();
        var outlines = Nodes(view).OfType<SexOutlineDisplay>().ToArray();
        if (texts.Length == 0 && outlines.Length == 0) return;
        foreach (var (sex, category) in new[] { ("미경산", "혈통"), ("프리마틴", "혈통"), ("비거세", "일치"), ("수", "기타") })
        {
            var changed = source.Clone(); changed.Sex = sex; changed.RegistrationCategory = category;
            publish(changed); Layout(view);
            foreach (var text in texts)
            {
                string profile = Profiles.First(pair => HasStyle(text.Style, (Style)System.Windows.Application.Current.FindResource(pair.Value))).Key;
                AssertText(text, sex, view.GetType().Name);
                var expected = Baselines.Single(row => row.Profile == profile && row.Sex == sex);
                Program.Check(text.Foreground.ToString() == expected.Foreground && text.FontSize == expected.FontSize,
                    "actual view preserves original sex color/size: " + view.GetType().Name);
                if (view.GetType().Name == "HoengseongRun")
                {
                    Program.Check(Near(text.DesiredSize.Width, 46), "Hoengseong sex retains its fixed layout width");
                    if (sex.Length == 3)
                        Program.Check(Near(text.ActualWidth * text.LayoutTransform.Value.M11, 46), "Hoengseong three-character rendering fits 46px");
                }
            }
            foreach (var outline in outlines) AssertOutline(outline, sex, category);
        }
    }

    private static void AssertText(TextBlock block, string sex, string label)
    {
        string expected = sex == "프리마틴" ? "프리" : sex;
        Program.Check(block.Text == expected, "sex text: " + label);
        double expectedScale = expected.Length == 3 ? label == "HoengseongRun" ? 46d / 72 : 2d / 3 : 1;
        Program.Check(Near(block.LayoutTransform.Value.M11, expectedScale) && Near(block.LayoutTransform.Value.M22, 1),
            "horizontal compression applies only to three characters and resets: " + label);
        if (expected.Length == 3)
        {
            var formatted = Format(expected, block.FontFamily, block.FontWeight, block.FontSize);
            var twoCharacters = Format(expected[..2], block.FontFamily, block.FontWeight, block.FontSize);
            Program.Check(formatted.Width * block.LayoutTransform.Value.M11 <= twoCharacters.Width + .01,
                "three characters fit the actual two-character font width: " + label);
        }
        Program.Check(BindingOperations.GetBinding(block, TextBlock.TextProperty)?.Mode == BindingMode.OneWay, "sex display is one-way");
    }

    private static void AssertOutline(SexOutlineDisplay view, string sex, string category)
    {
        var frame = (Border)view.FindName("Frame");
        var glyph = (OutlinedTextBlock)view.FindName("OutlinedText");
        var plain = (TextBlock)view.FindName("PlainText");
        bool framed = category is "혈통" or "일치";
        bool hidden = !framed && sex.Length > 2 && (sex.Contains("정보없음") || sex.Contains("일치"));
        string expected = hidden ? "" : sex == "프리마틴" ? "프리" : sex;
        Program.Check(glyph.Text == expected && plain.Text == expected, "outline abbreviation/hiding policy");
        Program.Check(frame.Visibility == (framed ? Visibility.Visible : Visibility.Collapsed) &&
            glyph.Visibility == (framed ? Visibility.Visible : Visibility.Collapsed) && plain.Visibility == (framed ? Visibility.Collapsed : Visibility.Visible),
            "outline presentation mode changes");
        if (framed)
        {
            double size = category == "일치" || sex == "일치" ? 14 : 24;
            Program.Check(frame.Width == size * (expected.Length == 1 ? 1 : 2) && frame.Height == size &&
                frame.BorderThickness.Left == (category == "일치" ? 1 : 2), "outline frame dimensions and thickness preserved");
            Program.Check(glyph.FontSize == size && glyph.StrokeThickness == (category == "일치" ? .2 : 1), "outline glyph height and stroke preserved");
            if (expected.Length == 3)
            {
                var expectedGeometry = Format(expected, glyph.FontFamily, glyph.FontWeight, glyph.FontSize).BuildGeometry(new Point());
                var bitmap = new RenderTargetBitmap(160, 80, 96, 96, PixelFormats.Pbgra32); bitmap.Render(view);
                var geometry = (Geometry?)typeof(OutlinedTextBlock).GetField("textGeometry", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(glyph);
                Program.Check(geometry != null && Near(geometry.Bounds.Width, expectedGeometry.Bounds.Width), $"outlined text geometry contains all three glyphs without layout clipping: {sex}/{category} actual={geometry?.Bounds} expected={expectedGeometry.Bounds} size={glyph.RenderSize} font={glyph.FontFamily}");
                Rect bounds = glyph.TransformToAncestor(frame.Parent as Visual ?? view).TransformBounds(expectedGeometry.Bounds);
                Rect frameBounds = frame.TransformToAncestor(frame.Parent as Visual ?? view).TransformBounds(new Rect(frame.RenderSize));
                Program.Check(bounds.Left >= frameBounds.Left && bounds.Right <= frameBounds.Right, "compressed outline glyphs fit inside the unchanged frame");
                Program.Check(Near(glyph.LayoutTransform.Value.M22, 1), "outline compression preserves vertical height");
            }
            else Program.Check(Near(glyph.LayoutTransform.Value.M11, 1), "outline compression resets for short labels");
        }
        if (sex == "프리마틴") Program.Check(view.Foreground.ToString() == (category == "일치" ? "#FFFF69B4" : "#FF5CAFFF"), "abbreviated freemartin retains original classification color");
    }

    private static bool HasStyle(Style? style, Style expected) => style != null && (ReferenceEquals(style, expected) || HasStyle(style.BasedOn, expected));
    private static bool Near(double a, double b) => Math.Abs(a - b) < .01;
    private static FormattedText Format(string text, FontFamily font, FontWeight weight, double size) =>
        new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(font, FontStyles.Normal, weight, FontStretches.Normal), size, Brushes.White, 1);
    private static HwndSource Host(FrameworkElement view)
    {
        var host = new HwndSource(new HwndSourceParameters("Offline sex display checks") { Width = 320, Height = 128, WindowStyle = unchecked((int)0x80000000) });
        host.RootVisual = view; Layout(view); return host;
    }
    private static void Layout(FrameworkElement view)
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
        view.Measure(new Size(320, 128)); view.Arrange(new Rect(0, 0, 320, 128)); view.UpdateLayout();
    }
    private static IEnumerable<DependencyObject> Nodes(DependencyObject node)
    {
        yield return node;
        if (node is not Visual) yield break;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            foreach (var child in Nodes(VisualTreeHelper.GetChild(node, i))) yield return child;
    }
    private sealed class Baseline
    {
        public string Profile { get; set; } = "";
        public string Sex { get; set; } = "";
        public double FontSize { get; set; }
        public string Foreground { get; set; } = "";
        public string Margin { get; set; } = "";
        public int Weight { get; set; }
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
    }
}
