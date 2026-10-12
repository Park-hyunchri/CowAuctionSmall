using GAMSTest.Services;
using System.IO;
using System.Text.Json;
using System.Xml;
using Size = CowAuctionSmall.Presentation.Display.DisplaySizeParser.DisplaySize;

internal static class ConfigurationChecks
{
    internal static void Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), "GAMSTestChecks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string board = Path.Combine(directory, "Board.XML");
        string users = Path.Combine(directory, "users.XML");
        var defaults = TestSettingsLoader.Load();
        Program.Check(defaults.Rows.Sum(row => row.Count) > 0, "missing optional paths use synthetic defaults");
        Program.Throws<FileNotFoundException>(() => TestSettingsLoader.Load(Path.Combine(directory, "absent.XML")), "explicit missing file is reported");

        foreach ((string text, Size size) in new[]
        {
            ("128,64", Size.Size128x64), ("128,128", Size.Size128x128),
            ("160,64", Size.Size160x64), ("320,64", Size.Size320x64)
        })
        {
            string xml = $"<Boards><Size>{text}</Size><MultiBoard Name='A'><RowIdx>7,7,8</RowIdx><RowIdx>9</RowIdx></MultiBoard><MultiBoard Name='B'><RowIdx>20,21</RowIdx></MultiBoard></Boards>";
            File.WriteAllText(board, xml);
            var settings = TestSettingsLoader.Load(board);
            Program.Check(settings.Resolution == size && settings.Boards.Count == 2 && settings.Rows.SelectMany(row => row).SequenceEqual(new[] { 7, 7, 8, 9 }), "board import preserves all physical slots and duplicate labels");
            Program.Check(settings.Boards[1].Rows[0].SequenceEqual(new[] { 20, 21 }), "alternate board retained");
            settings.Rows[0][0] = 999;
            Program.Check(settings.Boards[0].Rows[0][0] == 7 && File.ReadAllText(board) == xml, "in-memory layout edit changes neither source layout nor file");
        }

        foreach (string xml in new[]
        {
            "<Boards><Size>129,64</Size><MultiBoard><RowIdx>1</RowIdx></MultiBoard></Boards>",
            "<Boards><Size>128,128</Size></Boards>",
            "<Boards><Size>128,128</Size><MultiBoard /></Boards>",
            "<Boards><Size>128,128</Size><MultiBoard><RowIdx>1,x</RowIdx></MultiBoard></Boards>",
            "<Boards><Size>128,128</Size><MultiBoard><RowIdx></RowIdx></MultiBoard></Boards>"
        })
        {
            File.WriteAllText(board, xml);
            Program.Throws<FormatException>(() => TestSettingsLoader.Load(board), "invalid board must not silently shift slots or fall back to another size");
        }
        File.WriteAllText(board, "<Boards>");
        Program.Throws<XmlException>(() => TestSettingsLoader.Load(board), "malformed XML reported");
        File.WriteAllText(board, "<!DOCTYPE Boards [<!ENTITY test '1'>]><Boards><Size>128,128</Size><MultiBoard><RowIdx>&test;</RowIdx></MultiBoard></Boards>");
        Program.Throws<XmlException>(() => TestSettingsLoader.Load(board), "DTD processing prohibited");

        const string userXml = "<Users><Authentication><Password>SYNTHETIC_SECRET_SENTINEL</Password></Authentication><CurrentInfo><Address>SYNTHETIC_ENDPOINT_SENTINEL</Address></CurrentInfo><Auction><AuctionHouseCode>0002</AuctionHouseCode><IsShowQQuri>X</IsShowQQuri><BidderName>B</BidderName><IsShowOwnerName>N</IsShowOwnerName><LowestPriceTitle>가상가격</LowestPriceTitle><BoardPage>1</BoardPage><PageSetting><BoardPage>4</BoardPage></PageSetting><EntityNumberForeground>#FF123456</EntityNumberForeground></Auction></Users>";
        File.WriteAllText(users, userXml);
        File.SetAttributes(users, FileAttributes.ReadOnly);
        try
        {
            var settings = TestSettingsLoader.Load(usersPath: users);
            Program.Check(settings.HouseCode == "0002" && settings.BidderName == "B" && settings.MaskOwner && settings.PageCount == 4, "display-only config and nested page precedence imported");
            Program.Check(settings.Qquri == "X" && settings.ToViewOptions().IsShowQquri == "X", "QQuri setting is consistent for running and results");
            Program.Check(settings.Colors["EntityNumberForeground"] == "#FF123456", "display colors imported");
            string snapshot = JsonSerializer.Serialize(settings);
            Program.Check(!snapshot.Contains("SYNTHETIC_SECRET_SENTINEL") && !snapshot.Contains("SYNTHETIC_ENDPOINT_SENTINEL"), "credentials and endpoints excluded from retained settings");
            settings.HouseCode = "CHANGED_IN_MEMORY";
            Program.Check(File.ReadAllText(users) == userXml, "read-only source remains byte-equivalent after import and override");
        }
        finally { File.SetAttributes(users, FileAttributes.Normal); }

        foreach (string auction in new[] { "<BoardPage>5</BoardPage>", "<BidderName>INVALID</BidderName>", "<EntityNumberForeground>invalid-color</EntityNumberForeground>" })
        {
            File.WriteAllText(users, "<Users><Auction>" + auction + "</Auction></Users>");
            Program.Throws<FormatException>(() => TestSettingsLoader.Load(usersPath: users), "invalid display setting is rejected before applying");
        }
        File.Delete(board);
        File.Delete(users);
        Directory.Delete(directory);
    }
}
