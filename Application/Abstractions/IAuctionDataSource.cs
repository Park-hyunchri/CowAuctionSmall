using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CowAuctionSmall.Application.Abstractions
{
    /// <summary>경매 상태 처리를 운영 HTTP 연결 없이 검증하기 위한 조회 경계.</summary>
    public interface IAuctionDataSource
    {
        Task<List<string>> ReadSnapshotAsync(UserInfo user, string token, string? date);
        Task<AuctionRoundInfo?> ReadQcnAsync(UserInfo user, string token, string? date, string? qcn = null);
    }

}
