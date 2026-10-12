using System;

namespace CowAuctionSmall.Presentation.Display
{
    public sealed record PageRotationSnapshot(int CurrentPage, int TotalPages, bool IsMaster,
        bool IsFrozen, bool IsSubFallbackActive, string FreezeReason, bool IsTimerRunning = false)
    {
        public static PageRotationSnapshot Create(int page, int total, bool isMaster, bool untilRefresh,
            bool running, bool stoppedBySignal, bool fallback, bool isTimerRunning = false)
        {
            total = Math.Max(1, total);
            string reason = stoppedBySignal ? "StopTimer" : untilRefresh ? "UntilRefresh" : running ? "Running" : string.Empty;
            return new(Math.Clamp(page, 1, total), total, isMaster, reason.Length > 0, fallback, reason, isTimerRunning);
        }
    }
}
