using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.ViewModels;
using System.Globalization;

internal static class ItemDisplayChecks
{
    internal static void Run()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ko-KR");
            foreach (var (raw, formatted) in new (string?, string)[]
            {
                ("612", "612"), ("1234", "1,234"), ("0", "0"), ("-", "-"),
                (null, ""), ("", ""), (" ", " "), ("1,234", "1,234"),
                ("1234.6", "1,235"), ("-1234", "-1,234"), ("결장", "결장"), ("INVALID", "INVALID")
            })
            {
                var item = new AuctionItemDisplayViewModel(new gValues { LowestPrice = raw!, BidPrice = raw!, Weight = raw! });
                Program.Check(item.LowestPriceText == formatted && item.BidPriceText == formatted && item.FormattedWeightText == formatted,
                    "shared number display: " + (raw ?? "null"));
                Program.Check(item.LowestPriceOrDashText == (raw == "결장" ? "-" : formatted), "absence policy remains separate from number formatting");
            }
            foreach (var (raw, text) in new (string?, string)[]
            {
                (null, "-"), ("", "-"), ("  ", "-"), ("0", "-"), ("-", "-"),
                (" 0 ", "-"), ("444", "444kg"), (" 444 ", "444kg"), ("1234", "1234kg")
            })
            {
                var item = new AuctionItemDisplayViewModel(new gValues { Weight = raw! });
                Program.Check(item.WeightWithUnitText == text && item.WeightText == (raw ?? ""), "weight unit policy preserves raw display: " + raw);
            }
            foreach (var (raw, show) in new (string?, bool)[]
            {
                (null, false), ("", false), ("-", false), ("0", false), ("-1", false),
                ("1", true), (" 4 ", true), ("X", true), ("미정", true), ("x", false),
                (" X ", false), ("4.5", false), ("INVALID", false)
            })
                Program.Check(new AuctionItemDisplayViewModel(new gValues { Pregnant = raw! }).ShowPregnancy == show,
                    "pregnancy visibility policy: " + raw);

            var source = new gValues { LowestPrice = "1234", BidPrice = "4567", Weight = "444", Pregnant = "4" };
            var first = new AuctionItemDisplayViewModel(source);
            source.LowestPrice = "9876"; source.BidPrice = "8765"; source.Weight = "0"; source.Pregnant = "0";
            Program.Check(first.Snapshot.LowestPrice == "1234" && first.LowestPriceText == "1,234" && first.WeightWithUnitText == "444kg" && first.ShowPregnancy,
                "source mutation leaves published display snapshot intact");
            var refreshed = new AuctionItemDisplayViewModel(source);
            Program.Check(refreshed.LowestPriceText == "9,876" && refreshed.BidPriceText == "8,765" && refreshed.WeightWithUnitText == "-" && !refreshed.ShowPregnancy,
                "changed snapshot refreshes all three migrated responsibilities");
            Program.Check(source.LowestPrice == "9876" && source.BidPrice == "8765", "display formatting does not write commas into source prices");

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var localized = new AuctionItemDisplayViewModel(new gValues { LowestPrice = "1234,6" });
            Program.Check(localized.LowestPriceText == "1.235", "parse and display consistently use the current culture");
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
