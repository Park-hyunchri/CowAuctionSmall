using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;

namespace CowAuctionSmall.Application.Abstractions
{
    public interface IAuctionPacketParser
    {
        gValues ParsePacket(string message, UserInfo user);

        AuctionItemUpdate ParseUpdate(string message, UserInfo user, bool isSv, gValues? previous = null)
        {
            var parts = message.Split('|');
            var value = ParsePacket(message, user);
            value.AuctionHouseCode = parts.Length > 1 ? parts[1] : string.Empty;
            value.AuctionRound = parts.Length > 3 ? parts[3] : string.Empty;
            value.SuppliedFields = AuctionItemFields.All;
            return new AuctionItemUpdate(value, AuctionItemFields.All, value.AuctionHouseCode, value.AuctionRound);
        }
    }
}
