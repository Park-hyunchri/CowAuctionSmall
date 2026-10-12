using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Application.AnimalHistory;
using CowAuctionSmall.Domain.AnimalHistory;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Presentation.Display;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;

internal static class HistoryBackgroundChecks
{
    private const string A = "000000000000001", B = "000000000000002", C = "000000000000003";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(12);
    private static AnimalHistoryResult Success() => new() { Entries = new()
    {
        new() { Date = "20260101", Type = "등록", Owner = "FIXTURE_A" },
        new() { Date = "20260201", Type = "양수", Owner = "FIXTURE_B" }
    } };
    private static AnimalTraceSettings Settings(int concurrent = 1, int limit = 0) => new()
        { Enabled = true, ServiceKey = "fixture", MaxConcurrentRequests = concurrent, DailyLimit = limit };

    internal static async Task Run()
    {
        await CacheAndPublication();
        await ConfigurableInterval();
        await ProgressivePublication();
        await PendingPartialInvalidation();
        await TargetChurnWithBlockedUi();
        await StorageFailure();
        await AdmissionAndBudget();
        await RetryDoesNotBlock();
        await CancellationAndSharing();
        await TargetChangeAndProgress();
        await OverlappingTargets();
        await SyntheticComparison();
    }

    private static async Task CacheAndPublication()
    {
        var store = new Store();
        await store.SaveCacheAsync("animal_history", B, JsonConvert.SerializeObject(Success()), store.Epoch);
        var client = new Client();
        using var service = new AnimalHistoryService(store, client); service.Configure(Settings());
        var ui = new ConcurrentQueue<Action>(); int applied = 0; HistoryBatchProgress? seen = null;
        using var batch = new AnimalHistoryCoordinator(service.LookupAsync, service.GetCachedNoteAsync,
            () => service.Epoch, ui.Enqueue, _ => applied++);
        batch.ProgressChanged += progress => seen = progress;
        batch.UpdateTargets(new[] { A, B, A, "invalid" });
        var request = await HistoryTestUi.Await(client.Next(), ui);
        await HistoryTestUi.Until(() => applied == 1, ui);
        Program.Check(request.Number == A && batch.Progress.Total == 2,
            "later cached animal prepared before slow first HTTP response");
        Program.Check(store.Reads.Contains("animal_history:" + B), "persistent cache is included in preparation");
        Flush(ui);
        Program.Check(applied == 1 && batch.TryGetNote(B, out _) && !batch.TryGetNote(A, out _) && !batch.Completion.IsCompleted,
            "prepared cache is displayed while the first HTTP response is still pending");
        request.Reply.SetResult(new AnimalHistoryResult());
        await HistoryTestUi.Complete(batch.Completion, ui);
        Program.Check(batch.Progress.Stage == HistoryBatchStage.Complete && applied == 2,
            "completion includes actual single-item UI application");
        Flush(ui);
        Program.Check(applied == 2 && batch.TryGetNote(A, out string empty) && empty == "" && batch.TryGetNote(B, out var note) && note.Contains("FIXTURE_B"),
            "final commit adds successful empty response and retains the early cached note");
        await HistoryTestUi.Until(() => seen?.Stage == HistoryBatchStage.Complete, ui);
        Program.Check(seen?.Stage == HistoryBatchStage.Complete && seen.Processed == 2 && seen.Cached == 1 && seen.Succeeded == 1 && seen.Notes == 1,
            "progress distinguishes successful lookups from nonempty display notes");
        int calls = client.Calls;
        batch.UpdateTargets(new[] { B, A });
        Program.Check(client.Calls == calls, "same target polling does not restart background work");
        var cachedResult = await service.LookupAsync(B);
        Program.Check(cachedResult.Outcome == HistoryLookupOutcome.Cache && client.Calls == 1, "warm cache uses no HTTP");
        var failed = service.LookupAsync(B, true);
        (await client.Next()).Reply.SetResult(new AnimalHistoryResult { Status = HistoryStatus.InvalidResponse });
        var fallback = await failed.WaitAsync(Timeout);
        Program.Check(fallback.HasValue && fallback.Outcome == HistoryLookupOutcome.Failed && fallback.Note.Contains("FIXTURE_B"),
            "refresh failure keeps cached text but reports failure rather than success");
        var throttled = await service.LookupAsync(B, true);
        Program.Check(throttled.Outcome == HistoryLookupOutcome.Failed && client.Calls == 2, "failure cooldown avoids repeat HTTP and preserves failure status");
    }

