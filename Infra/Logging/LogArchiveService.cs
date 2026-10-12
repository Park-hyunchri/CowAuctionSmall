using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

namespace CowAuctionSmall.Infra.Logging
{
    public sealed record LogArchiveResult(int Archived, int Skipped, int Failed, int Deleted = 0);

    /// <summary>오늘 로그는 유지하고, 검증된 ZIP이 확보된 이전 로그만 정리한다.</summary>
    public sealed class LogArchiveService : IDisposable
    {
        private readonly string _root;
        private readonly Func<DateTime> _now;
        private readonly Action<LogArchiveResult>? _report;
        private readonly TimeSpan _interval;
        private readonly CancellationTokenSource _stop = new();
        private readonly object _lifetimeGate = new();
        private readonly object _scanGate = new();
        private Thread? _worker;
        private bool _stopping;
        private bool _disposed;

        public LogArchiveService(string logRoot, Action<LogArchiveResult>? report = null,
            Func<DateTime>? localNow = null, TimeSpan? interval = null)
        {
            _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(logRoot));
            _now = localNow ?? (() => DateTime.Now);
            _report = report;
            _interval = interval ?? TimeSpan.FromHours(1);
            if (_interval <= TimeSpan.Zero || _interval.TotalMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(interval));
        }

        public void Start()
        {
            lock (_lifetimeGate)
            {
                if (_stopping) throw new ObjectDisposedException(nameof(LogArchiveService));
                if (_worker != null) return;
                _worker = new Thread(Run)
                {
                    IsBackground = true, Name = "Auction log archive", Priority = ThreadPriority.BelowNormal
                };
                _worker.Start();
            }
        }

        private void Run()
        {
            while (!_stop.IsCancellationRequested)
            {
                try
                {
                    var result = ArchiveOnce(_stop.Token);
                    if (result.Archived > 0 || result.Failed > 0 || result.Deleted > 0) Report(result);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception) { Report(new LogArchiveResult(0, 0, 1)); }
                if (_stop.Token.WaitHandle.WaitOne(_interval)) break;
            }
        }

        private void Report(LogArchiveResult result)
        {
            try { _report?.Invoke(result); }
            catch { /* 진단 출력 실패가 백그라운드 작업자를 종료하지 않게 한다. */ }
        }

