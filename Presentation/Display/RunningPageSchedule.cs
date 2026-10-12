using System;
using System.Collections.Generic;
using System.Linq;

namespace CowAuctionSmall.Presentation.Display
{
    /// <summary>Calculates the existing shared UTC page schedule without starting a timer.</summary>
    public static class RunningPageSchedule
    {
        public static (int index, int secondsLeft) Calculate(DateTime utcNow, int totalRunningPage, IReadOnlyList<int> pageTime)
        {
            var pages = Math.Clamp(totalRunningPage, 1, pageTime.Count);
            var totalTime = pageTime.Take(pages).Sum();
            if (totalTime <= 0)
                return (0, pageTime[0]);

            var secondsSinceEpoch = (int)(utcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            var currentCycleTime = secondsSinceEpoch % totalTime;

            var elapsedTime = 0;
            for (int i = 0; i < pages; i++)
            {
                var duration = pageTime[i];
                if (currentCycleTime < elapsedTime + duration)
                {
                    var secondsLeft = (elapsedTime + duration) - currentCycleTime;
                    return (i, Math.Max(1, secondsLeft));
                }
                elapsedTime += duration;
            }

            return (0, pageTime[0]);
        }

    }
}