    private static async Task ConfigurableInterval()
    {
        string path = System.IO.Path.GetTempFileName();
        try
        {
            foreach (var (value, expected) in new[] { ("", 1000), ("250", 250), (" 500 ", 500), ("2000", 2000),
                ("1", 250), ("0", 1000), ("-1", 1000), ("invalid", 1000), ("999999999999", 1000) })
            {
                string tag = value == "" ? "" : $"<RequestIntervalMs>{value}</RequestIntervalMs>";
                System.IO.File.WriteAllText(path, "<User><AnimalTrace>" + tag + "</AnimalTrace></User>");
                var parsed = new CowAuctionSmall.Infra.XmlSettings.UserXmlParser().ParseXml(path).AnimalTrace;
                Program.Check(parsed.RequestIntervalMs == expected, "request interval XML preserves defaults and validates values: " + value);
                Program.Check(parsed.MaxConcurrentRequests == 1, "interval setting does not increase HTTP concurrency");
            }
        }
        finally { System.IO.File.Delete(path); }
        var store = new Store(); var client = new Client();
        using var service = new AnimalHistoryService(store, client);
        var settings = Settings(); settings.RequestIntervalMs = 250; service.Configure(settings);
        var a = service.LookupAsync(A); var first = await client.Next();
        var b = service.LookupAsync(B); first.Reply.SetResult(Success());
        var second = await client.Next(); second.Reply.SetResult(Success());
        await Task.WhenAll(a, b).WaitAsync(Timeout);
        double gap = second.Started - first.Started;
        Program.Check(gap >= .24 && gap < .95 && client.MaximumActive == 1,
            "configured 250ms pacing replaces hard one-second floor without overlapping HTTP");
        Console.WriteLine($"History configured interval: two fake immediate responses, start gap {gap * 1000:F1}ms.");
    }

    private static async Task ProgressivePublication()
    {
        var ui = new ConcurrentQueue<Action>(); int applied = 0, calls = 0;
        var replies = new[] { A, B, C }.ToDictionary(n => n,
            _ => new TaskCompletionSource<HistoryLookupResult>(TaskCreationOptions.RunContinuationsAsynchronously));
        using var batch = new AnimalHistoryCoordinator((n, _, token) =>
        {
            Interlocked.Increment(ref calls); return replies[n].Task.WaitAsync(token);
        }, null, () => "one", ui.Enqueue, _ => applied++);
        batch.UpdateTargets(new[] { A, B, C }, refresh: true);
        await Until(() => Volatile.Read(ref calls) == 3);
        replies[A].SetResult(new(true, "FIRST", HistoryLookupOutcome.Success)
            { LatestOwner = new("OWNER", "ADDRESS") });
        await Until(() => !ui.IsEmpty);
        Flush(ui);
        Program.Check(applied == 1 && batch.TryGetHistory(A, out var first) && first.LatestOwner?.FarmAddress == "ADDRESS"
            && !batch.TryGetHistory(B, out _) && !batch.Completion.IsCompleted && batch.Progress.Stage == HistoryBatchStage.Loading,
            "new note and third-page owner/address publish while other animals remain pending");
        for (int i = 0; i < 20; i++) batch.UpdateTargets(new[] { A, B, C }, refresh: true);
        Program.Check(calls == 3, "partial publication does not end pending manual refresh or duplicate requests");
        replies[B].SetResult(new(true, "", HistoryLookupOutcome.Success));
        await Until(() => !ui.IsEmpty);
        Program.Check(ui.Count == 1 && !batch.TryGetHistory(B, out _), "single result waits for its UI callback without changing other animals");
        replies[C].SetResult(new(true, "LAST", HistoryLookupOutcome.Success));
        await HistoryTestUi.Complete(batch.Completion, ui);
        Program.Check(batch.Progress.Stage == HistoryBatchStage.Complete,
            "completion waits until all individual UI results were applied");
        Flush(ui);
        Program.Check(applied == 3 && batch.TryGetNote(B, out var empty) && empty == "" && batch.TryGetNote(C, out var last) && last == "LAST"
            && batch.Progress.Stage == HistoryBatchStage.Complete && batch.Progress.Processed == 3 && batch.Progress.Notes == 2,
            "final publication preserves early values, empty success and accurate terminal counts");
    }

