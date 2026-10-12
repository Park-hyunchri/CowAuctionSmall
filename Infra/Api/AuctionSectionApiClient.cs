using CowAuctionSmall.Domain.Auction;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace CowAuctionSmall.Infra.Api
{
    /// <summary>일괄경매 구간 범위 조회. 호출자가 요청한 경우에만 조회하며 화면 상태를 변경하지 않는다.</summary>
    public sealed class AuctionSectionApiClient
    {
        private readonly HttpClient _http;
        public AuctionSectionApiClient(HttpClient http) { _http = http; }

        public async Task<AuctionSectionRange?> ReadAsync(Uri endpoint, string token, string houseCode,
            DateOnly date, int cowType, int sectionNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrEmpty(houseCode) ||
                houseCode.Any(c => c < '0' || c > '9') || cowType < 0 || cowType > 3 || sectionNumber < 1)
                return null;
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["naBzplc"] = houseCode,
                ["aucDt"] = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                ["aucObjDsc"] = cowType.ToString(CultureInfo.InvariantCulture),
                ["aucDsc"] = "2"
            });
            try
            {
                using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return null;
                string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var root = JObject.Parse(json);
                if (root["success"]?.Type != JTokenType.Boolean || root["success"]!.Value<bool>() != true ||
                    root["stnList"] is not JArray sections) return null;
                var matches = sections.OfType<JObject>().Where(item => Number(item["RG_SQNO"]) == sectionNumber).ToArray();
                if (matches.Length != 1) return null;
                int? start = Number(matches[0]["ST_AUC_NO"]);
                int? end = Number(matches[0]["ED_AUC_NO"]);
                return start is > 0 && end.HasValue && end.Value >= start.Value
                    ? new AuctionSectionRange(start.Value, end.Value) : null;
            }
            catch (HttpRequestException) { return null; }
            catch (JsonException) { return null; }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return null; }
        }

        private static int? Number(JToken? value) => value?.Type is JTokenType.Integer or JTokenType.String &&
            int.TryParse(value.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out int number) ? number : null;
    }
}
