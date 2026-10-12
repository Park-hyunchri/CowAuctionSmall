using System;

namespace CowAuctionSmall.Domain.Auction
{
    public static class NightShutdownPolicy
    {
        public static bool ShouldShutdown(DateTime localTime, bool auctionRunning, bool synchronizing, TimeSpan sinceAuctionSignal)
            => (localTime.Hour >= 23 || localTime.Hour < 1) && !auctionRunning && !synchronizing
                && sinceAuctionSignal >= TimeSpan.FromMinutes(1);
    }
}