    private static async Task PendingPartialInvalidation()
    {
        foreach (string change in new[] { "targets", "epoch", "reset", "dispose" })
        {
            var ui = new ConcurrentQueue<Action>(); int applied = 0; string epoch = "one";
            var held = new TaskCompletionSource<HistoryLookupResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var batch = new AnimalHistoryCoordinator((n, _, token) => n == A
                ? Task.FromResult(new HistoryLookupResult(true, "OLD", HistoryLookupOutcome.Success)) : held.Task.WaitAsync(token),
                null, () => epoch, ui.Enqueue, _ => applied++);
            batch.UpdateTargets(new[] { A, B });
            await Until(() => !ui.IsEmpty);
            if (change == "targets") batch.UpdateTargets(new[] { B });
            else if (change == "epoch") epoch = "two";
            else if (change == "reset") batch.Reset();
            else batch.Dispose();
            Flush(ui);
            Program.Check(applied == 0 && !batch.TryGetNote(A, out _), "queued partial result rejected after " + change);
            held.TrySetResult(HistoryLookupResult.Missing);
            await HistoryTestUi.Complete(batch.Completion, ui);
        }
    }

    private static async Task Until(Func<bool> condition)
    {
        var timer = Stopwatch.StartNew();
        while (!condition())
        {
            if (timer.Elapsed > Timeout) throw new TimeoutException("Expected history publication.");
            await Task.Delay(10);
        }
    }

    private static async Task AdmissionAndBudget()
    {
        var store = new Store(); var client = new Client();
        using var service = new AnimalHistoryService(store, client); service.Configure(Settings(2, 2));
        var a = service.LookupAsync(A); var first = await client.Next();
        var b = service.LookupAsync(B); var second = await client.Next();
        Program.Check(!a.IsCompleted && client.MaximumActive == 2, "configured concurrency overlaps slow HTTP responses");
        Program.Check(second.Started - first.Started >= .98, "overlapping requests still respect the one-second start interval");
        var c = service.LookupAsync(C);
        first.Reply.SetResult(Success()); second.Reply.SetResult(Success());
        await Task.WhenAll(a, b, c).WaitAsync(Timeout);
        Program.Check(client.Calls == 2 && client.MaximumActive == 2 && (await c).Outcome == HistoryLookupOutcome.Stopped,
            "daily cap prevents queued third request");
        Program.Check(store.BudgetCount == 2, "parallel successes cannot overwrite persisted budget increments");
        using var restarted = new AnimalHistoryService(store, new Client()); restarted.Configure(Settings(2, 2));
        Program.Check((await restarted.LookupAsync(C)).Outcome == HistoryLookupOutcome.Stopped, "persisted cap applies after restart");

        var deniedStore = new Store(); var deniedClient = new Client();
        using var denied = new AnimalHistoryService(deniedStore, deniedClient);
        var fastSettings = Settings(); fastSettings.RequestIntervalMs = 250; denied.Configure(fastSettings);
        var active = denied.LookupAsync(A); var request = await deniedClient.Next();
        var queued = denied.LookupAsync(B);
        request.Reply.SetResult(new AnimalHistoryResult { Status = HistoryStatus.QuotaExceeded });
        await Task.WhenAll(active, queued).WaitAsync(Timeout);
        Program.Check(deniedClient.Calls == 1 && (await queued).Outcome == HistoryLookupOutcome.Stopped,
            "quota response blocks unissued requests even with 250ms pacing");
        Program.Check(deniedStore.BudgetStop == nameof(HistoryStatus.QuotaExceeded), "stop persists without losing request count");
    }

