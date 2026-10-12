namespace CowAuctionSmall.Domain.AnimalHistory
{
    public enum HistoryLookupOutcome { Cache, Success, Failed, Stopped }

    // HasValue includes a valid fallback; Outcome records what happened to this lookup.
    public sealed record HistoryLookupResult(bool HasValue, string Note, HistoryLookupOutcome Outcome)
    {
        public AnimalHistoryOwner? LatestOwner { get; init; }
        public static HistoryLookupResult Missing { get; } = new(false, "", HistoryLookupOutcome.Failed);
    }
}
