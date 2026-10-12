using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using CowAuctionSmall.Presentation.Display;
using GAMSTest.Models;

namespace GAMSTest.Services;

/// <summary>Explicit, read-only import of display settings. Authentication and endpoints are never retained.</summary>
public static class TestSettingsLoader
{
    public static TestSettings LoadStartup(string baseDirectory, string workingDirectory)
    {
        string? directory = FindConfigurationDirectory(baseDirectory, workingDirectory);
        if (directory == null) return LoadEmbeddedDefaults();
        string board = Path.Combine(directory, "Board.XML");
        string users = Path.Combine(directory, "users.XML");
        return Load(File.Exists(board) ? board : null, File.Exists(users) ? users : null);
    }

    public static string? FindConfigurationDirectory(string baseDirectory, string workingDirectory)
    {
        // Deployment-local configuration takes precedence. Never search another installation.
        var candidates = new List<string> { Path.Combine(baseDirectory, "Config"), Path.Combine(workingDirectory, "Config") };
        for (var parent = new DirectoryInfo(baseDirectory); parent != null; parent = parent.Parent)
        {
            if (!File.Exists(Path.Combine(parent.FullName, "CowAuctionSmall.csproj"))) continue;
            candidates.Add(Path.Combine(parent.FullName, "Config"));
            break;
        }
        candidates.Add(Path.Combine(baseDirectory, "Examples"));
        return candidates.FirstOrDefault(path => File.Exists(Path.Combine(path, "Board.XML")) || File.Exists(Path.Combine(path, "users.XML")));
    }

    public static TestSettings Load(string? boardPath = null, string? usersPath = null)
    {
        var settings = new TestSettings();
        if (!string.IsNullOrWhiteSpace(boardPath))
        {
            ReadBoard(ReadXml(boardPath), settings);
            settings.BoardXmlPath = Path.GetFullPath(boardPath);
        }
        if (!string.IsNullOrWhiteSpace(usersPath))
            ReadUsers(ReadXml(usersPath), settings);
        return settings;
    }

    public static TestSettings LoadBoard(string path, TestSettings current)
    {
        var settings = current.Snapshot();
        ReadBoard(ReadXml(path), settings);
        settings.BoardXmlPath = Path.GetFullPath(path);
        return settings;
    }

    private static TestSettings LoadEmbeddedDefaults()
    {
        var settings = new TestSettings();
        var assembly = typeof(TestSettingsLoader).Assembly;
        using var board = assembly.GetManifestResourceStream("GAMSTest.Examples.Board.XML")
            ?? throw new FormatException("내장 보드 설정을 찾을 수 없습니다.");
        using var users = assembly.GetManifestResourceStream("GAMSTest.Examples.users.XML")
            ?? throw new FormatException("내장 표시 설정을 찾을 수 없습니다.");
        ReadBoard(ReadXml(board), settings);
        ReadUsers(ReadXml(users), settings);
        return settings;
    }

    private static XElement ReadXml(string path)
    {
        using var stream = File.OpenRead(path);
        return ReadXml(stream);
    }

