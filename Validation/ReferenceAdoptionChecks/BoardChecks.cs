using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Application.Auction;

internal static class BoardChecks
{
    internal static void Run()
    {
        var tracker = new BoardDisplayTracker(new[] { "7", "100000", "7", "3" });
        var input = new[]
        {
            Cow("7", "first"), Cow("7", "last"), Cow("100000"), Cow("42"),
            Cow("invalid"), Cow("0"), Cow("-1"), Cow("")
        };
        tracker.Apply(input, true);
        var coverage = tracker.Coverage;
        Program.Check(coverage.RawRowCount == 8 && coverage.InvalidRowCount == 3 && coverage.DuplicateRowCount == 1,
            "full snapshot separates invalid and duplicate rows");
        Program.Check(coverage.ValidSpaceCount == 4 && coverage.MatchedSpaceCount == 2 && coverage.ExcludedSpaceCount == 2,
            "non-contiguous large board identifiers and outside data are counted independently");
        Program.Check(coverage.ValidSpaceCount == coverage.MatchedSpaceCount + coverage.ExcludedSpaceCount,
            "valid unique space accounting invariant");
        Program.Check(coverage.BoardSlotCount == 4 && coverage.DisplayedSlotCount == 3 && coverage.LogoSlotCount == 1,
            "duplicate physical board slots contribute to display denominator");
        Program.Check(coverage.BoardSlotCount == coverage.DisplayedSlotCount + coverage.LogoSlotCount,
            "physical board slot accounting invariant");

        tracker.Apply(new[] { Cow("3") }, false);
        Program.Check(tracker.Coverage.DisplayedSlotCount == 4 && tracker.Coverage.LogoSlotCount == 0,
            "partial update retains previous data and fills remaining slot");
        Program.Check(tracker.Coverage.RawRowCount == 8 && tracker.Coverage.InvalidRowCount == 3,
            "partial update preserves most recent full response statistics");
        int countBefore = tracker.Snapshot.Count;
        tracker.Apply(Array.Empty<gValues>(), false);
        Program.Check(tracker.Snapshot.Count == countBefore && tracker.Coverage.DisplayedSlotCount == 4,
            "empty partial update preserves existing slots");

        tracker.Apply(Array.Empty<gValues>(), true);
        Program.Check(tracker.Snapshot.Count == 0 && tracker.Coverage.RawRowCount == 0
            && tracker.Coverage.DisplayedSlotCount == 0 && tracker.Coverage.LogoSlotCount == 4,
            "empty full snapshot clears animals and restores all logo slots");
        tracker.Apply(new[] { Cow("42") }, true);
        Program.Check(tracker.Coverage.DisplayedSlotCount == 0 && tracker.Coverage.ExcludedSpaceCount == 1,
            "outside board data never creates extra slots");
        var apiRows = new[] { Cow("7"), Cow("7"), Cow("invalid") };
        var source = BoardSourceStatistics.From(apiRows);
        tracker.Apply(new[] { Cow("7"), Cow("3") }, true, source);
        Program.Check(tracker.Coverage.RawRowCount == 3 && tracker.Coverage.InvalidRowCount == 1
            && tracker.Coverage.DuplicateRowCount == 1 && tracker.Coverage.DisplayedSlotCount == 3,
            "raw response metadata survives deduplication and preserved running overlay");
        var tombstones = tracker.Apply(new[] { new gValues { SpaceIndex = "3", AuctionResultStatus = "00" } }, false);
        Program.Check(tracker.Snapshot.All(item => item.SpaceIndex != "3") && tombstones.Count == 1
            && tombstones[0].AuctionResultStatus == "00" && tracker.Coverage.LogoSlotCount == 2,
            "partial removal deletes retained animal while forwarding logo update to UI");
    }

    private static gValues Cow(string space, string note = "fixture") => new() { SpaceIndex = space, Note = note, AuctionResultStatus = "11" };
}
