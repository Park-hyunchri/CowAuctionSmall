using System;
using System.Collections.Generic;
using System.Linq;
using CowAuctionSmall.Models.Structures;

namespace CowAuctionSmall.Utils
{
    public static class CowDisplaySelector
    {
        public static gValues? SelectForSpaceIndex(IEnumerable<gValues> candidates)
        {
            if (candidates == null)
                return null;

            return candidates
                .OrderByDescending(IsRunningOrActive)
                .ThenBy(c => CowDistinctionPriority(c.CowDistinction))
                .ThenBy(c => SipNumberPriority(c.SipNumber))
                .FirstOrDefault();
        }

        private static bool IsRunningOrActive(gValues cow)
        {
            return cow.IsRunning || string.Equals(cow.AuctionResultStatus, "11", StringComparison.Ordinal);
        }

        private static int CowDistinctionPriority(string? cowDistinction)
        {
            return cowDistinction switch
            {
                "1" => 1, // 송아지
                "2" => 2, // 비육우
                "3" => 3, // 번식우
                "5" => 5, // 염소
                "6" => 6, // 말
                _ => int.MaxValue
            };
        }

        private static int SipNumberPriority(string? sipNumber)
        {
            return int.TryParse(sipNumber, out var parsed) ? parsed : int.MaxValue;
        }
    }
}
