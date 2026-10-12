using CowAuctionSmall.Domain.Auction;

namespace CowAuctionSmall.Application.Messaging
{
    public sealed record AuctionDisplayStateMessage(AuctionDisplayState State);
}
