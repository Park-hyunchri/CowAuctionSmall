using System;
using System.Threading.Tasks;

namespace CowAuctionSmall.Application.Abstractions
{
    public interface IDailyCacheStore
    {
        string Epoch { get; }
        event Action? DayChanged;
        event Action? Unavailable;
        event Action? CleanupIncomplete;
        Task<bool> EnsureTodayAsync();
        Task<string?> GetCacheAsync(string type, string key);
        Task<bool> SaveCacheAsync(string type, string key, string json, string epoch);
    }
}