        // 파일 I/O를 수행하므로 앱에서는 Start의 전용 작업자를 통해 호출한다.
        public LogArchiveResult ArchiveOnce(CancellationToken cancellationToken = default)
        {
            lock (_scanGate)
            {
                int archived = 0, skipped = 0, failed = 0, deleted = 0;
                var pending = new Stack<string>();
                pending.Push(_root);
                while (pending.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string directory = pending.Pop();
                    try
                    {
                        if (!Directory.Exists(directory)) continue;
                        if (!IsSafePath(directory)) { skipped++; continue; }
                        foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos())
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) { skipped++; continue; }
                            if (entry is DirectoryInfo) { pending.Push(entry.FullName); continue; }
                            bool isLog = string.Equals(entry.Extension, ".log", StringComparison.OrdinalIgnoreCase);
                            if (!isLog && !IsLogArchive(entry.Name)) continue;
                            try
                            {
                                var file = (FileInfo)entry;
                                if (IsExpired(file))
                                {
                                    if (DeleteExpired(file, cancellationToken)) deleted++;
                                    else skipped++;
                                }
                                else if (!isLog) continue;
                                else if (ArchiveFile(file, cancellationToken)) archived++;
                                else skipped++;
                            }
                            catch (Exception error) when (IsFileError(error)) { failed++; }
                        }
                    }
                    catch (Exception error) when (IsFileError(error)) { failed++; }
                }
                return new LogArchiveResult(archived, skipped, failed, deleted);
            }
        }

        private static bool IsLogArchive(string name)
        {
            if (name.EndsWith(".log.zip", StringComparison.OrdinalIgnoreCase)) return true;
            if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return false;
            string stem = name[..^4];
            int marker = stem.LastIndexOf(".log.", StringComparison.OrdinalIgnoreCase);
            if (marker < 0 || stem.Length - marker - 5 != 64) return false;
            for (int index = marker + 5; index < stem.Length; index++)
                if (!Uri.IsHexDigit(stem[index])) return false;
            return true;
        }

        private bool IsExpired(FileInfo file)
        {
            DateTime cutoff = _now().Date.AddMonths(-2);
            DateTime? latest = null;
            if (file.Name.Length >= 10 && TryDate(file.Name[..10], out var fileDate)) latest = fileDate;
            for (var parent = file.Directory; parent != null && !string.Equals(parent.FullName, _root, StringComparison.OrdinalIgnoreCase); parent = parent.Parent)
                if (TryDate(parent.Name, out var date) && (!latest.HasValue || date > latest.Value)) latest = date;
            return (latest ?? file.LastWriteTime.Date) <= cutoff;
        }

        private bool DeleteExpired(FileInfo file, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!IsSafePath(file.FullName)) return false;
            using var handle = OpenExclusive(file.FullName);
            file.Refresh();
            if (!IsSafePath(file.FullName) || !IsExpired(file)) return false;
            token.ThrowIfCancellationRequested();
            var disposition = new FileDispositionInfo { DeleteFile = 1 };
            if (!SetFileInformationByHandle(handle, 4, ref disposition, (uint)Marshal.SizeOf<FileDispositionInfo>()))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return true;
        }

        private bool ArchiveFile(FileInfo file, CancellationToken token)
        {
            if (!IsSafePath(file.FullName) || IsToday(file)) return false;
            // Exclusive access prevents copying a file that another process is still writing.
            using var handle = OpenExclusive(file.FullName);
            using var source = new FileStream(handle, FileAccess.Read, 65536);
            file.Refresh();
            if (IsToday(file)) return false;
            byte[] hash = CopyAndHash(source, null, token);
            long length = source.Length;
            string archivePath = file.FullName + ".zip";
            if (File.Exists(archivePath) && !MatchesFile(archivePath, file.Name, length, hash, token))
                archivePath = file.FullName + "." + Convert.ToHexString(hash) + ".zip";

            if (!File.Exists(archivePath))
            {
                string temporary = archivePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                    {
                        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
                        {
                            var entry = zip.CreateEntry(file.Name, CompressionLevel.Optimal);
                            // ZIP timestamps cannot represent years before 1980 or after 2107.
                            if (file.LastWriteTime.Year is >= 1980 and <= 2107) entry.LastWriteTime = file.LastWriteTime;
                            using var destination = entry.Open();
                            source.Position = 0;
                            CopyAndHash(source, destination, token);
                        }
                        output.Flush(flushToDisk: true);
                    }
                    File.SetLastWriteTime(temporary, file.LastWriteTime);
                    token.ThrowIfCancellationRequested();
                    File.Move(temporary, archivePath, overwrite: false);
                }
                finally
                {
                    // Only this invocation's own temporary ZIP is eligible for cleanup.
                    try { File.Delete(temporary); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }

            // Keep the verified archive open against replacement until the source is marked for deletion.
            if (!IsSafePath(archivePath)) return false;
            using var archive = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (!Matches(archive, file.Name, length, hash, token))
                throw new InvalidDataException("Log archive verification failed.");
            token.ThrowIfCancellationRequested();
            file.Refresh();
            if (IsToday(file)) return false;
            // Delete by the same locked handle: no close/reopen race with a newly created log.
            token.ThrowIfCancellationRequested();
            var disposition = new FileDispositionInfo { DeleteFile = 1 };
            if (!SetFileInformationByHandle(handle, 4, ref disposition, (uint)Marshal.SizeOf<FileDispositionInfo>()))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return true;
        }

        private bool IsToday(FileInfo file)
        {
            DateTime today = _now().Date;
            bool dated = false;
            string name = Path.GetFileNameWithoutExtension(file.Name);
            if (name.Length >= 10 && TryDate(name[..10], out var fileDate))
            {
                dated = true;
                if (fileDate == today) return true;
            }
            for (var parent = file.Directory; parent != null && !string.Equals(parent.FullName, _root, StringComparison.OrdinalIgnoreCase); parent = parent.Parent)
            {
                if (!TryDate(parent.Name, out var date)) continue;
                dated = true;
                if (date == today) return true;
            }
            return !dated && file.LastWriteTime.Date == today;
        }

        private static bool TryDate(string text, out DateTime date) =>
            DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

        private bool IsSafePath(string path)
        {
            string full = Path.GetFullPath(path);
            if (!string.Equals(full, _root, StringComparison.OrdinalIgnoreCase)
                && !full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return false;
            for (string? current = full; current != null; current = Path.GetDirectoryName(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
                if (string.Equals(current, _root, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static bool MatchesFile(string path, string name, long length, byte[] hash, CancellationToken token)
        {
            try
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) return false;
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return Matches(stream, name, length, hash, token);
            }
            catch (Exception error) when (IsFileError(error)) { return false; }
        }

        private static bool Matches(Stream stream, string name, long length, byte[] hash, CancellationToken token)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            if (zip.Entries.Count != 1 || zip.Entries[0].FullName != name || zip.Entries[0].Length != length) return false;
            using var data = zip.Entries[0].Open();
            return CryptographicOperations.FixedTimeEquals(CopyAndHash(data, null, token), hash);
        }

        private static byte[] CopyAndHash(Stream source, Stream? destination, CancellationToken token)
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            byte[] buffer = new byte[65536];
            int read;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                read = source.Read(buffer, 0, buffer.Length);
                if (read == 0) break;
                destination?.Write(buffer, 0, read);
                hash.AppendData(buffer, 0, read);
            }
            return hash.GetHashAndReset();
        }

        private static bool IsFileError(Exception error) =>
            error is IOException or UnauthorizedAccessException or Win32Exception or InvalidDataException;

        private static SafeFileHandle OpenExclusive(string path)
        {
            // GENERIC_READ | DELETE, no sharing, OPEN_EXISTING, sequential scan.
            var handle = CreateFile(path, 0x80010000, 0, IntPtr.Zero, 3, 0x08000000, IntPtr.Zero);
            if (!handle.IsInvalid) return handle;
            int error = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new Win32Exception(error);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FileDispositionInfo
        {
            public byte DeleteFile;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string fileName, uint desiredAccess, uint shareMode,
            IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetFileInformationByHandle(SafeFileHandle file, int informationClass,
            ref FileDispositionInfo information, uint bufferSize);

        public bool Stop(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));
            Thread? worker;
            lock (_lifetimeGate)
            {
                if (_disposed) return true;
                _stopping = true;
                _stop.Cancel();
                worker = _worker;
            }
            bool stopped = worker == null || !worker.IsAlive || (worker != Thread.CurrentThread && worker.Join(timeout));
            if (stopped)
            {
                lock (_lifetimeGate)
                {
                    if (!_disposed)
                    {
                        _stop.Dispose();
                        _disposed = true;
                    }
                }
            }
            return stopped;
        }

        public void Dispose() => Stop(TimeSpan.FromSeconds(1));
    }
}
