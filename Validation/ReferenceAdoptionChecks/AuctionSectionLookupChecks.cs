using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Domain.Auction;
using System.Collections.Concurrent;

internal static class AuctionSectionLookupChecks
{
    internal static async Task Run()
    {
        using var ingress = new AuctionIngress();
        var context = new AuctionSectionContext("123", new DateOnly(2026, 9, 30));
        var now = DateTimeOffset.UtcNow;
        var requests = new ConcurrentQueue<(DateOnly Date, int Type, int Section)>();
        var publications = new ConcurrentQueue<AuctionSectionState>();
        var first = new TaskCompletionSource<AuctionSectionRange?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<DateOnly, int, int, CancellationToken, Task<AuctionSectionRange?>> read = (date, type, section, ct) =>
        {
            requests.Enqueue((date, type, section));
            return first.Task;
        };
        using var lookup = new AuctionSectionLookup(ingress, () => context,
            (date, type, section, ct) => read(date, type, section, ct), publications.Enqueue, now: () => now);
        async Task Apply(AuctionSectionState state, bool refresh = false)
        {
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ingress.TryPost(() => { lookup.Apply(state, refresh); done.TrySetResult(); });
            await done.Task.WaitAsync(TimeSpan.FromSeconds(3));
        }
        await Apply(As());
        await Until(() => requests.Count == 1);
        for (int i = 0; i < 20; i++) await Apply(As());
        Program.Check(requests.Count == 1 && !publications.Last().IsRangeConfirmed,
            "repeated AS shares one pending range request without blocking the state queue");
        first.SetResult(new AuctionSectionRange(1, 999));
        await Until(() => publications.Last().IsRangeConfirmed);
        Program.Check(publications.Last().Range == new AuctionSectionRange(1, 999) &&
            publications.Last().AuctionDate == context.Date, "background response confirms the range for the selected auction date");
        int previous = publications.Count;
        await Apply(As());
        Program.Check(requests.Count == 1 && publications.Skip(previous).All(s => s.IsRangeConfirmed),
            "cached AS does not query or briefly replace the range with unknown text");

        var slow = new TaskCompletionSource<AuctionSectionRange?>(TaskCreationOptions.RunContinuationsAsynchronously);
        read = (date, type, section, ct) =>
        {
            requests.Enqueue((date, type, section));
            return section == 2 ? slow.Task : Task.FromResult<AuctionSectionRange?>(new(1000, 1500));
        };
        await Apply(As(2));
        await Until(() => requests.Count == 2);
        await Apply(As(3));
        await Until(() => publications.Last().SectionNumber == 3 && publications.Last().IsRangeConfirmed);
        slow.SetResult(new(201, 400));
        await Apply(As(3));
        Program.Check(publications.Last().Range == new AuctionSectionRange(1000, 1500),
            "a late previous-section response cannot overwrite the current section");

        var oldDate = new TaskCompletionSource<AuctionSectionRange?>(TaskCreationOptions.RunContinuationsAsynchronously);
        read = (date, type, section, ct) => { requests.Enqueue((date, type, section)); return oldDate.Task; };
        await Apply(As(4));
        int target = requests.Count + (requests.Last().Section == 4 ? 0 : 1);
        await Until(() => requests.Count >= target && requests.Last().Section == 4);
        context = context with { Date = context.Date.AddDays(1) };
        oldDate.SetResult(new(1, 999));
        read = (date, type, section, ct) => { requests.Enqueue((date, type, section)); return Task.FromResult<AuctionSectionRange?>(new(20, 30)); };
        await Apply(As(4));
        await Until(() => publications.Last().IsRangeConfirmed && publications.Last().AuctionDate == context.Date);
        Program.Check(publications.Last().Range == new AuctionSectionRange(20, 30) && requests.Last().Date == context.Date,
            "date change queries a separate key and rejects the old date response");

        int beforeRefresh = requests.Count;
        await Apply(As(4), refresh: true);
        await Until(() => publications.Last().IsRangeConfirmed);
        Program.Check(requests.Count == beforeRefresh + 1, "explicit refresh bypasses a successful cache entry");

        read = (date, type, section, ct) => { requests.Enqueue((date, type, section)); return Task.FromResult<AuctionSectionRange?>(null); };
        int beforeFail = publications.Count;
        await Apply(As(5));
        await Until(() => publications.Count >= beforeFail + 2);
        int failedCalls = requests.Count;
        await Apply(As(5));
        Program.Check(requests.Count == failedCalls && !publications.Last().IsRangeConfirmed,
            "empty responses retain unknown status and suppress retry storms");
        now = now.AddSeconds(31);
        beforeFail = publications.Count;
        await Apply(As(5));
        await Until(() => publications.Count >= beforeFail + 2);
        Program.Check(requests.Count == failedCalls + 1, "a later signal retries after the failure cooldown");

        var pending = new TaskCompletionSource<AuctionSectionRange?>(TaskCreationOptions.RunContinuationsAsynchronously);
        read = (date, type, section, ct) => { requests.Enqueue((date, type, section)); return pending.Task; };
        await Apply(As(6));
        await Until(() => requests.Last().Section == 6);
        await Apply(AuctionSectionState.Empty, refresh: true);
        pending.SetResult(new(1, 999));
        await Apply(AuctionSectionState.Empty);
        Program.Check(publications.Last() == AuctionSectionState.Empty, "mode reset cancels a pending range and removes its status");

        int noLookup = requests.Count;
        await Apply(As(0));
        await Apply(AuctionSectionState.FromSignal(new[] { "AS" }));
        Program.Check(requests.Count == noLookup, "unspecified or invalid sections are never queried");
        var supplied = AuctionSectionState.FromSignal(new[] { "SZ", "123", "20261001", "1", "1", "P", "10", "99" });
        await Apply(supplied);
        await Apply(As());
        Program.Check(publications.Last().Range == new AuctionSectionRange(10, 99) && requests.Count == noLookup,
            "an explicit SZ range can satisfy a later AS for the same day and section");
        now = now.AddMinutes(6);
        read = (date, type, section, ct) => { requests.Enqueue((date, type, section)); return Task.FromResult<AuctionSectionRange?>(new(30, 40)); };
        await Apply(As());
        await Until(() => publications.Last().Range == new AuctionSectionRange(30, 40));
        Program.Check(requests.Count == noLookup + 1, "successful cached ranges expire and refresh on the next signal");
        lookup.Dispose();
        noLookup = requests.Count;
        await Apply(As(9));
        Program.Check(requests.Count == noLookup, "disposed lookup cannot start another request");
    }

    private static AuctionSectionState As(int section = 1) => AuctionSectionState.FromSignal(
        new[] { "AS", "123", "", "1", "", "", "8002", "", "", "", "", "", section.ToString(), "1" });
    private static async Task Until(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        while (!condition()) await Task.Delay(5, timeout.Token);
    }
}
