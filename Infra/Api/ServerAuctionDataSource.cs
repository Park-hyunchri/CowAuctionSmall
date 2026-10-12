using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CowAuctionSmall.Infra.Api
{
    public sealed class ServerAuctionDataSource : IAuctionDataSource, IAuctionSectionSource
    {
        private readonly ServerConn _connection;
        public ServerAuctionDataSource(ServerConn connection) => _connection = connection;
        public Task<AuctionSectionRange?> ReadSectionAsync(DateOnly date, int cowType, int sectionNumber, CancellationToken cancellationToken)
            => _connection.ReadAuctionSectionRangeAsync(date, cowType, sectionNumber, cancellationToken);
        public Task<List<string>> ReadSnapshotAsync(UserInfo user, string token, string? date)
            => _connection.SvInfoRequest(user, token, date);
        public async Task<AuctionRoundInfo?> ReadQcnAsync(UserInfo user, string token, string? date, string? qcn = null)
        {
            var result = await _connection.PostQcn(user, token, date ?? string.Empty, qcn).ConfigureAwait(false);
            return result == null ? null : new AuctionRoundInfo { AuctionDate = result.AucDt };
        }
    }
}
