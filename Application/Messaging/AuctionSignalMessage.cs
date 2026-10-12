using System;
using CowAuctionSmall.Domain.Auction;

namespace CowAuctionSmall.Application.Messaging
{
    public sealed record AuctionSignalMessage(DateTime ReceivedAt, string Text);
    public sealed record AuctionSectionMessage(AuctionSectionState State);
}
