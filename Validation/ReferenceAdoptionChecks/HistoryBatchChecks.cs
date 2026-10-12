using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Application.AnimalHistory;
using CowAuctionSmall.Domain.AnimalHistory;
using CowAuctionSmall.Domain.Settings;
using Newtonsoft.Json;
using System.Collections.Concurrent;

internal static class HistoryBatchChecks
{
    private const string A = "000000000000001";
    private const string B = "000000000000002";

    internal static async Task Run()
    {
        await CheckServiceResults();
        using (var f = new Fixture())
        {
            f.Batch.UpdateTargets(new[] { A, B, A, "invalid", "???????????????" });
            var first = await f.Next();
            first.Result.SetResult((true, "FIRST"));
            await HistoryTestUi.Until(() => !f.Ui.IsEmpty);
            Program.Check(!f.Batch.TryGetNote(A, out _) && !f.Batch.Completion.IsCompleted,
                "one ready result waits only for its UI callback, not for the entire query");
            f.Flush();
            Program.Check(f.Note(A) == "FIRST" && !f.Batch.TryGetNote(B, out _) && f.Applied == 1,
                "first result is visible while the next animal has no result");
            var second = await f.Next();
            Program.Check(first.Number == A && second.Number == B && f.Calls == 2, "valid targets are deduplicated and use the single-item pipeline");
            for (int index = 0; index < 20; index++) f.Batch.UpdateTargets(new[] { B, A });
            second.Result.SetResult((true, "SECOND"));
            await f.Idle();
            Program.Check(f.Note(B) == "SECOND" && f.Applied == 2 && f.Calls == 2,
                "each completed animal publishes once with no final whole-list callback");
            Program.Check(f.Published.All(f.Batch.IsCurrent), "published item carries valid per-animal request identity");

            f.Batch.UpdateTargets(new[] { A, B }, refresh: true);
            var failed = await f.Next();
            for (int index = 0; index < 20; index++) f.Batch.UpdateTargets(new[] { B, A }, refresh: true);
            failed.Result.SetResult((false, ""));
            var empty = await f.Next();
            Program.Check(failed.Refresh && empty.Refresh && f.Calls == 4, "repeated clicks share one active manual refresh");
            empty.Result.SetResult((true, ""));
            await HistoryTestUi.Until(() => !f.Ui.IsEmpty);
            f.Batch.UpdateTargets(new[] { A, B }, refresh: true);
            Program.Check(f.Calls == 4 && f.Note(B) == "SECOND", "refresh stays active until pending UI publication completes");
            await f.Idle();
            Program.Check(f.Note(A) == "FIRST" && f.Note(B) == "", "failure retains the old note and empty success clears it");
            for (int index = 0; index < 20; index++) f.Batch.UpdateTargets(new[] { A, B });
            Program.Check(f.Calls == 4, "polling does not restart failed animals");
        }
        using (var f = new Fixture())
        {
            f.Batch.UpdateTargets(new[] { A });
            var first = await f.Next();
            f.Batch.UpdateTargets(new[] { A, B });
            first.Result.SetResult((true, "KEPT"));
            var second = await f.Next();
            Program.Check(second.Number == B && f.Calls == 2 && f.Note(A) == "KEPT",
                "adding an animal preserves and publishes the already running request");
            second.Result.SetResult((true, "ADDED")); await f.Idle();
            Program.Check(f.Applied == 2 && f.Note(B) == "ADDED", "registration adds only the new animal to lookup and display");
        }
        using (var f = new Fixture())
        {
            f.Batch.UpdateTargets(new[] { A });
            var automatic = await f.Next();
            for (int index = 0; index < 20; index++) f.Batch.UpdateTargets(new[] { A }, refresh: true);
            Program.Check(f.Calls == 1, "manual refresh shares an active automatic request");
            automatic.Result.SetResult((true, "AUTOMATIC"));
            var manual = await f.Next();
            Program.Check(manual.Refresh && f.Calls == 2, "automatic query is followed by the requested refresh semantics");
            manual.Result.SetResult((true, "MANUAL")); await f.Idle();
            Program.Check(f.Note(A) == "MANUAL", "manual result supersedes the earlier automatic result");
        }
        foreach (string change in new[] { "replace", "epoch", "reset", "dispose" })
        {
            using var f = new Fixture();
            f.Batch.UpdateTargets(new[] { A });
            (await f.Next()).Result.SetResult((true, "OLD"));
            await HistoryTestUi.Until(() => !f.Ui.IsEmpty);
            if (change == "replace") f.Batch.UpdateTargets(new[] { B });
            else if (change == "epoch") f.Epoch = "next";
            else if (change == "reset") f.Batch.Reset();
            else f.Batch.Dispose();
            f.Flush();
            Program.Check(f.Applied == 0 && !f.Batch.TryGetNote(A, out _), "queued single-item result rejected after " + change);
            f.Batch.Dispose(); await f.Idle();
        }
        using (var f = new Fixture())
        {
            f.Batch.UpdateTargets(new[] { A });
            var old = await f.Next();
            f.Batch.UpdateTargets(Array.Empty<string>());
            f.Batch.UpdateTargets(new[] { A });
            var latest = await f.Next();
            old.Result.SetResult((true, "OLD_A")); latest.Result.SetResult((true, "NEW_A"));
            await f.Idle();
            Program.Check(f.Note(A) == "NEW_A" && f.Applied == 1, "A-to-removed-to-A does not reuse stale request identity");
            var published = f.Published.Single(); f.Batch.UpdateTargets(Array.Empty<string>());
            Program.Check(!f.Batch.IsCurrent(published), "removed identity becomes invalid immediately");
        }
        using (var f = new Fixture())
        {
            f.Batch.UpdateTargets(new[] { A }); var pending = await f.Next();
            f.Batch.Dispose(); await f.Idle();
            pending.Result.SetResult((true, "LATE")); f.Flush(); f.Batch.UpdateTargets(new[] { B });
            Program.Check(f.Calls == 1 && f.Applied == 0, "dispose finishes without waiting for an uncooperative query");
        }
    }

