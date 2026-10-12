using CowAuctionSmall.Infra.Logging;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

internal static class LogArchiveChecks
{
    internal static void Run()
    {
        string parent = Path.Combine(Path.GetTempPath(), "CowAuctionLogArchiveChecks");
        string root = Path.Combine(parent, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            CheckDates(Path.Combine(root, "dates"));
            CheckConflicts(Path.Combine(root, "conflicts"));
            CheckBusyAndCancellation(Path.Combine(root, "busy"));
            CheckWorker(Path.Combine(root, "worker"));
            CheckRetention(Path.Combine(root, "retention"));
            using var absent = new LogArchiveService(Path.Combine(root, "missing"));
            Program.Check(absent.ArchiveOnce() == new LogArchiveResult(0, 0, 0), "absent log directory is a no-op");
        }
        finally
        {
            // Cleanup is restricted to the unique synthetic test directory created above.
            string resolved = Path.GetFullPath(root);
            if (!resolved.StartsWith(Path.GetFullPath(parent) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unexpected test cleanup path.");
            foreach (string file in Directory.EnumerateFiles(resolved, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(resolved, recursive: true);
        }
    }

    private static readonly DateTime Today = new(2026, 10, 1, 12, 0, 0);

    private static string Write(string root, string relative, byte[]? bytes = null)
    {
        string path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes ?? Encoding.UTF8.GetBytes("가상 로그\r\nTEST|1||2|\r\n"));
        File.SetLastWriteTime(path, Today);
        return path;
    }

    private static byte[] ReadZip(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        Program.Check(zip.Entries.Count == 1, "archive contains only its own source log");
        using var data = zip.Entries[0].Open();
        using var output = new MemoryStream();
        data.CopyTo(output);
        return output.ToArray();
    }

    private static void CheckDates(string root)
    {
        byte[] bytes = Enumerable.Range(0, 300000).Select(index => (byte)(index % 251)).ToArray();
        string old = Write(root, "2026-09-30/2026-09-30.log", bytes);
        string today = Write(root, "2026-10-01/2026-10-01.log");
        string protectedByFolder = Write(root, "2026-10-01/2026-09-29.log");
        string protectedByName = Write(root, "2026-09-29/2026-10-01.log");
        string arbitraryOld = Write(root, "extra/session.log");
        File.SetLastWriteTime(arbitraryOld, Today.AddDays(-2));
        string arbitraryToday = Write(root, "extra/current.log");
        string future = Write(root, "2026-10-02/2026-10-02.LOG");
        string empty = Write(root, "2026-09-28/empty.log", []);
        string other = Write(root, "2026-09-30/keep.txt");
        using var service = new LogArchiveService(root, localNow: () => Today);
        var result = service.ArchiveOnce();
        Program.Check(result.Archived == 4 && result.Failed == 0 && result.Skipped == 4, "all non-today logs compressed; filename and folder protect today");
        Program.Check(!File.Exists(old) && ReadZip(old + ".zip").SequenceEqual(bytes), "verified ZIP preserves complete multi-buffer binary log before deleting source");
        Program.Check(File.Exists(today) && File.Exists(protectedByFolder) && File.Exists(protectedByName) && File.Exists(arbitraryToday), "today logs remain available as raw files");
        Program.Check(File.Exists(arbitraryOld + ".zip") && File.Exists(future + ".zip"), "undated names use last-write date and all non-today dates are eligible");
        Program.Check(File.GetLastWriteTime(arbitraryOld + ".zip").Date == Today.AddDays(-2).Date, "archive preserves source date for undated retention checks");
        Program.Check(ReadZip(empty + ".zip").Length == 0 && File.Exists(other), "empty log archives correctly while non-log files remain untouched");
        Program.Check(service.ArchiveOnce().Archived == 0, "repeated scan does not recompress ZIP files");
        Program.Check(!Directory.EnumerateFiles(root, "*.tmp", SearchOption.AllDirectories).Any(), "successful scan leaves no temporary files");
    }

    private static void CheckConflicts(string root)
    {
        string same = Write(root, "2026-09-30/same.log");
        byte[] bytes = File.ReadAllBytes(same);
        ZipFile.CreateFromDirectory(CreateSingleDirectory(root, "fixture", Path.GetFileName(same), bytes), same + ".zip");
        byte[] zipBefore = File.ReadAllBytes(same + ".zip");
        string corrupt = Write(root, "2026-09-30/corrupt.log");
        File.WriteAllText(corrupt + ".zip", "existing damaged archive must not be overwritten");
        string different = Write(root, "2026-09-30/different.log");
        CreateZip(different + ".zip", Path.GetFileName(different), Encoding.UTF8.GetBytes("different content"));
        byte[] differentZip = File.ReadAllBytes(different + ".zip");
        string blocked = Write(root, "2026-09-30/blocked.log");
        File.WriteAllText(blocked + ".zip", "existing archive");
        string fallback = blocked + "." + Convert.ToHexString(SHA256.HashData(bytes)) + ".zip";
        File.WriteAllText(fallback, "damaged fallback");
        using var service = new LogArchiveService(root, localNow: () => Today);
        var result = service.ArchiveOnce();
        Program.Check(result.Archived == 3 && result.Failed == 1, "matching archive recovers safely and conflicting archives retain existing bytes");
        Program.Check(!File.Exists(same) && File.ReadAllBytes(same + ".zip").SequenceEqual(zipBefore), "verified existing archive permits source cleanup without rewriting archive");
        string corruptNew = corrupt + "." + Convert.ToHexString(SHA256.HashData(bytes)) + ".zip";
        Program.Check(!File.Exists(corrupt) && ReadZip(corruptNew).SequenceEqual(bytes) && File.ReadAllText(corrupt + ".zip").StartsWith("existing damaged"), "damaged default archive kept while new verified archive is published separately");
        Program.Check(!File.Exists(different) && File.ReadAllBytes(different + ".zip").SequenceEqual(differentZip), "different valid ZIP never overwritten");
        Program.Check(File.Exists(blocked) && File.ReadAllText(fallback) == "damaged fallback", "failed final ZIP verification leaves source and existing archive intact");
    }

    private static string CreateSingleDirectory(string root, string name, string fileName, byte[] bytes)
    {
        string directory = Path.Combine(root, name);
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, fileName), bytes);
        File.SetLastWriteTime(Path.Combine(directory, fileName), Today);
        return directory;
    }

    private static void CreateZip(string path, string name, byte[] bytes)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        using var entry = archive.CreateEntry(name).Open();
        entry.Write(bytes);
    }