    private static XElement ReadXml(Stream stream)
    {
        var options = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 4 * 1024 * 1024
        };
        using var reader = XmlReader.Create(stream, options);
        return XDocument.Load(reader).Root ?? throw new FormatException("XML의 최상위 항목이 없습니다.");
    }

    private static void ReadBoard(XElement root, TestSettings settings)
    {
        string size = string.Join(",", (root.Element("Size")?.Value ?? "").Split(',').Select(x => x.Trim()));
        settings.Resolution = DisplaySizeParser.Parse(size);
        if (settings.Resolution == DisplaySizeParser.DisplaySize.Unknown)
            throw new FormatException("지원 해상도는 128,64 / 128,128 / 160,64 / 320,64입니다.");

        var boards = new List<TestBoardLayout>();
        foreach (var element in root.Elements("MultiBoard"))
        {
            var rows = new List<List<int>>();
            foreach (var row in element.Elements("RowIdx"))
            {
                var numbers = new List<int>();
                foreach (string token in row.Value.Split(','))
                {
                    if (!int.TryParse(token.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                        throw new FormatException("RowIdx에는 쉼표로 구분한 정수 계류대 번호를 입력해야 합니다.");
                    numbers.Add(number);
                }
                rows.Add(numbers);
            }
            if (rows.Count == 0)
                throw new FormatException("MultiBoard에 표시할 RowIdx가 없습니다.");
            boards.Add(new TestBoardLayout
            {
                Name = element.Attribute("Name")?.Value ?? $"배치 {boards.Count + 1}",
                Rows = rows
            });
        }
        if (boards.Count == 0)
            throw new FormatException("Board.XML에 MultiBoard 배치가 없습니다.");
        settings.Boards = boards;
        settings.Rows = boards[0].Rows.Select(row => new List<int>(row)).ToList();
    }

    private static void ReadUsers(XElement root, TestSettings settings)
    {
        var auction = root.Element("Auction") ?? throw new FormatException("users.XML에 Auction 표시 설정이 없습니다.");
        settings.HouseCode = Value(auction, "AuctionHouseCode", settings.HouseCode);
        string? position = auction.Element("StartPosition")?.Value;
        if (!string.IsNullOrWhiteSpace(position))
        {
            string[] coordinates = position.Split(',');
            if (coordinates.Length != 2 ||
                !double.TryParse(coordinates[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double left) ||
                !double.TryParse(coordinates[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double top) ||
                !double.IsFinite(left) || !double.IsFinite(top))
                throw new FormatException("StartPosition에는 표시창의 X,Y 좌표를 입력해야 합니다.");
            settings.DisplayLeft = left;
            settings.DisplayTop = top;
        }
        settings.Qquri = Choice(auction, "IsShowQQuri", settings.Qquri, "Y", "N", "X");
        settings.IsShowQquri = settings.Qquri;
        settings.LowestPriceTitle = Value(auction, "LowestPriceTitle", settings.LowestPriceTitle);
        settings.BidderName = Choice(auction, "BidderName", settings.BidderName, "Y", "N", "B", "X");
        settings.IsShowOwnerName = Choice(auction, "IsShowOwnerName", settings.IsShowOwnerName, "Y", "N", "NN");
        settings.MaskOwner = settings.IsShowOwnerName == "N";
        settings.IsPaternityMatch = Choice(auction, "IsPaternityMatch", settings.IsPaternityMatch, "Y", "N");
        settings.ChangeSexName = Choice(auction, "ChangeSexName", settings.ChangeSexName, "Y", "N");
        settings.SelectShowWeightEpd = Choice(auction, "SelectShowWeight_EPD", settings.SelectShowWeightEpd, "Y", "N");
        settings.CowBirth = Choice(auction, "CowBirth", settings.CowBirth, "Y", "N");

        var pages = auction.Element("PageSetting") ?? root.Element("PageSetting");
        string? pageCount = pages?.Element("BoardPage")?.Value ?? auction.Element("BoardPage")?.Value;
        if (!string.IsNullOrWhiteSpace(pageCount))
        {
            if (!int.TryParse(pageCount, out int count) || count < 1 || count > 4)
                throw new FormatException("BoardPage는 1~4 사이의 페이지 수여야 합니다.");
            settings.PageCount = count;
        }

        foreach (string key in new[] { "EntityNumberForeground", "EntityNumberShortForeground", "LocationForeground", "EpdGradeForeground" })
        {
            string? color = auction.Element(key)?.Value.Trim();
            if (string.IsNullOrEmpty(color)) continue;
            try { _ = ColorConverter.ConvertFromString(color); }
            catch (Exception ex) when (ex is FormatException or ArgumentException or NotSupportedException)
            { throw new FormatException($"{key} 색상 설정을 확인해 주세요."); }
            settings.Colors[key] = color;
        }
    }

    private static string Value(XElement parent, string key, string fallback) =>
        string.IsNullOrWhiteSpace(parent.Element(key)?.Value) ? fallback : parent.Element(key)!.Value.Trim();

    private static string Choice(XElement parent, string key, string fallback, params string[] allowed)
    {
        string value = Value(parent, key, fallback).ToUpperInvariant();
        if (!allowed.Contains(value)) throw new FormatException($"{key} 표시 설정을 확인해 주세요.");
        return value;
    }
}
