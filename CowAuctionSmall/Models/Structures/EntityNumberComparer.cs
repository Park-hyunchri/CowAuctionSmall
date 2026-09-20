using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CowAuctionSmall.Models.Structures
{
    public class EntityNumberComparer : IEqualityComparer<gValues>
    {
        public bool Equals(gValues? x, gValues? y)
        {
            // 비교 기준을 설정합니다. 예를 들어, EntityNumber 속성을 기준으로 비교할 수 있습니다.
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.EntityNumber == y.EntityNumber; // EntityNumber가 같으면 true, 다르면 false
        }

        public int GetHashCode([DisallowNull] gValues obj)
        {
            // Id의 해시 코드를 반환합니다.
            return obj.EntityNumber.GetHashCode();
        }
    }
}
