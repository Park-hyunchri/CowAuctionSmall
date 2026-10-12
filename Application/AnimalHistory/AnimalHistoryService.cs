using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Domain.AnimalHistory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CowAuctionSmall.Application.AnimalHistory
{
    public sealed class AnimalHistoryService : IDisposable
    {
        private sealed class Budget { public int Count { get; set; } public string Stop { get; set; } = ""; }
        private readonly IDailyCacheStore _cache;
        private readonly IAnimalHistoryClient _client;
        private readonly SemaphoreSlim _admission = new SemaphoreSlim(1, 1);
        private int _activeRequests;
        private TaskCompletionSource _requestFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentDictionary<string, byte> _blockedEpochs = new();
        private readonly ConcurrentDictionary<string, HistoryLookupOutcome> _outcomes = new();
        private volatile bool _cacheUnavailable;
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private sealed class PendingLookup
        {
            public required CancellationTokenSource Stop { get; init; }
            public required Task<string> Task { get; init; }
            public int Subscribers;
        }
        private readonly object _pendingGate = new();
        private readonly System.Collections.Generic.Dictionary<string, PendingLookup> _pending = new();
        private readonly ConcurrentDictionary<string, HistoryLookupResult> _history = new();
        private readonly ConcurrentDictionary<string, DateTime> _retryAfter = new ConcurrentDictionary<string, DateTime>();
        private DateTime _nextRequest;
        private string _lastNotice = "";
        private AnimalTraceSettings _settings = new AnimalTraceSettings();
        public event Action<string>? Status;
        public event Action? DayChanged;

        public AnimalHistoryService(IDailyCacheStore cache, IAnimalHistoryClient client)
        {
            _cache = cache; _client = client;
            _cache.DayChanged += OnDayChanged;
            _cache.Unavailable += OnUnavailable;
            _cache.CleanupIncomplete += OnCleanupIncomplete;
        }
        private void OnUnavailable() { _cacheUnavailable = true; Notice("이력 캐시를 사용할 수 없어 신규 자동 조회를 중단했습니다."); }
        private void OnCleanupIncomplete() => Notice("다른 실행본의 캐시를 정리하지 못했습니다. 사용 중인 실행본 또는 파일 접근 상태를 확인해 주세요.");
        private void OnDayChanged()
        {
            _history.Clear(); _retryAfter.Clear(); _outcomes.Clear(); _lastNotice = "";
            DayChanged?.Invoke();
        }
        private void Notice(string text) { if (Interlocked.Exchange(ref _lastNotice, text) != text) Status?.Invoke(text); }
        public void Configure(AnimalTraceSettings settings) { _settings = settings; _ = InitializeAsync(); }
        private async Task InitializeAsync()
        {
            if (!await _cache.EnsureTodayAsync()) OnUnavailable();
        }
        public static bool IsValidNumber(string number) => number != null && number.Length == 15 && number.All(c => c >= '0' && c <= '9');
        public int MaxConcurrentRequests => Math.Clamp(_settings.MaxConcurrentRequests, 1, 4);
        public bool TryGetNote(string number, out string note)
        {
            bool found = TryGetHistory(number, out var history);
            note = history.Note;
            return found;
        }
        private bool TryGetHistory(string number, out HistoryLookupResult history)
        {
            history = HistoryLookupResult.Missing;
            if (!_settings.Enabled || !_history.TryGetValue(_cache.Epoch + ":" + number, out var cached)) return false;
            history = cached;
            return true;
        }

        public string Epoch => _cache.Epoch;

        // Cache preparation never waits for an unrelated HTTP response.
        public async Task<HistoryLookupResult> GetCachedNoteAsync(string number)
        {
            if (!_settings.Enabled || !IsValidNumber(number) || _stop.IsCancellationRequested) return HistoryLookupResult.Missing;
            if (!await _cache.EnsureTodayAsync().ConfigureAwait(false)) OnUnavailable();
            string epoch = Epoch;
            if (TryGetHistory(number, out var cached)) return cached with { Outcome = HistoryLookupOutcome.Cache };
            var result = await ReadCachedAsync(number).ConfigureAwait(false);
            if (result == null || epoch != Epoch || _stop.IsCancellationRequested) return HistoryLookupResult.Missing;
            Remember(number, result, epoch);
            return epoch == Epoch && TryGetHistory(number, out cached)
                ? cached with { Outcome = HistoryLookupOutcome.Cache } : HistoryLookupResult.Missing;
        }

        public async Task<HistoryLookupResult> LookupAsync(string number, bool refresh = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string epoch = Epoch;
            if (!refresh)
            {
                var cached = await GetCachedNoteAsync(number).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (cached.HasValue && epoch == Epoch) return cached;
            }
            cancellationToken.ThrowIfCancellationRequested();
            await JoinAsync(number, refresh, cancellationToken).ConfigureAwait(false);
            if (epoch != Epoch) return HistoryLookupResult.Missing;
            bool hasValue = TryGetHistory(number, out var history);
            var outcome = _outcomes.TryGetValue(epoch + ":" + number, out var actual) ? actual : HistoryLookupOutcome.Stopped;
            return hasValue ? history with { Outcome = outcome } : new(false, "", outcome);
        }

        public async Task<(bool HasValue, string Note)> GetAnimalHistoryNoteAsync(string number, bool refresh = false)
        {
            string epoch = _cache.Epoch;
            await GetAnimalHistoryAsync(number, refresh).ConfigureAwait(false);
            if (epoch != _cache.Epoch || !TryGetNote(number, out string note) || epoch != _cache.Epoch)
                return (false, string.Empty);
            return (true, note);
        }

        public Task<string> GetAnimalHistoryAsync(string number, bool refresh = false) => JoinAsync(number, refresh, CancellationToken.None);

        private async Task<string> JoinAsync(string number, bool refresh, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_stop.IsCancellationRequested || !IsValidNumber(number) || !_settings.Enabled) return "";
            if (!refresh && TryGetNote(number, out string note)) return note;
            string epoch = Epoch;
            string key = epoch + ":" + number;
            PendingLookup work;
            lock (_pendingGate)
            {
                if (!_pending.TryGetValue(key, out work!))
                {
                    var stop = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                    work = new PendingLookup { Stop = stop, Task = Task.Run(() => LoadAsync(number, refresh, epoch, stop.Token)) };
                    _pending.Add(key, work);
                }
                work.Subscribers++;
            }
            try
            {
                string result = await work.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                return epoch == Epoch && !_stop.IsCancellationRequested ? result : "";
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return ""; }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { Notice("소유주 이력 조회에 실패했습니다. 잠시 후 다시 확인합니다."); return ""; }
            finally
            {
                lock (_pendingGate)
                {
                    if (--work.Subscribers == 0)
                    {
                        if (_pending.TryGetValue(key, out var current) && ReferenceEquals(work, current)) _pending.Remove(key);
                        work.Stop.Cancel();
                        _ = work.Task.ContinueWith(task => { _ = task.Exception; work.Stop.Dispose(); }, TaskScheduler.Default);
                    }
                }
            }
        }
        private string Remember(string number, AnimalHistoryResult result, string epoch)
        {
            if (epoch != _cache.Epoch || _stop.IsCancellationRequested) return "";
            string note = AnimalHistoryNoteFormatter.FormatAnimalHistoryNote(result);
            _history[epoch + ":" + number] = new HistoryLookupResult(true, note, HistoryLookupOutcome.Success)
            {
                LatestOwner = AnimalHistoryNoteFormatter.GetLatestOwner(result)
            };
            return note;
        }
        private async Task<AnimalHistoryResult?> ReadCachedAsync(string number)
        {
            string? stored = await _cache.GetCacheAsync("animal_history", number);
            AnimalHistoryResult? existing = null;
            try
            {
                var document = stored == null ? null : JObject.Parse(stored);
                if (document?["Version"]?.Value<int>() == AnimalHistoryResult.CurrentVersion && document["Status"]?.Value<int>() == (int)HistoryStatus.Success && document["Entries"] is JArray)
                    existing = document.ToObject<AnimalHistoryResult>();
                if (existing?.Entries == null || existing.Entries.Any(e => e == null || e.FarmAddress == null || string.IsNullOrWhiteSpace(e.Owner) || string.IsNullOrWhiteSpace(e.Type) ||
                    !DateTime.TryParseExact(e.Date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))) existing = null;
            }
            catch (Exception ex) when (ex is JsonException || ex is FormatException || ex is InvalidCastException) { existing = null; }
            return existing;
        }
        private async Task<string> LoadAsync(string number, bool refresh, string epoch, CancellationToken token)
        {
            if (!await _cache.EnsureTodayAsync().ConfigureAwait(false)) { OnUnavailable(); return TryGetNote(number, out var fallback) ? fallback : ""; }
            token.ThrowIfCancellationRequested();
            if (epoch != Epoch) return "";
            var existing = await ReadCachedAsync(number).ConfigureAwait(false);
            if (epoch != Epoch) return "";
            if (existing?.Version == AnimalHistoryResult.CurrentVersion && existing.Status == HistoryStatus.Success && !refresh)
            {
                _outcomes[epoch + ":" + number] = HistoryLookupOutcome.Cache;
                return Remember(number, existing, epoch);
            }
            string retryKey = epoch + ":" + number;
            if (_retryAfter.TryGetValue(retryKey, out var retry) && retry > DateTime.UtcNow)
            {
                if (_outcomes.TryGetValue(retryKey, out var prior) && prior == HistoryLookupOutcome.Success) _outcomes[retryKey] = HistoryLookupOutcome.Cache;
                return existing?.Status == HistoryStatus.Success ? Remember(number, existing, epoch) : "";
            }
            _outcomes[retryKey] = HistoryLookupOutcome.Failed;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (!await AdmitAsync(epoch, token).ConfigureAwait(false)) { _outcomes[retryKey] = HistoryLookupOutcome.Stopped; break; }
                AnimalHistoryResult result;
                try
                {
                    result = await _client.FetchAnimalHistoryAsync(_settings, number, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    if (epoch == Epoch && (result.Status is HistoryStatus.AuthenticationError or HistoryStatus.QuotaExceeded or HistoryStatus.Disabled))
                        await BlockAsync(epoch, result.Status).ConfigureAwait(false);
                }
                finally { await ReleaseRequestAsync().ConfigureAwait(false); }
                if (epoch != _cache.Epoch) break;
                if (result.Status == HistoryStatus.Success)
                {
                    _retryAfter[retryKey] = DateTime.UtcNow.AddMinutes(1);
                    _outcomes[retryKey] = HistoryLookupOutcome.Success;
                    await _cache.SaveCacheAsync("animal_history", number, JsonConvert.SerializeObject(result), epoch);
                    return Remember(number, result, epoch);
                }
                if (result.Status == HistoryStatus.AuthenticationError || result.Status == HistoryStatus.QuotaExceeded || result.Status == HistoryStatus.Disabled)
                {
                    _outcomes[retryKey] = HistoryLookupOutcome.Stopped;
                    break;
                }
                if (result.Status != HistoryStatus.TransientError)
                { Notice("소유주 이력 응답을 해석하지 못했습니다. 기존 정상 결과를 유지합니다."); break; }
                if (attempt < 2) await Task.Delay(TimeSpan.FromSeconds(2 * (attempt + 1)), token).ConfigureAwait(false);
                else Notice("소유주 이력 통신 오류로 재시도를 잠시 중단했습니다.");
            }
            _retryAfter[retryKey] = DateTime.UtcNow.AddMinutes(5);
            return existing?.Status == HistoryStatus.Success ? Remember(number, existing, epoch) : "";
        }

        private static readonly string BudgetKey = "animal-trace:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("Config/animalTrace.local.json")));

        private async Task<Budget?> ReadBudgetAsync()
        {
            try
            {
                string? saved = await _cache.GetCacheAsync("control", BudgetKey).ConfigureAwait(false);
                if (saved == null)
                {
                    string legacy = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(_settings.Address + "|Config/animalTrace.local.json")));
                    saved = await _cache.GetCacheAsync("control", "animal-trace:" + legacy).ConfigureAwait(false);
                }
                var budget = JsonConvert.DeserializeObject<Budget>(saved ?? "{}") ?? new Budget();
                if (budget.Count >= 0 && budget.Stop != null) return budget;
            }
            catch (JsonException) { }
            Notice("이력 API 호출 예산을 확인할 수 없어 조회를 중단했습니다.");
            return null;
        }

        // Only admission and budget writes are serialized. HTTP responses and retry delays are not.
        private async Task<bool> AdmitAsync(string epoch, CancellationToken token)
        {
            while (true)
            {
                Task? finished = null;
                TimeSpan delay = TimeSpan.Zero;
                await _admission.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    if (epoch != Epoch || !_settings.Enabled || _cacheUnavailable || _blockedEpochs.ContainsKey(epoch)) return false;
                    if (_activeRequests >= MaxConcurrentRequests) finished = _requestFinished.Task;
                    else
                    {
                        delay = _nextRequest - DateTime.UtcNow;
                        if (delay <= TimeSpan.Zero)
                        {
                            var budget = await ReadBudgetAsync().ConfigureAwait(false);
                            if (budget == null) { _blockedEpochs.TryAdd(epoch, 0); return false; }
                            if (budget.Stop.Length > 0 || (_settings.DailyLimit > 0 && budget.Count >= _settings.DailyLimit))
                            {
                                _blockedEpochs.TryAdd(epoch, 0);
                                Notice("이력 API 호출 예산 또는 인증 상태로 인해 조회가 중단되었습니다.");
                                return false;
                            }
                            budget.Count = checked(budget.Count + 1);
                            if (!await _cache.SaveCacheAsync("control", BudgetKey, JsonConvert.SerializeObject(budget), epoch).ConfigureAwait(false))
                            { OnUnavailable(); return false; }
                            if (epoch != Epoch || token.IsCancellationRequested || _blockedEpochs.ContainsKey(epoch)) return false;
                            _nextRequest = DateTime.UtcNow.AddMilliseconds(AnimalTraceSettings.NormalizeRequestInterval(_settings.RequestIntervalMs));
                            _activeRequests++;
                            return true;
                        }
                    }
                }
                finally { _admission.Release(); }
                if (finished != null) await finished.WaitAsync(token).ConfigureAwait(false);
                else await Task.Delay(delay, token).ConfigureAwait(false);
            }
        }
        private async Task ReleaseRequestAsync()
        {
            await _admission.WaitAsync().ConfigureAwait(false);
            try
            {
                _activeRequests--;
                var signal = _requestFinished;
                _requestFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
                signal.TrySetResult();
            }
            finally { _admission.Release(); }
        }
        private async Task BlockAsync(string epoch, HistoryStatus status)
        {
            _blockedEpochs.TryAdd(epoch, 0);
            await _admission.WaitAsync(_stop.Token).ConfigureAwait(false);
            try
            {
                if (epoch != Epoch) return;
                var budget = await ReadBudgetAsync().ConfigureAwait(false);
                if (budget != null)
                {
                    budget.Stop = status.ToString();
                    await _cache.SaveCacheAsync("control", BudgetKey, JsonConvert.SerializeObject(budget), epoch).ConfigureAwait(false);
                }
                Notice("이력 API 인증·설정 또는 허용량을 확인해 주세요. 자동 조회를 중단했습니다.");
            }
            finally { _admission.Release(); }
        }

        public void Dispose()
        {
            _stop.Cancel(); _client.Dispose();
            _cache.DayChanged -= OnDayChanged; _cache.Unavailable -= OnUnavailable;
            _cache.CleanupIncomplete -= OnCleanupIncomplete;
        }
    }
}
