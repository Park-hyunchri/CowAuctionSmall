using CowAuctionSmall.Domain.Auction;
using System.Collections.Generic;
using System.Linq;

namespace CowAuctionSmall.Application.Auction
{
    public sealed record BoardSourceStatistics(int RawRowCount, int InvalidRowCount, int DuplicateRowCount)
    {
        public static BoardSourceStatistics From(IEnumerable<gValues> data)
        {
            var rows = data.ToArray();
            var valid = rows.Where(item => item != null && int.TryParse(item.SpaceIndex, out int number) && number >= 0).ToArray();
            return new(rows.Length, rows.Length - valid.Length,
                valid.Length - valid.Select(item => item.SpaceIndex.Trim()).Distinct().Count());
        }
    }
}
