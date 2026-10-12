using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Infra.Api;
using CowAuctionSmall.Infra.XmlSettings;
using CowAuctionSmall.Presentation.Converters;
using CowAuctionSmall.Presentation.Converters.Mokpo;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views.Common;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

internal static class WarningBoundaryChecks
{
    internal static void Run()
    {
        var parser = new AnimalParseData();
        foreach (var user in new[] { new UserInfo(), new UserInfo { Auction = new Auction() } })
        foreach (bool sv in new[] { false, true })
        {
            var packet = sv ? ItemUpdateChecks.Sv(7) : ItemUpdateChecks.Api(7);
            var value = parser.ParseUpdate(string.Join('|', packet), user, sv).Values;
            Program.Check(value.BidderName == string.Empty, "missing auction options preserve an empty bidder setting for API/SV");
        }

        var model = (MainWindowViewModel)RuntimeHelpers.GetUninitializedObject(typeof(MainWindowViewModel));
        var notifications = new List<string?>();
        model.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);
        Call(model, "InitializeUserInfo", new UserInfo { Auction = new Auction { StartPosition = "12,34" } });
        Call(model, "InitializeBoardSize", new BoardList { Size = "128,64", MultiBoards = new()
            { new Board { Rows = new() { new[] { 1, 2 }, new[] { 3, 4 } } } } });
        Program.Check(model.MainPositionX == 12 && model.MainPositionY == 34 && model.MainWindowWidth == 256 && model.MainWindowHeight == 262,
            "window initialization retains configured geometry");
        Program.Check(new[] { "MainPositionX", "MainPositionY", "MainWindowWidth", "MainWindowHeight" }.All(notifications.Contains),
            "window initialization raises the four binding notifications");
        foreach (var board in new[] { new BoardList(), new BoardList { MultiBoards = new() },
            new BoardList { MultiBoards = new() { new Board() } },
            new BoardList { MultiBoards = new() { new Board { Rows = new() } } } })
            Call(model, "InitializeBoardSize", board);
        Program.Check(model.MainWindowWidth == 256 && model.MainWindowHeight == 262, "missing board rows leave current dimensions intact");

        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "<Board><Size>128,128</Size><LogoBoard Name='fixture'><LogoRowIdx ID='1'>1,2</LogoRowIdx></LogoBoard></Board>");
            var board = new BoardXmlParser().ParseXml(path);
            Program.Check(board.LogoBoard?.Single().Rows?.Single().Rows?.SequenceEqual(new[] { 1, 2 }) == true,
                "board parser initializes and retains logo rows");
        }
        finally { File.Delete(path); }

        var culture = CultureInfo.InvariantCulture;
        Program.Check((Visibility)new MokpoEPD().Convert(new object[] { "Y", "2" }, typeof(Visibility), new NullText(), culture) == Visibility.Collapsed,
            "null converter parameter text is treated as absent");
        Program.Check((Visibility)new NH_EPD_ShowToVisibilityConverter().Convert(DependencyProperty.UnsetValue, typeof(Visibility), null!, culture) == Visibility.Collapsed,
            "unresolved EPD binding remains hidden");
        var text = new OutlinedTextBlock { Text = "비거세", FontSize = 12 };
        text.Measure(new Size(128, 64));
        var field = typeof(OutlinedTextBlock).GetField("formattedText", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var formatted = (FormattedText)field.GetValue(text)!;
        Program.Check(formatted.PixelsPerDip == VisualTreeHelper.GetDpi(text).PixelsPerDip && text.DesiredSize.Width > 0,
            "outlined text uses the visual's DPI and remains measurable");
        Call(text, "OnDpiChanged", new DpiScale(1, 1), new DpiScale(1.5, 1.5));
        Program.Check(field.GetValue(text) == null && !text.IsMeasureValid, "DPI change invalidates cached text measurements");
    }

    private static void Call(object target, string method, params object[] args) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
    private sealed class NullText { public override string? ToString() => null; }
}
