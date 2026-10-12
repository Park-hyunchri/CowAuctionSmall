using CowAuctionSmall.Domain.Auction;

internal static class StateChecks
{
    internal static void Run()
    {
        Program.Check(AuctionStateResolver.Resolve(null) == DisplayAuctionState.Logo, "empty slot displays logo");
        foreach (bool running in new[] { false, true })
        {
            foreach (string? status in new string?[] { null, "", " ", "unknown", "99", "9999999999999999999999", "00" })
                Expect(status, running, DisplayAuctionState.Logo);
            Expect("11", running, running ? DisplayAuctionState.Running : DisplayAuctionState.Ready);
            Expect("22", running, DisplayAuctionState.Sold);
            Expect("23", running, DisplayAuctionState.Unsold);
            Expect("24", running, DisplayAuctionState.Canceled);
            Expect(" 22 ", running, DisplayAuctionState.Sold);
            Expect("+23", running, DisplayAuctionState.Unsold);
        }

        var cow = new gValues { AuctionResultStatus = "11" };
        Program.Check(AuctionStateResolver.Resolve(cow) == DisplayAuctionState.Ready, "ready before start");
        cow.IsRunning = true;
        Program.Check(AuctionStateResolver.Resolve(cow) == DisplayAuctionState.Running, "start enters running");
        cow.AuctionResultStatus = "22";
        Program.Check(AuctionStateResolver.Resolve(cow) == DisplayAuctionState.Sold, "result before end wins over running flag");
        cow.IsRunning = false;
        Program.Check(AuctionStateResolver.Resolve(cow) == DisplayAuctionState.Sold, "end after result retains sold");
        cow.AuctionResultStatus = "11";
        cow.IsRunning = true;
        Program.Check(AuctionStateResolver.Resolve(cow) == DisplayAuctionState.Running, "same animal reauction after explicit reset");
        cow.IsRunning = false;
        Program.Check(AuctionStateResolver.Resolve(cow) == DisplayAuctionState.Ready, "end before result uses existing ready display");
        cow.AuctionResultStatus = "23";
        Program.Check(AuctionStateResolver.Resolve(cow) == DisplayAuctionState.Unsold, "result arriving after end");
    }

    private static void Expect(string? status, bool running, DisplayAuctionState expected)
    {
        var cow = new gValues { AuctionResultStatus = status!, IsRunning = running };
        Program.Check(AuctionStateResolver.Resolve(cow) == expected, $"status {status ?? "null"}, running {running}: {expected}");
    }
}
