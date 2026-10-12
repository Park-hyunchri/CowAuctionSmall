using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Application.Auction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CowAuctionSmall.Presentation.Display
{
    public sealed record BoardDisplayCoverage(int RawRowCount, int InvalidRowCount, int DuplicateRowCount,
        int ValidSpaceCount, int MatchedSpaceCount, int ExcludedSpaceCount,
        int BoardSlotCount, int DisplayedSlotCount, int LogoSlotCount);

    /// <summary>전체 조회의 통계와 부분 갱신 후 슬롯 상태를 구분한다. UI 스레드에서 사용한다.</summary>
    public sealed class BoardDisplayTracker
    {
        private readonly string[] _slots;
        private readonly HashSet<string> _configured;
        private readonly Dictionary<string, gValues> _items = new(StringComparer.Ordinal);
        private int _rawRows;
        private int _invalidRows;
        private int _duplicateRows;

        public BoardDisplayTracker(IEnumerable<string> configuredSlots)
        {
            _slots = configuredSlots.Select(s => s.Trim()).ToArray();
            _configured = new HashSet<string>(_slots, StringComparer.Ordinal);
        }

        public IReadOnlyList<gValues> Snapshot => _items.Values.Select(item => item.Clone()).ToArray();

        public BoardDisplayCoverage Coverage
        {
            get
            {
                int matched = _items.Keys.Count(_configured.Contains);
                int displayed = _slots.Count(slot => _items.TryGetValue(slot, out var item) && IsDisplayed(item));
                return new(_rawRows, _invalidRows, _duplicateRows, _items.Count, matched,
                    _items.Count - matched, _slots.Length, displayed, _slots.Length - displayed);
            }
        }

        public IReadOnlyList<gValues> Apply(IEnumerable<gValues> data, bool isFullSnapshot, BoardSourceStatistics? sourceStatistics = null)
        {
            var rows = data.ToArray();
            var valid = rows.Where(item => item != null && IsValidSpace(item.SpaceIndex)).ToArray();
            var groups = valid.GroupBy(item => item.SpaceIndex.Trim(), StringComparer.Ordinal).ToArray();
            if (isFullSnapshot)
            {
                _items.Clear();
            }
            if (isFullSnapshot || sourceStatistics != null)
            {
                _rawRows = sourceStatistics?.RawRowCount ?? rows.Length;
                _invalidRows = sourceStatistics?.InvalidRowCount ?? rows.Length - valid.Length;
                _duplicateRows = sourceStatistics?.DuplicateRowCount ?? valid.Length - groups.Length;
            }
            var selected = new List<gValues>();
            foreach (var group in groups)
            {
                var winner = CowDisplaySelector.SelectForSpaceIndex(group)!.Clone();
                winner.SpaceIndex = group.Key;
                if (!isFullSnapshot && winner.AuctionResultStatus == "00" &&
                    string.IsNullOrEmpty(winner.EntityNumber) && winner.SipNumber == "-1")
                    _items.Remove(group.Key);
                else
                    _items[group.Key] = winner;
                selected.Add(winner.Clone());
            }
            return selected;
        }

        private static bool IsValidSpace(string? value) =>
            int.TryParse(value, out int number) && number >= 0;

        private static bool IsDisplayed(gValues item) => AuctionStateResolver.Resolve(item) is
            DisplayAuctionState.Ready or DisplayAuctionState.Running or DisplayAuctionState.Sold or DisplayAuctionState.Unsold;
    }
}