    private static async Task RetryDoesNotBlock()
    {
        var store = new Store(); var client = new Client();
        using var service = new AnimalHistoryService(store, client); service.Configure(Settings());
        var a = service.LookupAsync(A); var first = await client.Next();
        var b = service.LookupAsync(B);
        first.Reply.SetResult(new AnimalHistoryResult { Status = HistoryStatus.TransientError });
        var next = await client.Next();
        Program.Check(next.Number == B, "other animal runs while first request waits for retry");
        next.Reply.SetResult(Success());
        var retry = await client.Next();
        Program.Check(retry.Number == A && client.MaximumActive == 1, "retry rejoins admission without exceeding production concurrency");
        retry.Reply.SetResult(Success());
        await Task.WhenAll(a, b).WaitAsync(Timeout);
        Program.Check(store.BudgetCount == 3, "each actual retry counts against budget");
    }

    private static async Task CancellationAndSharing()
    {
        var store = new Store(); var client = new Client();
        using var service = new AnimalHistoryService(store, client); service.Configure(Settings());
        using var subscriber = new CancellationTokenSource();
        var first = service.LookupAsync(A, false, subscriber.Token);
        var request = await client.Next();
        var shared = service.LookupAsync(A);
        subscriber.Cancel();
        try { await first; throw new Exception("subscriber should cancel"); } catch (OperationCanceledException) { }
        Program.Check(!request.Cancelled.Task.IsCompleted && client.Calls == 1, "one departing subscriber does not cancel shared HTTP");
        request.Reply.SetResult(Success());
        Program.Check((await shared.WaitAsync(Timeout)).HasValue, "remaining subscriber receives shared result");

        var active = service.LookupAsync(B); var held = await client.Next();
        using var stale = new CancellationTokenSource();
        var waiting = service.LookupAsync(C, false, stale.Token); stale.Cancel();
        try { await waiting; throw new Exception("waiting subscriber should cancel"); } catch (OperationCanceledException) { }
        held.Reply.SetResult(Success()); await active.WaitAsync(Timeout);
        Program.Check(client.Calls == 2, "last subscriber cancellation removes unissued request");
        var epochClient = new Client(); var epochStore = new Store();
        using var epochService = new AnimalHistoryService(epochStore, epochClient); epochService.Configure(Settings());
        var old = epochService.LookupAsync(A); var late = await epochClient.Next();
        epochStore.Advance(); late.Reply.SetResult(Success());
        Program.Check(!(await old.WaitAsync(Timeout)).HasValue && !epochService.TryGetNote(A, out _), "old epoch response cannot publish history");
    }