    private static void CheckBusyAndCancellation(string root)
    {
        string path = Write(root, "2026-09-30/busy.log");
        string readOnly = Write(root, "2026-09-30/read-only.log");
        File.SetAttributes(readOnly, FileAttributes.ReadOnly);
        using var service = new LogArchiveService(root, localNow: () => Today);
        using (var writer = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
        {
            var result = service.ArchiveOnce();
            Program.Check(result.Archived == 0 && result.Failed == 2 && File.Exists(path), "active writer and read-only log are retained for retry");
            writer.Position = writer.Length;
            writer.Write(Encoding.UTF8.GetBytes("APPENDED"));
        }
        File.SetAttributes(readOnly, FileAttributes.Normal);
        byte[] final = File.ReadAllBytes(path);
        Program.Check(service.ArchiveOnce().Archived == 2 && ReadZip(path + ".zip").SequenceEqual(final), "retry after writer closes includes its last append");
        string cancelled = Write(root, "2026-09-30/cancelled.log");
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        bool caught = false;
        try { service.ArchiveOnce(cancel.Token); }
        catch (OperationCanceledException) { caught = true; }
        Program.Check(caught && File.Exists(cancelled) && !File.Exists(cancelled + ".zip"), "cancelled scan retains source");

        // Cancel at a controlled checkpoint rather than relying on a large-file timing race.
        using var during = new CancellationTokenSource();
        using var cancellingService = new LogArchiveService(root, localNow: () =>
        {
            try { using var probe = new FileStream(cancelled, FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) { during.Cancel(); }
            return Today;
        });
        caught = false;
        try { cancellingService.ArchiveOnce(during.Token); }
        catch (OperationCanceledException) { caught = true; }
        Program.Check(caught && File.Exists(cancelled), "cancellation after locking source never deletes it");
        Program.Check(!Directory.EnumerateFiles(root, "*.tmp", SearchOption.AllDirectories).Any(), "cancellation leaves no owned temporary ZIP");
        using var afterPublish = new CancellationTokenSource();
        using var publishingService = new LogArchiveService(root, localNow: () =>
        {
            if (File.Exists(cancelled + ".zip")) afterPublish.Cancel();
            return Today;
        });
        caught = false;
        try { publishingService.ArchiveOnce(afterPublish.Token); }
        catch (OperationCanceledException) { caught = true; }
        Program.Check(caught && File.Exists(cancelled) && File.Exists(cancelled + ".zip"), "cancellation after ZIP publication retains original and recoverable archive");
        byte[] published = File.ReadAllBytes(cancelled + ".zip");
        Program.Check(service.ArchiveOnce().Archived == 1 && !File.Exists(cancelled) && File.ReadAllBytes(cancelled + ".zip").SequenceEqual(published), "next scan verifies and reuses archive left by interrupted cleanup");
    }

    private static void CheckRetention(string root)
    {
        DateTime now = new(2026, 10, 12, 12, 0, 0);
        string expired = Write(root, "2026-08-11/2026-08-11.log");
        string boundary = Write(root, "2026-08-12/2026-08-12.log");
        string recent = Write(root, "2026-08-13/2026-08-13.log");
        string expiredZip = Write(root, "2026-08-12/2026-08-12.log.zip");
        string fallback = Write(root, "2026-08-12/2026-08-12.log." + new string('A', 64) + ".zip");
        string recentZip = Write(root, "2026-08-13/2026-08-13.log.zip");
        CreateZip(recentZip + ".fixture", "ignored", []);
        File.Delete(recentZip);
        File.Move(recentZip + ".fixture", recentZip);
        string undated = Write(root, "extra/session.log.zip");
        File.SetLastWriteTime(undated, now.AddMonths(-2));
        string todayByFolder = Write(root, "2026-10-12/2026-07-01.log");
        string todayByName = Write(root, "2026-07-01/2026-10-12.log.zip");
        string other = Write(root, "2026-07-01/backup.zip");
        string text = Write(root, "2026-07-01/keep.txt");
        string temporary = Write(root, "2026-07-01/2026-07-01.log.zip.pending.tmp");
        string invalidHash = Write(root, "2026-07-01/2026-07-01.log.not-a-hash.zip");
        using var service = new LogArchiveService(root, localNow: () => now);
        var result = service.ArchiveOnce();
        Program.Check(result.Deleted == 5 && result.Failed == 0, "two-calendar-month cutoff deletes expired raw logs, ZIPs, hash ZIPs and undated old archives");
        Program.Check(!File.Exists(expired) && !File.Exists(boundary) && !File.Exists(expiredZip) && !File.Exists(fallback) && !File.Exists(undated), "expired files are removed including the exact two-month boundary");
        Program.Check(!File.Exists(expired + ".zip") && !File.Exists(boundary + ".zip"), "expired raw logs are deleted without creating pointless archives");
        Program.Check(File.Exists(recent + ".zip") && File.Exists(recentZip), "inside-retention logs are compressed and recent archives remain");
        Program.Check(File.Exists(todayByFolder) && File.Exists(todayByName), "newer filename or date folder protects against conflicting older dates");
        Program.Check(File.Exists(other) && File.Exists(text) && File.Exists(temporary) && File.Exists(invalidHash), "unrelated ZIPs, text, temporary files and non-service archive names remain");
        Program.Check(service.ArchiveOnce().Deleted == 0, "repeated retention scan does not delete retained archives");

        string busy = Write(root, "2026-08-01/busy.log.zip");
        string readOnly = Write(root, "2026-08-01/readonly.log");
        File.SetAttributes(readOnly, FileAttributes.ReadOnly);
        using (var writer = new FileStream(busy, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
        {
            result = service.ArchiveOnce();
            Program.Check(result.Deleted == 0 && result.Failed == 2 && File.Exists(busy) && File.Exists(readOnly), "busy and read-only expired files survive for retry");
        }
        File.SetAttributes(readOnly, FileAttributes.Normal);
        Program.Check(service.ArchiveOnce().Deleted == 2, "expired files are removed after their locks and read-only flag clear");

        string cancelled = Write(root, "2026-08-01/cancelled.log.zip");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        bool caught = false;
        try { service.ArchiveOnce(cancellation.Token); }
        catch (OperationCanceledException) { caught = true; }
        Program.Check(caught && File.Exists(cancelled), "cancelled retention scan preserves expired files");
        using var duringDelete = new CancellationTokenSource();
        using var cancelling = new LogArchiveService(root, localNow: () =>
        {
            try { using var probe = new FileStream(cancelled, FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) { duringDelete.Cancel(); }
            return now;
        });
        caught = false;
        try { cancelling.ArchiveOnce(duringDelete.Token); }
        catch (OperationCanceledException) { caught = true; }
        Program.Check(caught && File.Exists(cancelled), "cancellation after locking expired file prevents deletion");

        string leap = Write(root, "2024-02-29/leap.log.zip");
        using var leapService = new LogArchiveService(root, localNow: () => new DateTime(2024, 4, 30));
        Program.Check(leapService.ArchiveOnce().Deleted == 1 && !File.Exists(leap), "calendar-month retention handles leap-year month ends");
        string normal = Write(root, "2025-02-28/month-end.log.zip");
        File.SetLastWriteTime(normal, new DateTime(2025, 2, 28));
        using var monthEnd = new LogArchiveService(root, localNow: () => new DateTime(2025, 4, 30));
        Program.Check(monthEnd.ArchiveOnce().Deleted == 1 && !File.Exists(normal), "calendar-month retention handles non-leap month ends");
    }

    private static void CheckWorker(string root)
    {
        DateTime date = Today;
        string yesterday = Write(root, "2026-09-30/2026-09-30.log");
        string today = Write(root, "2026-10-01/2026-10-01.log");
        string expired = Write(root, "2026-07-31/2026-07-31.log.zip");
        using var completed = new ManualResetEventSlim();
        int deleted = 0;
        int caller = Environment.CurrentManagedThreadId, worker = caller;
        long ticks = date.Ticks;
        using var service = new LogArchiveService(root, result =>
        {
            worker = Environment.CurrentManagedThreadId;
            Interlocked.Add(ref deleted, result.Deleted);
            if (result.Archived > 0) completed.Set();
        }, () => new DateTime(Interlocked.Read(ref ticks)), TimeSpan.FromMilliseconds(30));
        service.Start(); service.Start();
        Program.Check(completed.Wait(TimeSpan.FromSeconds(5)) && !File.Exists(yesterday) && worker != caller, "startup archive runs on a separate worker; repeated Start is harmless");
        Program.Check(File.Exists(today), "worker preserves today while app remains running");
        Program.Check(deleted == 1 && !File.Exists(expired), "startup worker reports and removes expired archives");
        completed.Reset();
        Interlocked.Exchange(ref ticks, Today.AddDays(1).Ticks);
        Program.Check(completed.Wait(TimeSpan.FromSeconds(5)) && !File.Exists(today), "periodic scan archives yesterday after local date rollover");
        Program.Check(service.Stop(TimeSpan.FromSeconds(1)), "archive worker stops within bounded shutdown wait");
        Program.Check(service.Stop(TimeSpan.Zero), "repeated Stop is safe");
    }
}
