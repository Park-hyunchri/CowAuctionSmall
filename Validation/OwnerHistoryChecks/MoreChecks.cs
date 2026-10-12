using CowAuctionSmall.Infra.Persistence;
using CowAuctionSmall.Application.AnimalHistory;
using CowAuctionSmall.Infra.Api;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.AnimalHistory;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.Data.Sqlite;

static class MoreChecks
{
    private static int _checks;
    private static void Check(bool condition, string name) { if (!condition) throw new Exception(name); _checks++; }
    public static async Task Run()
    {
        string dir = Path.Combine(Path.GetTempPath(), "HistoryMore_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var now = new DateTimeOffset(2026, 9, 15, 2, 0, 0, TimeSpan.Zero);
        using (var cache = new DailyCacheService(Path.Combine(dir, "snapshot.db"), () => now))
        {
            await cache.EnsureTodayAsync();
            var snapshots = new AuctionSnapshotCache(cache);
            var user = new UserInfo { Authentication = new Authentication { UserID = "fake", Address = "https://example.test" }, Auction = new Auction { AuctionHouseCode = "111" } };
            var cow = new gValues { CowDistinction = "5", Note = "original", NoteChangeOwner = "DO_NOT_PERSIST_HISTORY", TraceNumber = "000000000000001", IsRunning = true, ProcessStatus = 8002 };
            Check(await snapshots.SaveAsync(user, "20260101", new[] { cow }, cache.Epoch), "save auction");
            var loaded = await snapshots.ReadAsync(user, "20260101");
            Check(loaded?.Count == 1 && loaded[0].CowDistinction == "5", "snapshot species");
            Check(loaded![0].Note == "original" && loaded[0].NoteChangeOwner == "", "snapshot allowlist");
            Check(!loaded[0].IsRunning && loaded[0].ProcessStatus == 8001, "no running state resumed");
            Check(!(await cache.GetCacheAsync("auction", AuctionSnapshotCache.Key(user, "20260101")))!.Contains("DO_NOT_PERSIST_HISTORY"), "history not duplicated");
            string snapshotJson = (await cache.GetCacheAsync("auction", AuctionSnapshotCache.Key(user, "20260101")))!;
            Check(!snapshotJson.Contains("EntityNumberShort"), "new cache does not persist redundant short number");
            var legacy = Newtonsoft.Json.Linq.JObject.Parse(snapshotJson);
            var legacyItem = (Newtonsoft.Json.Linq.JObject)legacy["items"]![0]!;
            legacyItem["EntityNumber"] = "1234 5678 9";
            legacyItem["EntityNumberShort"] = "OLD_DERIVED_VALUE";
            Check(await cache.SaveCacheAsync("auction", AuctionSnapshotCache.Key(user, "20260101"), legacy.ToString(), cache.Epoch), "save synthetic legacy snapshot");
            var legacyLoaded = await snapshots.ReadAsync(user, "20260101");
            Check(legacyLoaded?.Count == 1 && legacyLoaded[0].EntityNumber == "1234 5678 9", "old cache remains readable with unused short field");
            user.Auction.AuctionHouseCode = "222";
            Check(await snapshots.ReadAsync(user, "20260101") == null, "house isolation");
            user.Auction.AuctionHouseCode = "111"; user.Authentication.UserID = "other";
            Check(await snapshots.ReadAsync(user, "20260101") == null, "account isolation");
            user.Authentication.UserID = "fake";
            Check(await snapshots.ReadAsync(user, "20260102") == null, "auction date isolation");
            await snapshots.SaveAsync(user, "20260101", Array.Empty<gValues>(), cache.Epoch);
            Check((await snapshots.ReadAsync(user, "20260101"))?.Count == 0, "normal empty snapshot");
            string epoch = cache.Epoch; now = now.AddDays(1);
            Check(!await snapshots.SaveAsync(user, "20260101", new[] { cow }, epoch), "old auction response blocked");
        }
        string corruptPath = Path.Combine(dir, "corrupt.db"); File.WriteAllText(corruptPath, "not a database");
        using (var cache = new DailyCacheService(corruptPath)) Check(!await cache.EnsureTodayAsync(), "corrupt database fails closed");
        var settings = new AnimalTraceSettings { Enabled = true, Address = "https://example.test/trace", DailyLimit = 5, ServiceKey = "fake" };
        foreach (string code in new[] { "30", "22" })
        {
            using var cache = new DailyCacheService(Path.Combine(dir, code + ".db"));
            var handler = new ControlledHandler("<response><resultCode>" + code + "</resultCode></response>");
            using (var history = new AnimalHistoryService(cache, new AnimalHistoryClient(new HttpClient(handler))))
            {
                history.Configure(settings);
                await history.GetAnimalHistoryAsync("000000000000001");
                await history.GetAnimalHistoryAsync("000000000000002");
                Check(handler.Calls == 1, "API stop " + code);
                Check(await cache.GetCacheAsync("animal_history", "000000000000001") == null, "error not empty cache " + code);
            }
            var restarted = new ControlledHandler("unused");
            using (var history = new AnimalHistoryService(cache, new AnimalHistoryClient(new HttpClient(restarted))))
            { history.Configure(settings); await history.GetAnimalHistoryAsync("000000000000003"); Check(restarted.Calls == 0, "persisted stop " + code); }
        }
        using (var cache = new DailyCacheService(Path.Combine(dir, "late.db"), () => now))
        {
            var handler = new ControlledHandler("<response><resultCode>00</resultCode><items/></response>", true);
            using var history = new AnimalHistoryService(cache, new AnimalHistoryClient(new HttpClient(handler)));
            history.Configure(settings);
            var pending = history.GetAnimalHistoryAsync("000000000000001");
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            now = now.AddDays(1); await cache.EnsureTodayAsync(); handler.Release.TrySetResult(true);
            await pending;
            Check(await cache.GetCacheAsync("animal_history", "000000000000001") == null, "late history not saved");
            Check(!history.TryGetNote("000000000000001", out _), "late history not displayed");
            await history.GetAnimalHistoryAsync("000000000000001");
            Check(handler.Calls == 2 && history.TryGetNote("000000000000001", out _), "new day can fetch");
        }
        string installationRoot = Path.Combine(dir, "installations");
        string oldPath = Path.Combine(installationRoot, "AAAAAAAAAAAAAAAA", "auction-cache.db");
        using (var old = new DailyCacheService(oldPath, () => now.AddDays(-1)))
        { await old.EnsureTodayAsync(); await old.SaveCacheAsync("control", "old", "{}", old.Epoch); }
        using (var current = new DailyCacheService(Path.Combine(installationRoot, "BBBBBBBBBBBBBBBB", "auction-cache.db"), () => now))
        {
            await current.EnsureTodayAsync();
            typeof(DailyCacheService).GetMethod("CleanOtherInstallations", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(current, new object[] { current.Today });
            using var connection = new SqliteConnection("Data Source=" + oldPath + ";Pooling=False"); connection.Open();
            using var command = connection.CreateCommand(); command.CommandText = "SELECT COUNT(*) FROM daily_cache;";
            Check((long)command.ExecuteScalar()! == 0, "other installation old cache purged");
        }
        using (var old = new DailyCacheService(oldPath, () => now.AddDays(-1)))
        using (var current = new DailyCacheService(Path.Combine(installationRoot, "BBBBBBBBBBBBBBBB", "auction-cache.db"), () => now))
        {
            await old.EnsureTodayAsync(); await old.SaveCacheAsync("control", "locked", "{}", old.Epoch); await current.EnsureTodayAsync();
            int notices = 0; current.CleanupIncomplete += () => notices++;
            typeof(DailyCacheService).GetMethod("CleanOtherInstallations", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(current, new object[] { current.Today });
            Check(notices == 1 && await old.GetCacheAsync("control", "locked") == "{}", "active installation not modified");
        }
        Console.WriteLine($"PASS {_checks} additional cache/error/race checks");
    }
    private sealed class ControlledHandler : HttpMessageHandler
    {
        private readonly string _body;
        private readonly bool _wait;
        public int Calls;
        public TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ControlledHandler(string body, bool wait = false) { _body = body; _wait = wait; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Calls); Started.TrySetResult(true);
            if (_wait) await Release.Task.WaitAsync(token);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_body) };
        }
    }
}