    private static async Task TargetChangeAndProgress()
    {
        var store = new Store(); var client = new Client();
        using var service = new AnimalHistoryService(store, client); service.Configure(Settings());
        var ui = new ConcurrentQueue<Action>(); int applied = 0; var notices = new List<HistoryBatchProgress>();
        using var batch = new AnimalHistoryCoordinator(service.LookupAsync, service.GetCachedNoteAsync,
            () => service.Epoch, ui.Enqueue, _ => applied++);
        batch.ProgressChanged += notices.Add;
        batch.UpdateTargets(new[] { A }); var old = await client.Next();
        batch.UpdateTargets(new[] { B });
        await old.Cancelled.Task.WaitAsync(Timeout);
        var latest = await client.Next();
        Program.Check(latest.Number == B, "new display target replaces cancelled old request");
        latest.Reply.SetResult(Success()); await HistoryTestUi.Complete(batch.Completion, ui);
        Flush(ui);
        Program.Check(applied == 1 && batch.TryGetNote(B, out _) && !batch.TryGetNote(A, out _), "only latest display generation commits");
        Program.Check(notices.All(p => p.Version == batch.Progress.Version), "delayed progress callbacks cannot report old generation");
        batch.UpdateTargets(Array.Empty<string>()); Flush(ui);
        Program.Check(batch.Progress.Stage == HistoryBatchStage.Empty && batch.Progress.Total == 0, "empty target state clears progress");
        batch.UpdateTargets(new[] { C }); var stopping = await client.Next();
        batch.Dispose(); await HistoryTestUi.Complete(batch.Completion, ui);
        await stopping.Cancelled.Task.WaitAsync(Timeout); int noticeCount = notices.Count; Flush(ui);
        Program.Check(notices.Count == noticeCount && applied == 1, "shutdown prevents both progress and final UI updates");

        var cachedUi = new ConcurrentQueue<Action>(); int cachedApplied = 0, maximumQueued = 0, cacheReads = 0;
        using var cachedBatch = new AnimalHistoryCoordinator((_, _, _) => throw new Exception("no HTTP expected"),
            _ => { Interlocked.Increment(ref cacheReads); return Task.FromResult(new HistoryLookupResult(true, "cached", HistoryLookupOutcome.Cache)); },
            () => "epoch", cachedUi.Enqueue, _ => cachedApplied++);
        cachedBatch.ProgressChanged += _ => { };
        cachedBatch.UpdateTargets(Enumerable.Range(1, 501).Select(i => i.ToString("D15")));
        await Until(() => !cachedUi.IsEmpty);
        Program.Check(!cachedBatch.Completion.IsCompleted && cachedApplied == 0, "slow UI keeps cache publication completion pending");
        var cachedTimer = Stopwatch.StartNew();
        while (!cachedBatch.Completion.IsCompleted)
        {
            maximumQueued = Math.Max(maximumQueued, cachedUi.Count);
            Flush(cachedUi);
            if (cachedTimer.Elapsed > TimeSpan.FromSeconds(15)) throw new TimeoutException("Cache UI completion.");
            await Task.Delay(1);
        }
        await cachedBatch.Completion; Flush(cachedUi);
        Program.Check(cachedBatch.Progress.Cached == 501 && cachedApplied == 501 && maximumQueued <= 8,
            "501 cache hits publish individually with a bounded UI backlog");
        Console.WriteLine($"History individual cache publication: items={cachedApplied}, peak_queued={maximumQueued}, elapsed_ms={cachedTimer.Elapsed.TotalMilliseconds:F1}; fake UI, no network or rendering.");
        Program.Check(cachedBatch.Progress.Stage == HistoryBatchStage.Complete, "all-cache batch completes without HTTP");
        cachedBatch.UpdateTargets(Enumerable.Range(1, 502).Select(i => i.ToString("D15")));
        await HistoryTestUi.Complete(cachedBatch.Completion, cachedUi);
        Program.Check(cacheReads == 502 && cachedApplied == 502 && cachedBatch.Progress.Total == 502,
            "registration after 501 cached animals reads and publishes only the one added animal");
    }

