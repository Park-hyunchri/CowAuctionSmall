using CowAuctionSmall.Domain.Auction;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CowAuctionSmall.Application.Abstractions
{
    public interface IAuctionSectionSource
    {
        Task<AuctionSectionRange?> ReadSectionAsync(DateOnly date, int cowType, int sectionNumber, CancellationToken cancellationToken);
    }
}
