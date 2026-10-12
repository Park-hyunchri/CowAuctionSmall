using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CowAuctionSmall.Application.Abstractions
{
    public enum AuctionConnectionResult { Connected, Duplicate, Timeout, Failed }

    /// <summary>시작과 재연결 흐름에 필요한 외부 환경 경계.</summary>
    public interface IAuctionSessionRuntime
    {
        UserInfo? LoadUserSettings();
        bool IsNetworkAvailable { get; }
        bool IsConnected { get; }
        long TotalApiCount { get; }
        Task<AuctionAuthenticationResult?> AuthenticateAsync(UserInfo user);
        Task<List<gValues>?> ReadCachedAuctionAsync(UserInfo user);
        Task<AuctionConnectionResult> ConnectAsync(UserInfo user, string token);
        Task DisconnectAsync();
    }
}
