namespace CowAuctionSmall.Domain.AnimalHistory
{
    public sealed record HistoryItemUpdate(string Number, string Epoch, long RequestId, HistoryLookupResult Result);
}
