using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Domain.AnimalHistory;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CowAuctionSmall.Infra.Api
{
    public sealed class AnimalHistoryClient : IAnimalHistoryClient
    {
        private readonly HttpClient _http;
        private long _requestCount;
        public long RequestCount => Interlocked.Read(ref _requestCount);
        public AnimalHistoryClient(HttpClient? http = null)
        {
            _http = http ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        }

        public async Task<AnimalHistoryResult> FetchAnimalHistoryAsync(AnimalTraceSettings settings, string number, CancellationToken cancellationToken)
        {
            try
            {
                if (!Uri.TryCreate(settings.Address, UriKind.Absolute, out var address) ||
                    (address.Scheme != "http" && address.Scheme != "https") || address.Query.Length > 0 || address.Fragment.Length > 0)
                    return new AnimalHistoryResult { Status = HistoryStatus.Disabled };
                string encoded = settings.ServiceKey;
                if (string.IsNullOrWhiteSpace(encoded)) return new AnimalHistoryResult { Status = HistoryStatus.Disabled };
                string key = Uri.EscapeDataString(Uri.UnescapeDataString(encoded.Trim()));
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                using var request = new HttpRequestMessage(HttpMethod.Get, settings.Address + "?ServiceKey=" + key + "&traceNo=" + Uri.EscapeDataString(number) + "&optionNo=2");
                Interlocked.Increment(ref _requestCount);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                    return new AnimalHistoryResult { Status = HistoryStatus.AuthenticationError };
                if ((int)response.StatusCode == 429) return new AnimalHistoryResult { Status = HistoryStatus.QuotaExceeded };
                if (!response.IsSuccessStatusCode) return new AnimalHistoryResult { Status = HistoryStatus.TransientError };
                using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                using var reader = new StreamReader(stream, Encoding.UTF8, true);
                char[] buffer = new char[4096];
                var text = new StringBuilder();
                int count;
                while ((count = await reader.ReadAsync(buffer.AsMemory(), timeout.Token)) > 0)
                {
                    if (text.Length + count > 1024 * 1024) return new AnimalHistoryResult { Status = HistoryStatus.InvalidResponse };
                    text.Append(buffer, 0, count);
                }
                return AnimalHistoryParser.ParseAnimalHistory(text.ToString());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception) { return new AnimalHistoryResult { Status = HistoryStatus.TransientError }; }
        }

        public void Dispose() => _http.Dispose();
    }
}
