using CowAuctionSmall.Infra.Netty.netty;
using CowAuctionSmall.Infra.Logging;
using CowAuctionSmall.Infra.Persistence;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Infra.Api;
using CowAuctionSmall.Domain.Auction;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading; // Interlocked 사용
using System.Threading.Tasks;
using UserInfo = CowAuctionSmall.Domain.Settings.UserInfo;

namespace CowAuctionSmall.Infra.Api
{
    public enum NettyConnectResult
    {
        Connected,
        Duplicate,
        Timeout,
        Failed
    }

    /// <summary>
    /// 서버에서 연결 및 데이터 받아오는 곳
    /// </summary>
    public class ServerConn
    {
        //private HttpWebRequest request;

        private NLogger logger;
        private readonly WeakReferenceMessenger _messengerStringDateMsg;
        private string? date;
        private readonly HttpClient _http;
        private UserInfo? _userInfo;
        private string? _token;
        private LoginResult? _loginResult;
        private readonly DailyCacheService? _cache;
        private readonly AuctionSnapshotCache? _snapshots;
        private long _snapshotRequest;
        // -------------------------------------------------------------------
        // 💡 [추가] API 총 호출 건수 누적 카운터 (스레드 안전)
        // -------------------------------------------------------------------
        private static long _totalApiCount = 0;
        public static long TotalApiCount => Interlocked.Read(ref _totalApiCount);
        // -------------------------------------------------------------------

        public static ServerConn? Instance { get; private set; }
        /// <summary>
        /// 서버 통신에 사용할 HttpClient를 주입받아 초기화한다.
        /// </summary>
        public ServerConn(HttpClient http, DailyCacheService? cache = null)
        {
            Instance = this;

            _http = http;
            _cache = cache;
            _snapshots = cache == null ? null : new AuctionSnapshotCache(cache);
            if (_cache != null) _ = _cache.EnsureTodayAsync();

            // NLogger 초기화
            logger = NLogger.Instance;
            _messengerStringDateMsg = WeakReferenceMessenger.Default;
            _messengerStringDateMsg.Register<DataToServerConnMsg>(this, OnChangeDeta);
        }

        /// <summary>
        /// 초기 토큰 생성
        /// </summary>
        public async Task<LoginResult?> IssueToken(UserInfo userInfo)
        {
            _loginResult = null;
            _token = null;
            _userInfo = null;

            if (userInfo?.Authentication == null || userInfo.Auction == null || userInfo.CurrentInfo == null ||
                string.IsNullOrWhiteSpace(userInfo.Authentication.UserID) ||
                string.IsNullOrWhiteSpace(userInfo.Authentication.Password))
            {
                logger.LogError("IssueToken: 인증 또는 사업장 설정이 없습니다.");
                return null;
            }
            // 💡 [추가] 토큰 발급 API 호출 카운트 증가
            Interlocked.Increment(ref _totalApiCount);

            string? url = userInfo.Authentication.Address;
            string payloadJson = JsonConvert.SerializeObject(new
            {
                usrid = userInfo.Authentication.UserID.Trim(),
                pw = userInfo.Authentication.Password.Trim()
            });

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                logger.LogError("IssueToken: 요청 URL이 잘못되었습니다.");
                return null;
            }

            for (int initRetry = 0; initRetry < 5; initRetry++)
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                    break;

                logger.LogWarn("IssueToken: 네트워크 연결을 기다립니다...");
                await Task.Delay(5000);
            }

            for (int retry = 0; retry < 10; retry++)
            {
                try
                {
                    logger.LogInfo($"IssueToken: 토큰 요청 (시도 {retry + 1}/10)");
                    using var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                    };

                    using HttpResponseMessage response = await _http.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogError($"IssueToken: HTTP 오류 - {response.StatusCode}");
                        return null;
                    }