    private static async Task TargetChurnWithBlockedUi()
    {
        var ui = new ConcurrentQueue<Action>(); int reads = 0;
        var published = new List<HistoryItemUpdate>();
        using var coordinator = new AnimalHistoryCoordinator((_, _, _) => throw new Exception("No HTTP expected"),
            _ => { Interlocked.Increment(ref reads); return Task.FromResult(new HistoryLookupResult(true, "cached", HistoryLookupOutcome.Cache)); },
            () => "one", ui.Enqueue, published.Add);
        coordinator.ProgressChanged += _ => { };
        for (int i = 1; i <= 20; i++)
        {
            coordinator.UpdateTargets(new[] { i.ToString("D15") });
            await Until(() => Volatile.Read(ref reads) == i);
            if (i <= 5) await Until(() => ui.Count >= i + 1);
            Program.Check(ui.Count <= 6, "cancelled result callbacks retain their UI budget during target churn");
        }
        await HistoryTestUi.Complete(coordinator.Completion, ui);
        Program.Check(published.Count == 1 && published[0].Number == 20.ToString("D15"),
            "blocked UI discards retired animals and eventually applies only the latest target");
        coordinator.UpdateTargets(new[] { A });
        await Until(() => ui.Count > 0);
        var completion = coordinator.Completion;
        coordinator.Dispose();
        await completion.WaitAsync(Timeout);
        Flush(ui);
        Program.Check(published.Count == 1, "shutdown completes and ignores a still-queued single-item callback");
    }

    private static async Task StorageFailure()
    {
        var store = new Store { FailHistoryWrites = true }; var client = new Client();
        using var service = new AnimalHistoryService(store, client); service.Configure(Settings());
        var query = service.LookupAsync(A);
        (await client.Next()).Reply.SetResult(Success());
        var result = await query.WaitAsync(Timeout);
        Program.Check(result.HasValue && await store.GetCacheAsync("animal_history", A) == null,
            "a normal fetched result does not manufacture a successful SQLite record when saving fails");
        var stopped = await service.LookupAsync(B).WaitAsync(Timeout);
        Program.Check(stopped.Outcome == HistoryLookupOutcome.Stopped && client.Calls == 1,
            "cache-unavailable signal retains existing stop policy for later API requests");
    }

    private static async Task SyntheticComparison()
    {
        async Task<double> Measure(int concurrency)
        {
            var store = new Store();
            await store.SaveCacheAsync("animal_history", B, JsonConvert.SerializeObject(Success()), store.Epoch);
            var client = new TimedClient();
            using var service = new AnimalHistoryService(store, client); service.Configure(Settings(concurrency));
            var ui = new ConcurrentQueue<Action>(); var timer = Stopwatch.StartNew();
            using var batch = new AnimalHistoryCoordinator(service.LookupAsync, service.GetCachedNoteAsync,
                () => service.Epoch, ui.Enqueue, _ => { });
            batch.UpdateTargets(new[] { A, B, C, "000000000000004" });
            await HistoryTestUi.Complete(batch.Completion, ui); Flush(ui);
            Program.Check(batch.Progress.Processed == 4 && batch.Progress.Cached == 1 && batch.Progress.Notes == 4 && client.Calls == 3,
                "synthetic pipeline comparison preserves results and HTTP count");
            return timer.Elapsed.TotalMilliseconds;
        }
        double serial = await Measure(1), parallel = await Measure(2);
        Console.WriteLine($"History synthetic pipeline comparison: 1 cache hit + 3 fake 1500ms responses, 1000ms start interval; concurrency=1 {serial:F1}ms, concurrency=2 {parallel:F1}ms. Production remains 1; no live network or production speed claim.");
    }

    private static async Task OverlappingTargets()
    {
        var store = new Store(); var client = new Client();
        using var service = new AnimalHistoryService(store, client); service.Configure(Settings());
        var ui = new ConcurrentQueue<Action>(); int applied = 0;
        using var batch = new AnimalHistoryCoordinator(service.LookupAsync, service.GetCachedNoteAsync,
            () => service.Epoch, ui.Enqueue, _ => applied++);
        batch.UpdateTargets(new[] { A }); var shared = await client.Next();
        batch.UpdateTargets(new[] { A, B });
        // The active A request remains subscribed while a new generation prepares its cache.
        shared.Reply.SetResult(Success());
        var next = await client.Next();
        Program.Check(next.Number == B && !shared.Cancelled.Task.IsCompleted, "overlapping target reuses active request across batch generations");
        next.Reply.SetResult(Success()); await HistoryTestUi.Complete(batch.Completion, ui); Flush(ui);
        Program.Check(client.Calls == 2 && applied == 2 && batch.Progress.Processed == 2,
            "overlap reuse avoids duplicate HTTP and applies each animal exactly once");

        var failingUi = new ConcurrentQueue<Action>();
        using var failing = new AnimalHistoryCoordinator((_, _, _) => Task.FromResult(HistoryLookupResult.Missing),
            _ => Task.FromResult(new HistoryLookupResult(true, "cached", HistoryLookupOutcome.Cache)), () => "day", failingUi.Enqueue,
            _ => throw new InvalidOperationException("synthetic UI failure"));
        failing.UpdateTargets(new[] { A }); await HistoryTestUi.Complete(failing.Completion, failingUi); Flush(failingUi);
        Program.Check(failing.Progress.Stage == HistoryBatchStage.Failed, "UI failure is not falsely reported as completed");
    }

