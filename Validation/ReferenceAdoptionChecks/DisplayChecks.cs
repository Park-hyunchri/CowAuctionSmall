using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.Domain.Auction;
using System.Diagnostics;

internal static class DisplayChecks
{
    internal static void Run()
    {
        foreach (var flags in new[]
        {
            (Until: false, Running: false, Stop: false, Reason: ""),
            (Until: false, Running: true, Stop: false, Reason: "Running"),
            (Until: true, Running: true, Stop: false, Reason: "UntilRefresh"),
            (Until: true, Running: true, Stop: true, Reason: "StopTimer")
        })
        {
            var state = PageRotationSnapshot.Create(9, 3, false, flags.Until, flags.Running, flags.Stop, true);
            Program.Check(state.CurrentPage == 3 && state.TotalPages == 3 && !state.IsMaster && state.IsSubFallbackActive
                && state.FreezeReason == flags.Reason && state.IsFrozen == (flags.Reason.Length > 0),
                "rotation snapshot clamp and freeze reason: " + flags.Reason);
        }
        var empty = PageRotationSnapshot.Create(-1, 0, true, false, false, false, false);
        Program.Check(empty.CurrentPage == 1 && empty.TotalPages == 1 && !empty.IsFrozen,
            "empty/negative page inputs clamp to one unpaused page");
        var original = new gValues { SpaceIndex = "7", AuctionResultStatus = "22", Note = "note", BidderName = "BIDDER_A" };
        Program.Check(original.UpdateSignature() == original.Clone().UpdateSignature(), "unchanged clone retains display signature");
        foreach (var edit in new Action<gValues>[]
        {
            cow => cow.BidderName = "BIDDER_B",
            cow => cow.Note = "edited note",
            cow => cow.NoteChangeOwner = "fixture history",
            cow => cow.PaternityMatch = "fixture paternity",
            cow => cow.Reproduction_Imsin_Sujung_Date = "20260917",
            cow => cow.Reproduction_Sujung_KPN = "fixture KPN"
        })
        {
            var updated = original.Clone();
            edit(updated);
            Program.Check(updated.UpdateSignature() != original.UpdateSignature(), "actual display edit changes update signature");
        }

        var slots = Enumerable.Range(0, 501).Select(index => (index * 3).ToString()).ToArray();
        var tracker = new BoardDisplayTracker(slots);
        var rows = slots.Select(space => new gValues { SpaceIndex = space, AuctionResultStatus = "11", Note = "fixture" }).ToArray();
        tracker.Apply(rows, true);
        var watch = Stopwatch.StartNew();
        for (int index = 0; index < 1000; index++) tracker.Apply(new[] { rows[index % rows.Length] }, false);
        watch.Stop();
        Program.Check(tracker.Coverage.DisplayedSlotCount == 501 && tracker.Coverage.LogoSlotCount == 0,
            "501-slot repeated partial input retains complete display coverage");
        var owned = tracker.Snapshot;
        owned[0].Note = "consumer mutation";
        Program.Check(tracker.Snapshot.All(cow => cow.Note == "fixture"), "snapshot consumer cannot mutate retained display state");
        tracker.Apply(new[] { new gValues { SpaceIndex = slots[0], AuctionResultStatus = "24" } }, false);
        Program.Check(tracker.Coverage.DisplayedSlotCount == 500 && tracker.Coverage.LogoSlotCount == 1,
            "partial cancellation changes only the matching slot to logo");
        var updates = new DisplayUpdateTracker();
        foreach (var row in rows)
            Program.Check(updates.ShouldUpdate("panel_" + row.SpaceIndex, row, 0), "new slot needs rendering");
        int repeatedUpdates = 0;
        for (int index = 0; index < 1000; index++)
        {
            var row = rows[index % rows.Length].Clone();
            if (updates.ShouldUpdate("panel_" + row.SpaceIndex, row, 0)) repeatedUpdates++;
        }
        Program.Check(repeatedUpdates == 0, "identical repeated snapshots do not request new display updates");
        foreach (var edit in new Action<gValues>[]
        {
            cow => cow.Note = "new note", cow => cow.NoteChangeOwner = "history",
            cow => cow.BidderName = "BIDDER_B", cow => cow.FrontNoteWord = "prefix",
            cow => cow.FrontNoteWordBrush = "Red", cow => cow.LowestPriceTitle = "시중가",
            cow => cow.Reproduction_Imsin_Sujung_Date = "20260917", cow => cow.GoatPricePerKg = 99m
        })
        {
            var cow = rows[0].Clone();
            updates.ShouldUpdate("edited", rows[0], 0);
            edit(cow);
            Program.Check(updates.ShouldUpdate("edited", cow, 0), "visible scalar changes request update");
            Program.Check(!updates.ShouldUpdate("edited", cow.Clone(), 0), "same edited data does not restart update");
        }
        Program.Check(updates.ShouldUpdate("edited", rows[0], 1), "auction method changes invalidate cached display");
        updates.Remove("edited");
        Program.Check(updates.ShouldUpdate("edited", rows[0], 1), "removed slot invalidates its cached snapshot");
        updates.Clear();
        Program.Check(updates.ShouldUpdate("panel_" + rows[0].SpaceIndex, rows[0], 0), "clearing display cache permits rebuilding");
        Console.WriteLine($"Synthetic tracker load: 501 slots, 1000 partial updates, {watch.ElapsedMilliseconds} ms (no rendering or before/after comparison).");
    }
}