                    string responseText = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(responseText))
                    {
                        logger.LogError("IssueToken: 응답 본문이 비어 있습니다.");
                        return null;
                    }

                    var loginResult = LoginResult.FromResponse(JObject.Parse(responseText));
                    if (loginResult == null)
                    {
                        logger.LogError("IssueToken: 로그인 응답의 인증 정보 또는 사업장 코드가 없거나 올바르지 않습니다.");
                        return null;
                    }

                    AuctionApiAddressResolver.Resolve(userInfo.CurrentInfo.Address ?? string.Empty, loginResult.AuctionHouseCode);
                    if (!string.IsNullOrWhiteSpace(userInfo.CurrentInfo.AddressEPD))
                    {
                        AuctionApiAddressResolver.Resolve(userInfo.CurrentInfo.AddressEPD, loginResult.AuctionHouseCode);
                    }

                    userInfo.Auction.AuctionHouseCode = loginResult.AuctionHouseCode;
                    _userInfo = userInfo;
                    _token = loginResult.AccessToken;
                    _loginResult = loginResult;
                    logger.LogInfo("IssueToken: 인증 정보 및 사업장 확인 완료");
                    return loginResult;
                }
                catch (HttpRequestException) when (retry < 9)
                {
                    logger.LogWarn($"IssueToken: 요청 실패 (재시도 {retry + 1}/10)");
                    await Task.Delay(1000 * (retry + 1));
                }
                catch (Exception)
                {
                    logger.LogError("IssueToken: 인증 요청 또는 API 주소 설정을 확인하지 못했습니다.");
                    return null;
                }
            }

            logger.LogError("IssueToken: 모든 시도가 실패했습니다.");
            return null;
        }


        /// <summary>
        /// 데이터 덩어리를 고정적으로, 오늘 경매하는 데이터 받아올때
        /// "SC|8808990657202|1|314|5|062024052200001|06|G000001|1|홍길동||(개월 일)||암|||||||||서울||0|23|20000|20000|비고1|11||||N|1|N||20240522092837|||"
        /// </summary>
        public async Task<List<gValues>?> ReadCachedAuctionAsync(UserInfo userInfo)
        {
            if (_snapshots == null || _loginResult == null) return null;
            string queryDate = string.IsNullOrWhiteSpace(userInfo.CurrentInfo?.Date) ? DateTime.Now.ToString("yyyyMMdd") : userInfo.CurrentInfo.Date;
            return await _snapshots.ReadAsync(userInfo, queryDate);
        }

        public async Task<List<string>> SvInfoRequest(UserInfo userInfo, string token, string? date = null, string? refresh=null)
        {
            try
            {
                if (refresh != null && refresh == "refresh")
                {
                    if (_userInfo == null || _token == null)
                    {
                        logger.LogError("SvInfoRequest: refresh 요청인데 _userInfo 또는 _token이 null입니다.");
                        return new List<string>();
                    }

                    userInfo = _userInfo;
                    if (userInfo.CurrentInfo == null)
                    {
                        logger.LogError("SvInfoRequest: CurrentInfo가 null입니다.");
                        return new List<string>();
                    }
                    userInfo.CurrentInfo.Date = date ?? userInfo.CurrentInfo.Date;
                    token = _token;

                }

                JObject? currentJObject = await GetCurrentInfo(userInfo, token, date);
                if (currentJObject?["entry"] is not JArray entries)
                    throw new InvalidOperationException("Auction query did not return a valid snapshot.");

                if (currentJObject != null && currentJObject.Count > 0 && userInfo != null && token != null)
                {

                    List<string> currentInfoList = entries.Select(s => (string?)s
                        ?? throw new InvalidOperationException("Auction snapshot contains a null entry.")).ToList();

                    /* 사용안함
                     * if (currentInfoList != null && userInfo.Auction.IsGoatAuction.ToUpper().Equals("N"))// 염소 경매 옵션
                    {
                        bool existGoat = currentInfoList.First().Split('|')[4] == "5";
                        if (existGoat)
                        {
                            currentInfoList.Clear();
                        }
                    }*/
                    if (AsyncDebugOutput.Enabled)
                        AsyncDebugOutput.WriteLine("[" + DateTime.Now.ToString() + "] " + (currentInfoList?.Count ?? 0) + "개의 API 경매 정보가 수신되었습니다.\r\n");
                    return currentInfoList ?? new List<string>();
                }
                else
                {
                    AsyncDebugOutput.WriteLine("API 수신데이터가 없습니다.");
                    logger.LogError("API 수신데이터가 없습니다.");
                    return new List<string>();
                }
            }
            catch (Exception)
            {
                logger.LogError("Auction snapshot request failed; existing data is retained.");
                throw;
            }
        }


        

        /// <summary>
        /// 오늘 경매하는 데이터 덩어리를 받아오는 함수
        /// </summary>
        public async Task<JObject?> GetCurrentInfo(UserInfo userInfo, string token, string? forcedDate = null)
        {
            var loginResult = _loginResult;
            if (loginResult == null || _userInfo == null)
            {
                logger.LogError("GetCurrentInfo: 확인된 로그인 사업장 정보가 없습니다.");
                return null;
            }
            userInfo = _userInfo;
            token = loginResult.AccessToken;
            if (userInfo?.CurrentInfo?.Address == null || string.IsNullOrEmpty(token))
            {
                logger.LogError("GetCurrentInfo: UserInfo 또는 Token이 null입니다.");
                return null;
            }
            // 💡 [추가] 경매 정보 데이터 요청 API 호출 카운트 증가
            Interlocked.Increment(ref _totalApiCount);

            string dateToUse = "";

            if (!string.IsNullOrWhiteSpace(forcedDate))
            {
                dateToUse = forcedDate;
            }
            else if (date == null || date.Equals(""))
            {
                dateToUse = string.IsNullOrEmpty(userInfo.CurrentInfo.Date)
                ? DateTime.Now.ToString("yyyyMMdd")
                : userInfo.CurrentInfo.Date;
            }
            else
            {
                dateToUse = date;
            }

            string epoch = _cache?.Epoch ?? "";
            long requestVersion = Interlocked.Increment(ref _snapshotRequest);
            string fullUrl;
            try
            {
                fullUrl = AuctionApiAddressResolver.Resolve(userInfo.CurrentInfo.Address, loginResult.AuctionHouseCode) + dateToUse;
            }
            catch (ArgumentException)
            {
                logger.LogError("GetCurrentInfo: API 주소 설정이 올바르지 않습니다.");
                return null;
            }

            // 인증서 유효성 검사 비활성화
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                //logger.LogInfo("GetCurrentInfo: 서버 요청 중...");
                HttpResponseMessage response = await client.GetAsync(fullUrl, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError($"GetCurrentInfo: 서버 응답 오류 - {response.StatusCode}");
                    return null;
                }

                string responseText = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(responseText))
                {
                    logger.LogError("GetCurrentInfo: 응답 내용이 비어있습니다.");
                    return null;
                }

                JObject responseObject = JObject.Parse(responseText);
                if (responseObject.SelectToken("success")?.Value<bool>() == true)
                {
                    if (_snapshots != null && responseObject["entry"] is JArray entries && requestVersion == Interlocked.Read(ref _snapshotRequest))
                    {
                        try
                        {
                            var parser = new AnimalParseData();
                            var parsed = entries.Select(e => parser.Parse_PacketApi(e.Value<string>() ?? "", userInfo, this)).ToList();
                            if (parsed.All(e => e != null)) await _snapshots.SaveAsync(userInfo, dateToUse, parsed, epoch);
                        }
                        catch (Exception) { logger.LogWarn("Auction snapshot could not be cached."); }
                    }
                    return responseObject;
                }

                logger.LogError("GetCurrentInfo: API 요청 실패.");
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError($"GetCurrentInfo: 예외 발생 - {ex.Message}");
                return null;
            }
        }



        /// <summary>일괄 구간 번호의 출품번호 범위를 명시적으로 조회한다. 기존 날짜별 회차 조회와 별개이다.</summary>
        public Task<AuctionSectionRange?> ReadAuctionSectionRangeAsync(DateOnly date, int cowType, int sectionNumber,
            CancellationToken cancellationToken = default)
        {
            var login = _loginResult;
            if (login == null || !Uri.TryCreate(_userInfo?.Authentication?.Address, UriKind.Absolute, out var address) ||
                (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps))
                return Task.FromResult<AuctionSectionRange?>(null);
            var endpoint = new Uri(address, "/api/v1/auction/select/qcn");
            Interlocked.Increment(ref _totalApiCount);
            return new AuctionSectionApiClient(_http).ReadAsync(endpoint, login.AccessToken, login.AuctionHouseCode,
                date, cowType, sectionNumber, cancellationToken);
        }

        /// <summary>
        /// QCN 정보를 조회한다.
        /// </summary>
        public async Task<Qcn?> PostQcn(UserInfo userInfo, string? token, string date, string? qcn = null, string? refresh=null)
        {
            var loginResult = _loginResult;
            if (loginResult == null || _userInfo == null)
            {
                logger.LogError("PostQcn: 확인된 로그인 사업장 정보가 없습니다.");
                return null;
            }
            userInfo = _userInfo;
            token = loginResult.AccessToken;
            // 💡 [추가] QCN 정보 조회 API 호출 카운트 증가
            Interlocked.Increment(ref _totalApiCount);

            if (refresh != null && refresh== "refresh")
            {
                if (_userInfo == null || _token == null)
                {
                    logger.LogError("PostQcn: refresh 요청인데 _userInfo 또는 _token이 null입니다.");
                    return null;
                }
                userInfo = _userInfo;
                token = _token;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                logger.LogError("PostQcn: token이 없습니다.");
                return null;
            }

            if ((string.IsNullOrWhiteSpace(qcn) ? 0 : 1) + (string.IsNullOrWhiteSpace(date) ? 0 : 1) != 1)
                throw new ArgumentException("qcn 또는 date 중 하나만 지정하십시오.");

            string naBzplc = loginResult.AuctionHouseCode;

            string? dateStr = null;
            if (!string.IsNullOrWhiteSpace(date))
            {
                if (date.Length != 8) throw new ArgumentException("date는 yyyyMMdd 형식이어야 합니다.", nameof(date));
                dateStr = date;
            }

            object payload = !string.IsNullOrWhiteSpace(qcn)
                ? new { naBzplc, qcn }
                : new { naBzplc, aucDt = dateStr };

            var json = JsonConvert.SerializeObject(payload);
            var addressQcn = userInfo.CurrentInfo?.AddressQcn;
            if (string.IsNullOrWhiteSpace(addressQcn))
                throw new ArgumentException("AddressQcn이 비어 있습니다.");

            using var req = new HttpRequestMessage(HttpMethod.Post, addressQcn)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var res = await _http.SendAsync(req);
            res.EnsureSuccessStatusCode();

            string body = await res.Content.ReadAsStringAsync();
            var jo = JObject.Parse(body);

            if (jo.SelectToken("success")?.Value<bool>() != true)
                throw new InvalidOperationException($"API error: {jo.SelectToken("message")?.Value<string>()}");

            var arr = jo.SelectToken("data") as JArray;
            if (arr == null || arr.Count == 0)
                return null;

            return arr[0].ToObject<Qcn>();
        }






        /// <summary>
        /// 네티 대리자 실행, 비동기적으로 데이터를 받아온다
        /// </summary>
        private TaskCompletionSource<NettyConnectResult>? _nettyConnResultTcs;

        public async Task<NettyConnectResult> NettyComm(UserInfo userInfo, string token)
        {
            var loginResult = _loginResult;
            if (loginResult == null || _userInfo == null)
            {
                logger.LogError("NettyComm: 확인된 로그인 사업장 정보가 없습니다.");
                return NettyConnectResult.Failed;
            }
            userInfo = _userInfo;
            token = loginResult.AccessToken;
            _nettyConnResultTcs = new TaskCompletionSource<NettyConnectResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            // 메시지 등록 (한 번만)
            WeakReferenceMessenger.Default.Register<NettyConnectionResultMessage>(this, OnNettyConnectionResult);

            try
            {
                // 연결 요청 시작
                string houseCode = loginResult.AuctionHouseCode;
                string id = userInfo.Authentication?.UserID ?? throw new ArgumentNullException(nameof(userInfo.Authentication.UserID));
                string channel = userInfo.Auction?.Channel ?? throw new ArgumentNullException(nameof(userInfo.Auction.Channel));
                string priority = userInfo.Auction?.Priority ?? throw new ArgumentNullException(nameof(userInfo.Auction.Priority));
                string host = userInfo.Auction.Address ?? throw new ArgumentNullException(nameof(userInfo.Auction.Address));
                int port = Convert.ToInt32(userInfo.Auction.Port);

                NettyControllable nc = new NettyControllable(houseCode, id, token, channel, priority);
                AuctionDelegate.getInstance().createClients(host, port, nc);

                // 최대 5초까지 응답 대기
                var timeoutTask = Task.Delay(5000);
                var resultTask = await Task.WhenAny(_nettyConnResultTcs.Task, timeoutTask);
                if (resultTask == timeoutTask)
                {
                    AsyncDebugOutput.WriteLine("5초 연결 대기 시간 초과");
                    return NettyConnectResult.Timeout;
                }

                var result = _nettyConnResultTcs.Task.Result;
                AsyncDebugOutput.WriteLine($"Netty 연결 결과: {result}");
                return result;
            }
            finally
            {
                // 메시지 등록 해제
                WeakReferenceMessenger.Default.Unregister<NettyConnectionResultMessage>(this);
            }
        }

        /// <summary>
        /// 네티 연결 결과를 대기 중인 Task에 전달한다.
        /// </summary>
        private void OnNettyConnectionResult(object recipient, NettyConnectionResultMessage message)
        {
            _nettyConnResultTcs?.TrySetResult(MapNettyResult(message.ResultCode));
        }

        private static NettyConnectResult MapNettyResult(string? resultCode)
        {
            return resultCode switch
            {
                "2000" => NettyConnectResult.Connected,
                "2002" => NettyConnectResult.Duplicate,
                _ => NettyConnectResult.Failed
            };
        }

        /// <summary>
        /// 일괄 경매, 경매 대상을 클릭시 날짜 변경
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="message"></param>
        private void OnChangeDeta(object recipient, DataToServerConnMsg message)
        {
            string msg = message.Data;
            date = msg;
            logger.LogInfo($"GetCurrentInfo OnChangeDeta : 일괄경매 날짜 변경 {date}");
        }


    }


}