    private static void Flush(ConcurrentQueue<Action> ui) { while (ui.TryDequeue(out var action)) action(); }
    private sealed class TimedClient : IAnimalHistoryClient
    {
        public int Calls;
        public async Task<AnimalHistoryResult> FetchAnimalHistoryAsync(AnimalTraceSettings settings, string number, CancellationToken token)
        {
            Interlocked.Increment(ref Calls);
            await Task.Delay(1500, token);
            return Success();
        }
        public void Dispose() { }
    }
    private sealed class Request(string number)
    {
        public string Number = number;
        public double Started = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        public TaskCompletionSource<AnimalHistoryResult> Reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    private sealed class Client : IAnimalHistoryClient
    {
        private readonly ConcurrentQueue<Request> _requests = new(); private readonly SemaphoreSlim _ready = new(0);
        private int _active; public int Calls, MaximumActive;
        public async Task<AnimalHistoryResult> FetchAnimalHistoryAsync(AnimalTraceSettings settings, string number, CancellationToken token)
        {
            var request = new Request(number); Interlocked.Increment(ref Calls);
            int active = Interlocked.Increment(ref _active); MaximumActive = Math.Max(MaximumActive, active);
            _requests.Enqueue(request); _ready.Release();
            try { return await request.Reply.Task.WaitAsync(token); }
            catch (OperationCanceledException) { request.Cancelled.TrySetResult(); throw; }
            finally { Interlocked.Decrement(ref _active); }
        }
        public async Task<Request> Next()
        {
            if (!await _ready.WaitAsync(Timeout) || !_requests.TryDequeue(out var request)) throw new TimeoutException("Expected history request.");
            return request;
        }
        public void Dispose() { }
    }
    private sealed class Store : IDailyCacheStore
    {
        private readonly ConcurrentDictionary<string, string> _values = new();
        public readonly ConcurrentBag<string> Reads = new();
        public string Epoch { get; private set; } = "one";
        public event Action? DayChanged;
        public event Action? Unavailable;
        public bool FailHistoryWrites;
        public event Action? CleanupIncomplete { add { } remove { } }
        public Task<bool> EnsureTodayAsync() => Task.FromResult(true);
        public Task<string?> GetCacheAsync(string type, string key)
        { Reads.Add(type + ":" + key); return Task.FromResult(_values.TryGetValue(type + ":" + key, out var value) ? value : null); }
        public Task<bool> SaveCacheAsync(string type, string key, string json, string epoch)
        {
            if (epoch != Epoch) return Task.FromResult(false);
            if (type == "animal_history" && FailHistoryWrites) { Unavailable?.Invoke(); return Task.FromResult(false); }
            _values[type + ":" + key] = json; return Task.FromResult(true);
        }
        private JObject Budget => JObject.Parse(_values.Single(pair => pair.Key.StartsWith("control:")).Value);
        public int BudgetCount => Budget["Count"]!.Value<int>();
        public string BudgetStop => Budget["Stop"]!.Value<string>()!;
        public void Advance() { _values.Clear(); Epoch = "two"; DayChanged?.Invoke(); }
    }
}
