using CowAuctionSmall.Application.Abstractions;
using Microsoft.Data.Sqlite;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CowAuctionSmall.Infra.Persistence
{
    public sealed class DailyCacheService : IDisposable, IDailyCacheStore
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly Func<DateTimeOffset> _clock;
        private readonly string _path;
        private readonly bool _cleanOtherInstallations;
        private string _cleanupDate = "";
        private SqliteConnection? _connection;
        private FileStream? _ownership;
        private readonly Timer _timer;
        private string _date = "";
        private long _generation;
        private bool _failed;
        private int _disposed;
        public event Action? DayChanged;
        public event Action? Unavailable;
        public event Action? CleanupIncomplete;
        public string Today => TimeZoneInfo.ConvertTime(_clock(), TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time")).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public string Epoch => Today + ":" + Interlocked.Read(ref _generation);

        public DailyCacheService(string? path = null, Func<DateTimeOffset>? clock = null)
        {
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
            _cleanOtherInstallations = path == null;
            string scope = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(AppContext.BaseDirectory.ToUpperInvariant()))).Substring(0, 16);
            _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CowAuctionSmall", "Cache", scope, "auction-cache.db");
            _timer = new Timer(_ => { _ = EnsureTodayAsync(); }, null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
        }

        private void EnsureCore()
        {
            if (_connection == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
                _ownership = new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                _connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _path, Pooling = false, DefaultTimeout = 3 }.ToString());
                _connection.Open();
                using var setup = _connection.CreateCommand();
                setup.CommandText = "PRAGMA journal_mode=DELETE; PRAGMA secure_delete=ON; PRAGMA busy_timeout=3000;";
                setup.ExecuteNonQuery();
                using var version = _connection.CreateCommand();
                version.CommandText = "PRAGMA user_version;";
                long currentVersion = (long)version.ExecuteScalar()!;
                if (currentVersion != 0 && currentVersion != 1) throw new InvalidDataException();
                setup.CommandText = "CREATE TABLE IF NOT EXISTS daily_cache (cache_type TEXT NOT NULL CHECK(cache_type IN ('auction','animal_history','control')), cache_key TEXT NOT NULL, cache_date TEXT NOT NULL CHECK(length(cache_date)=10), payload_json TEXT NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, PRIMARY KEY(cache_type,cache_key)); PRAGMA user_version=1;";
                setup.ExecuteNonQuery();
            }
            string today = Today;
            using var check = _connection.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM daily_cache WHERE cache_date <> $date;";
            check.Parameters.AddWithValue("$date", today);
            bool expired = (long)check.ExecuteScalar()! > 0;
            if (expired || (_date.Length > 0 && _date != today))
            {
                using var transaction = _connection.BeginTransaction();
                using var delete = _connection.CreateCommand();
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM daily_cache;";
                delete.ExecuteNonQuery();
                transaction.Commit();
                Interlocked.Increment(ref _generation);
                _date = today;
                DayChanged?.Invoke();
            }
            _date = today;
            if (_cleanOtherInstallations && _cleanupDate != today)
            {
                _cleanupDate = today;
                CleanOtherInstallations(today);
            }
        }

        private void CleanOtherInstallations(string today)
        {
            string root = Path.GetDirectoryName(Path.GetDirectoryName(_path))!;
            try
            {
                foreach (string directory in Directory.EnumerateDirectories(root))
                {
                    string name = Path.GetFileName(directory);
                    if (name.Length != 16 || !System.Linq.Enumerable.All(name, Uri.IsHexDigit) ||
                        (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) continue;
                    string path = Path.Combine(directory, "auction-cache.db");
                    if (string.Equals(path, _path, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) continue;
                    try
                    {
                        using var ownership = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false, Mode = SqliteOpenMode.ReadWrite, DefaultTimeout = 1 }.ToString());
                        connection.Open();
                        using var command = connection.CreateCommand();
                        command.CommandText = "PRAGMA user_version;";
                        if ((long)command.ExecuteScalar()! != 1) { CleanupIncomplete?.Invoke(); continue; }
                        command.CommandText = "PRAGMA secure_delete=ON; PRAGMA journal_mode=DELETE;";
                        command.ExecuteNonQuery();
                        command.CommandText = "SELECT COUNT(*) FROM daily_cache WHERE cache_date <> $date;";
                        command.Parameters.AddWithValue("$date", today);
                        if ((long)command.ExecuteScalar()! > 0)
                        {
                            command.CommandText = "DELETE FROM daily_cache;";
                            command.ExecuteNonQuery();
                        }
                    }
                    catch (Exception) { CleanupIncomplete?.Invoke(); }
                }
            }
            catch (Exception) { CleanupIncomplete?.Invoke(); }
        }

        private Task<T> RunAsync<T>(Func<T> operation, T fallback)
        {
            return Task.Run(async () =>
            {
                await _gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_failed || Volatile.Read(ref _disposed) != 0) return fallback;
                    EnsureCore();
                    return operation();
                }
                catch (Exception)
                {
                    _failed = true;
                    _connection?.Dispose(); _connection = null;
                    _ownership?.Dispose(); _ownership = null;
                    Unavailable?.Invoke();
                    return fallback;
                }
                finally { _gate.Release(); }
            });
        }

        public Task<bool> EnsureTodayAsync() => RunAsync(() => true, false);

        public Task<string?> GetCacheAsync(string type, string key) => RunAsync(() =>
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = "SELECT payload_json FROM daily_cache WHERE cache_type=$type AND cache_key=$key AND cache_date=$date;";
            command.Parameters.AddWithValue("$type", type); command.Parameters.AddWithValue("$key", key); command.Parameters.AddWithValue("$date", _date);
            return command.ExecuteScalar() as string;
        }, (string?)null);

        public Task<bool> SaveCacheAsync(string type, string key, string json, string epoch) => RunAsync(() =>
        {
            if (epoch != Epoch) return false;
            using var command = _connection!.CreateCommand();
            command.CommandText = "INSERT INTO daily_cache VALUES($type,$key,$date,$json,$now,$now) ON CONFLICT(cache_type,cache_key) DO UPDATE SET cache_date=$date,payload_json=$json,updated_at=$now;";
            command.Parameters.AddWithValue("$type", type); command.Parameters.AddWithValue("$key", key);
            command.Parameters.AddWithValue("$date", _date); command.Parameters.AddWithValue("$json", json);
            command.Parameters.AddWithValue("$now", _clock().ToUniversalTime().ToString("O"));
            command.ExecuteNonQuery(); return true;
        }, false);

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _timer.Dispose();
            _gate.Wait();
            try { _connection?.Dispose(); _ownership?.Dispose(); }
            finally { _gate.Release(); }
        }
    }
}
