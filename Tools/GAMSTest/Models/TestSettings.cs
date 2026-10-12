using CowAuctionSmall.Presentation.Display;
using static CowAuctionSmall.Presentation.Display.DisplaySizeParser;

namespace GAMSTest.Models;

public sealed class TestBoardLayout
{
    public string Name { get; set; } = "가상 배치";
    public List<List<int>> Rows { get; set; } = [[1, 2, 3, 4], [5, 6, 7, 8]];
}

public sealed class TestSettings
{
    public string? BoardXmlPath { get; set; }
    public DisplaySize Resolution { get; set; } = DisplaySize.Size128x128;
    public List<List<int>> Rows { get; set; } = [[1, 2, 3, 4], [5, 6, 7, 8]];
    public List<TestBoardLayout> Boards { get; set; } = [new()];
    public string HouseCode { get; set; } = "0000000000000";
    public string Qquri { get; set; } = "Y";
    public string IsShowQquri { get; set; } = "Y";
    public string CowDistinction { get; set; } = "1";
    public string NhExcellent { get; set; } = "N";
    public string MotherNhExcellent { get; set; } = "N";
    public string LowestPriceTitle { get; set; } = "최저가";
    public string BidderName { get; set; } = "B";
    public bool MaskOwner { get; set; }
    public string SelectShowWeightEpd { get; set; } = "Y";
    public string AbilityNumber { get; set; } = "12.34";
    public string IsPaternityMatch { get; set; } = "Y";
    public string ChangeSexName { get; set; } = "N";
    public string CowBirth { get; set; } = "Y";
    public string IsShowOwnerName { get; set; } = "Y";
    public int PageCount { get; set; } = 4;
    public double DisplayLeft { get; set; }
    public double DisplayTop { get; set; }
    public Dictionary<string, string> Colors { get; set; } = new();

    public AuctionViewOptions ToViewOptions() => new()
    {
        Size = Resolution, NhCode = HouseCode, IsQquri = Qquri,
        IsShowQquri = IsShowQquri, CowDistinction = CowDistinction,
        IsNhExcellent = NhExcellent, IsMotherNhExcellent = MotherNhExcellent,
        LowestPriceTitle = LowestPriceTitle, AbilityNumber = AbilityNumber
    };

    public TestSettings Snapshot()
    {
        var copy = (TestSettings)MemberwiseClone();
        copy.Rows = Rows.Select(row => new List<int>(row)).ToList();
        copy.Boards = Boards.Select(board => new TestBoardLayout { Name = board.Name, Rows = board.Rows.Select(row => new List<int>(row)).ToList() }).ToList();
        copy.Colors = new Dictionary<string, string>(Colors);
        return copy;
    }

    public static (int Width, int Height) GetDimensions(DisplaySize size) => size switch
    {
        DisplaySize.Size128x64 => (128, 64),
        DisplaySize.Size128x128 => (128, 128),
        DisplaySize.Size160x64 => (160, 64),
        DisplaySize.Size320x64 => (320, 64),
        _ => throw new ArgumentException("지원하지 않는 전광판 해상도입니다.")
    };
}

public enum SampleKind { Normal, LongNames, Empty, ThreeCharacterSex }
public sealed record DisplayMode(string Key, string Label, int Page = 0, bool IsSupported = true)
{
    public override string ToString() => Label;
}

