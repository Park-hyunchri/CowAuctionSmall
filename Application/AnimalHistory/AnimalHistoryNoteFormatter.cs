using CowAuctionSmall.Domain.AnimalHistory;
using System;
using System.Linq;

namespace CowAuctionSmall.Application.AnimalHistory
{
    public static class AnimalHistoryNoteFormatter
    {
        public static AnimalHistoryOwner? GetLatestOwner(AnimalHistoryResult result)
        {
            if (result.Status != HistoryStatus.Success || result.Entries.Count == 0) return null;
            var entries = result.Entries.OrderByDescending(e => e.Date, StringComparer.Ordinal).ToList();
            // Equal dates do not establish an order between different reports.
            if (entries.Count > 1 && entries[0].Date == entries[1].Date) return null;
            return new AnimalHistoryOwner(entries[0].Owner, entries[0].FarmAddress);
        }

        public static string FormatAnimalHistoryNote(AnimalHistoryResult result)
        {
            if (result.Status != HistoryStatus.Success) return "";
            var entries = result.Entries.OrderBy(e => e.Date, StringComparer.Ordinal).ToList();
            for (int i = entries.Count - 1; i > 0; i--)
            {
                var current = entries[i];
                var previous = entries[i - 1];
                if (current.Type != "양수") continue;
                // Do not infer identity or ordering from an equal name or equal date.
                if (current.Date == previous.Date || entries.Count(e => e.Date == current.Date) > 1 ||
                    entries.Count(e => e.Date == previous.Date) > 1 || current.Owner == previous.Owner)
                    return "";
                return $"{previous.Owner} → {current.Owner}(양수 {current.Date.Substring(0, 4)}-{current.Date.Substring(4, 2)}-{current.Date.Substring(6, 2)})";
            }
            return "";
        }
    }
}