    private sealed record Request(string Number, bool Refresh, TaskCompletionSource<(bool HasValue, string Note)> Result);

    private static async Task CheckServiceResults()
    {
        foreach (var status in new[] { HistoryStatus.Success, HistoryStatus.InvalidResponse })
        {
            var cache = new MemoryStore();
            using var service = new AnimalHistoryService(cache, new FakeClient(_ => Task.FromResult(new AnimalHistoryResult { Status = status })));
            service.Configure(new AnimalTraceSettings { Enabled = true, ServiceKey = "fixture" });
            var result = await service.GetAnimalHistoryNoteAsync(A);
            Program.Check(result.HasValue == (status == HistoryStatus.Success) && result.Note == "",
                "service tuple distinguishes successful empty history from failure: " + status);
        }
        var cachedStore = new MemoryStore();
        var previous = new AnimalHistoryResult { Status = HistoryStatus.Success, Entries = new()
        {
            new() { Date = "20260101", Type = "등록", Owner = "FIXTURE_A" },
            new() { Date = "20260102", Type = "양수", Owner = "FIXTURE_B" }
        } };
        await cachedStore.SaveCacheAsync("animal_history", A, JsonConvert.SerializeObject(previous), cachedStore.Epoch);
        using (var service = new AnimalHistoryService(cachedStore, new FakeClient(_ => Task.FromResult(new AnimalHistoryResult { Status = HistoryStatus.InvalidResponse }))))
        {
            service.Configure(new AnimalTraceSettings { Enabled = true, ServiceKey = "fixture" });
            var result = await service.GetAnimalHistoryNoteAsync(A, true);
            Program.Check(result.HasValue && result.Note.Contains("FIXTURE_B"), "service failure preserves valid cached history as fallback");
        }
        var changingStore = new MemoryStore();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<AnimalHistoryResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var service = new AnimalHistoryService(changingStore, new FakeClient(_ => { started.TrySetResult(); return response.Task; })))
        {
            service.Configure(new AnimalTraceSettings { Enabled = true, ServiceKey = "fixture" });
            var pending = service.GetAnimalHistoryNoteAsync(A);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            changingStore.Advance(); response.SetResult(previous);
            Program.Check(!(await pending.WaitAsync(TimeSpan.FromSeconds(5))).HasValue, "service tuple rejects response crossing cache epoch");
        }
    }

    private sealed class FakeClient(Func<string, Task<AnimalHistoryResult>> load) : IAnimalHistoryClient
    {
        public Task<AnimalHistoryResult> FetchAnimalHistoryAsync(AnimalTraceSettings settings, string number, CancellationToken cancellationToken) => load(number);
        public void Dispose() { }
    }

    private sealed class MemoryStore : IDailyCacheStore
    {
        private readonly Dictionary<string, string> _values = new();
        public string Epoch { get; private set; } = "epoch-one";
        public event Action? DayChanged;
        public event Action? Unavailable { add { } remove { } }
        public event Action? CleanupIncomplete { add { } remove { } }
        public Task<bool> EnsureTodayAsync() => Task.FromResult(true);
        public Task<string?> GetCacheAsync(string type, string key) => Task.FromResult(_values.TryGetValue(type + key, out var value) ? value : null);
        public Task<bool> SaveCacheAsync(string type, string key, string json, string epoch)
        {
            if (epoch != Epoch) return Task.FromResult(false);
            _values[type + key] = json; return Task.FromResult(true);
        }
        internal void Advance() { _values.Clear(); Epoch = "epoch-two"; DayChanged?.Invoke(); }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly ConcurrentQueue<Request> _requests = new();
        private readonly SemaphoreSlim _started = new(0);
        internal readonly ConcurrentQueue<Action> Ui = new();
        internal readonly AnimalHistoryCoordinator Batch;
        internal string Epoch = "day-one";
        internal int Calls;
        internal int Applied;
        internal readonly List<HistoryItemUpdate> Published = new();

        internal Fixture()
        {
            Batch = new AnimalHistoryCoordinator((number, refresh) =>
            {
                var result = new TaskCompletionSource<(bool HasValue, string Note)>(TaskCreationOptions.RunContinuationsAsynchronously);
                Interlocked.Increment(ref Calls);
                _requests.Enqueue(new Request(number, refresh, result)); _started.Release();
                return result.Task;
            }, () => Epoch, action => Ui.Enqueue(action), update => { Applied++; Published.Add(update); });
        }

        internal async Task<Request> Next()
        {
            return await HistoryTestUi.Await(NextCore(), Ui);
        }

        private async Task<Request> NextCore()
        {
            if (!await _started.WaitAsync(TimeSpan.FromSeconds(5))) throw new TimeoutException("History request was not started.");
            if (!_requests.TryDequeue(out var request)) throw new InvalidOperationException("Missing history request.");
            return request;
        }
        internal Task Idle() => HistoryTestUi.Complete(Batch.Completion, Ui);
        internal string Note(string number) => Batch.TryGetNote(number, out string note) ? note : throw new InvalidOperationException("History not committed.");
        internal void Flush() { while (Ui.TryDequeue(out var action)) action(); }
        public void Dispose() { Batch.Dispose(); }
    }
}
