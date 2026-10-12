using CowAuctionSmall.Domain.Auction;
using System;
using System.Collections.Generic;

namespace CowAuctionSmall.Presentation.Display
{
    /// <summary>화면별 마지막 적용 데이터를 소유하여 원본 변경과 해시 충돌의 영향을 받지 않는다.</summary>
    public sealed class DisplayUpdateTracker
    {
        private readonly Dictionary<string, (gValues Item, int Method)> _last = new(StringComparer.Ordinal);

        public bool ShouldUpdate(string panelKey, gValues item, int auctionMethod)
        {
            if (_last.TryGetValue(panelKey, out var previous) && previous.Method == auctionMethod &&
                previous.Item.Equals(item))
                return false;

            _last[panelKey] = (item.Clone(), auctionMethod);
            return true;
        }

        public void Remove(string panelKey) => _last.Remove(panelKey);
        public void Clear() => _last.Clear();
    }
}
