using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.NetProto.netty;
using DocumentFormat.OpenXml.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;
using UserInfo = CowAuctionSmall.Models.Structures.UserInfo;

namespace CowAuctionSmall.Models
{
    /// <summary>
    /// 서버에서 연결 및 데이터 받아오는 곳
    /// </summary>
    public class ServerConn
    {
        //private HttpWebRequest request;

        private NLogger logger;

        private readonly WeakReferenceMessenger _messengerStringDateMsg;

        private string date;

        public ServerConn() 
        {
            // NLogger 초기화
            logger = NLogger.Instance;
            _messengerStringDateMsg = WeakReferenceMessenger.Default;
            _messengerStringDateMsg.Register<DataToServerConnMsg>(this, OnChangeDeta);
        }

        /// <summary>
        /// 초기 토큰 생성
        /// </summary>
        /// <summary>
        /// 초기 토큰 생성
        /// </summary>
/*        public async Task<string?> IssueTocken(UserInfo userInfo)
        {
            if (userInfo?.Authentication?.Address == null)
            {
                logger.LogError("IssueTocken: userInfo.Authentication.Address가 null입니다.");
                return null;
            }

            string? token = string.Empty;
            try
            {
                Debug.WriteLine("인증서버 연결 시도 중... " + "\r\n");
                logger.LogInfo("IssueTocken 토큰 인증서버 연결 시도 중...: ");
                string? url = userInfo.Authentication?.Address;
                var content = new StringContent(JsonConvert.SerializeObject(new { usrid = userInfo.Authentication?.UserID.Trim(), pw = userInfo.Authentication?.Password.Trim() }), Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10); // 예: 10분으로 Timeout 설정

                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseText = await response.Content.ReadAsStringAsync();

                        if (!string.IsNullOrEmpty(responseText))
                        {
                            JObject? jObject = JObject.Parse(responseText);
                            if ((bool)jObject.SelectToken("success"))
                            {
                                token = (string)jObject.SelectToken("accessToken");
                            }
                        }
                        return token;
                    }
                    else
                    {
                        Debug.WriteLine("IssueTocken 서버 응답 오류: " + response.StatusCode + "\r\n");
                        logger.LogError("IssueTocken 서버 응답 오류: " + response.StatusCode);
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + "\r\n");
                logger.LogError("IssueTocken 발생 " + e.Message);
                return null;
            }
        }*/

        public async Task<string?> IssueTocken(UserInfo userInfo)
        {
            if (userInfo?.Authentication?.Address == null)
            {
                logger.LogError("IssueTocken: userInfo.Authentication.Address가 null입니다.");
                return null;
            }

            string? token = string.Empty;
            string? url = userInfo.Authentication?.Address;
            var content = new StringContent(JsonConvert.SerializeObject(new { usrid = userInfo.Authentication?.UserID.Trim(), pw = userInfo.Authentication?.Password.Trim() }), Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10);
                for (int retry = 0; retry < 3; retry++) // 3번까지 재시도
                {
                    try
                    {
                        Debug.WriteLine("인증서버 연결 시도 중... " + "\r\n");
                        logger.LogInfo("IssueTocken 토큰 인증서버 연결 시도 중...: ");
                        HttpResponseMessage response = await client.PostAsync(url, content);

                        if (response.IsSuccessStatusCode)
                        {
                            string responseText = await response.Content.ReadAsStringAsync();
                            if (!string.IsNullOrEmpty(responseText))
                            {
                                JObject? jObject = JObject.Parse(responseText);
                                if ((bool)jObject.SelectToken("success"))
                                {
                                    token = (string)jObject.SelectToken("accessToken");
                                    return token;
                                }
                            }
                            return null;
                        }
                        else
                        {
                            Debug.WriteLine("IssueTocken 서버 응답 오류: " + response.StatusCode + "\r\n");
                            logger.LogError("IssueTocken 서버 응답 오류: " + response.StatusCode);
                            return null;
                        }
                    }
                    catch (HttpRequestException e) when (retry < 2) // 네트워크 예외 발생 시 재시도
                    {
                        logger.LogError($"IssueTocken 재시도 {retry + 1}/3: {e.Message}");
                        await Task.Delay(1000); // 1초 대기 후 재시도
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message + "\r\n");
                        logger.LogError("IssueTocken 발생 " + e.Message);
                        return null;
                    }
                }
            }

            return null;
        }




        /// <summary>
        /// 데이터 덩어리를 고정적으로, 오늘 경매하는 데이터 받아올때
        /// "SC|8808990657202|1|314|5|062024052200001|06|G000001|1|홍길동||(개월 일)||암|||||||||서울||0|23|20000|20000|비고1|11||||N|1|N||20240522092837|||"
        /// </summary>
        public async Task<List<string>> SvInfoRequest(UserInfo userInfo, string token)
        {
            try
            {
                JObject currentJObject = await GetCurrentInfo(userInfo, token);

                if (currentJObject != null && currentJObject.Count > 0  && userInfo != null && token !=null)
                {

                    List<string> currentInfoList = currentJObject.SelectToken("entry")?.Select(s => (string)s).ToList();

                    if (currentInfoList != null && userInfo.Auction.IsGoatAuction.ToUpper().Equals("N"))// 염소 경매 옵션
                    {
                        bool existGoat = currentInfoList.First().Split('|')[4] == "5";
                        if (existGoat)
                        {
                            currentInfoList.Clear();
                        }
                    }
                    Debug.WriteLine("[" + DateTime.Now.ToString() + "] " + currentInfoList.Count + "개의 API 경매 정보가 수신되었습니다.\r\n");
                    return currentInfoList;
                }
                else
                {
                    Debug.WriteLine("API 수신데이터가 없습니다.");
                    logger.LogError("API 수신데이터가 없습니다.");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                logger.LogError("Task<List<string>> SvInfoRequest(UserInfo userInfo, string token)" + e.Message);
                return null;
            }
        }

        /// <summary>
        /// 실질적으로 오늘 경매하는 데이터 덩어리를 받아오는 곳
        /// </summary>
        public async Task<JObject> GetCurrentInfo(UserInfo userInfo, string token)
        {
            string url = userInfo.CurrentInfo.Address;

            if (userInfo.CurrentInfo.Date.Length > 0) // user.xml에서 설정한 값이 있는지
            {
                date = userInfo.CurrentInfo.Date;
            }
            else if (string.IsNullOrEmpty(date)) // date가 비어있는지 당연히 오늘날짜로 받아옴
            {
                date = DateTime.Now.ToString("yyyyMMdd");
            }
            else
            {
                date = date;
            }

            string fullUrl = url + date;

            // 인증서 유효성 검사 비활성화 (테스트 환경에서만 사용)
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromMinutes(15); // Timeout 설정을 15분으로 증가

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                try
                {
                    HttpResponseMessage response = await client.GetAsync(fullUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseText = await response.Content.ReadAsStringAsync();

                        if (!string.IsNullOrEmpty(responseText))
                        {
                            JObject? jObject = JObject.Parse(responseText);
                            if ((bool)jObject.SelectToken("success"))
                            {
                                return jObject;
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("서버 응답 오류: " + response.StatusCode);
                        logger.LogError("GetCurrentInfo 서버 응답 오류: " + response.StatusCode);
                    }
                }
                catch (TaskCanceledException e)
                {
                    // Timeout 에러 처리
                    Debug.WriteLine("요청이 시간 초과되었습니다: " + e.Message);
                    logger.LogError("GetCurrentInfo 요청 시간 초과: " + e.Message);
                }
                catch (Exception e)
                {
                    try
                    {
                        Debug.WriteLine(e.Message);
                        logger.LogError("GetCurrentInfo : " + e.Message);

/*                        logger.LogError("GetCurrentInfo : " + e.Message + "\n NettyComm 초기화 후 시작 2초 대기후 진행");
                        AuctionDelegate.getInstance().disposeClients();
                        await Task.Delay(2000); // 1초 대기
                        NettyComm(userInfo, token);*/
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("GetCurrentInfo 2.... : " + e.Message + "\n 재 실행");
                        var fileName = Process.GetCurrentProcess().MainModule.FileName;

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                // 새 프로세스 시작
                                Process.Start(fileName);
                                // 현재 애플리케이션 종료
                                Application.Current.Shutdown();
                            }
                            catch (Exception ex)
                            {
                                // 예외 처리 (필요 시 로그 작성 또는 사용자에게 알림)
                                MessageBox.Show($"프로그램 재시작 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        });
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// 네티 대리자 실행, 비동기적으로 데이터를 받아온다
        /// </summary>
        /// <param name="userInfo"></param>
        /// <param name="token"></param>
        public bool NettyComm(UserInfo userInfo, string token)
        {
            /*string host = userInfo.Auction.Address;
            int port = Convert.ToInt32(userInfo.Auction.Port);
            string id = userInfo.Authentication.UserID;

            NettyControllable nc = new NettyControllable(userInfo.Auction.AuctionHouseCode, id, token, userInfo.Auction.Channel, userInfo.Auction.Priority);*/

            string houseCode = userInfo.Auction?.AuctionHouseCode ?? throw new ArgumentNullException(nameof(userInfo.Auction.AuctionHouseCode));
            string id = userInfo.Authentication?.UserID ?? throw new ArgumentNullException(nameof(userInfo.Authentication.UserID));
            string channel = userInfo.Auction?.Channel ?? throw new ArgumentNullException(nameof(userInfo.Auction.Channel));
            string priority = userInfo.Auction?.Priority ?? throw new ArgumentNullException(nameof(userInfo.Auction.Priority));
            string host = userInfo.Auction.Address ?? throw new ArgumentNullException(nameof(userInfo.Auction.Address));
            int port = Convert.ToInt32(userInfo.Auction.Port);

            NettyControllable nc = new NettyControllable(houseCode, id, token, channel, priority);


            AuctionDelegate.getInstance().createClients(host, port, nc);

            bool isActiveNetty = AuctionDelegate.getInstance().isActive();
            Debug.WriteLine($"isActiveNetty : {isActiveNetty}");
            return isActiveNetty;
        }

        public async Task<List<EpdValue>> GetCurrentInfoEPD(UserInfo userInfo, string token)
        {
            List<EpdValue> responseListEpd = new List<EpdValue>();

            string url = userInfo.CurrentInfo.AddressEPD;
            
            if (userInfo.CurrentInfo.Date.Length > 0)
            {
                date = userInfo.CurrentInfo.Date;
            }
            else if (string.IsNullOrEmpty(date))
            {
                date = DateTime.Now.ToString("yyyyMMdd");
            }
            else
            {
                date = date;
            }

            string fullUrl = url.Replace("date", date);

            // TLS 1.2 강제 설정
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                try
                {
                    HttpResponseMessage response = await client.GetAsync(fullUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseText = await response.Content.ReadAsStringAsync();

                        if (!string.IsNullOrEmpty(responseText))
                        {
                            JObject jObject = JObject.Parse(responseText);
                            if ((bool)jObject.SelectToken("success"))
                            {
                                JArray dataArray = (JArray)jObject["data"];

                                foreach (JToken item in dataArray)
                                {
                                    EpdValue epdValue = new EpdValue
                                    {
                                        EPD_1 = item.Value<string>("EPD_1") ?? string.Empty,
                                        EPD_2 = item.Value<string>("EPD_2") ?? string.Empty,
                                        EPD_3 = item.Value<string>("EPD_3") ?? string.Empty,
                                        EPD_4 = item.Value<string>("EPD_4") ?? string.Empty,
                                        SRA_INDV_AMNNO = item.Value<string>("SRA_INDV_AMNNO") ?? string.Empty
                                    };
                                    responseListEpd.Add(epdValue);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("서버 응답 오류: " + response.StatusCode);
                        logger.LogError("GetCurrentInfoEPD : 서버 응답 오류");
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    logger.LogError("GetCurrentInfoEPD : " + e.Message);
                }
            }

            return responseListEpd;
        }


        public List<string> JoinEpdnData(List<string> dataList, List<EpdValue> epdlist)
        {
            List<string> newDataList = new List<string>();
            if (dataList != null)
            {
                foreach (string dataItem in dataList)
                {
                    string[] dataParts = dataItem.Split('|');
                    string sraIndvAmnno = dataParts[5]; // 데이터에서 개체번호 가져오기

                    // 개체번호가 있을 때만 추가 데이터 추가
                    if (!string.IsNullOrEmpty(sraIndvAmnno))
                    {
                        // 개체번호와 일치하는 EpdValue 객체 찾기
                        EpdValue matchedEpdValue = epdlist.FirstOrDefault(epd => epd.SRA_INDV_AMNNO == sraIndvAmnno);

                        // 개체번호와 일치하는 EpdValue가 있을 경우에만 추가 데이터 추가
                        if (matchedEpdValue != null)
                        {
                            string temp = "";
                            temp = dataItem + "|" + matchedEpdValue.EPD_1.ToString() +
                                 "|" + matchedEpdValue.EPD_2.ToString() +
                                  "|" + matchedEpdValue.EPD_3.ToString() +
                                   "|" + matchedEpdValue.EPD_4.ToString() +
                                    "|" + matchedEpdValue.AUC_PRG_SQ.ToString();

                            newDataList.Add(temp);
                        }
                        else
                        {
                            newDataList.Add(dataItem);
                        }
                    }
                }
            }


            return newDataList;
        }

        public string JoinEpdnDataSV(string cowData, List<EpdValue> epdlist)
        {
            if (cowData != null && epdlist.Count >0)
            {
                string[] dataParts = cowData.Split('|');
                string sraIndvAmnno = dataParts[5]; // 데이터에서 개체번호 가져오기

                // 개체번호가 있을 때만 추가 데이터 추가
                if (!string.IsNullOrEmpty(sraIndvAmnno))
                {
                    // 개체번호와 일치하는 EpdValue 객체 찾기
                    EpdValue matchedEpdValue = epdlist.FirstOrDefault(epd => epd.SRA_INDV_AMNNO == sraIndvAmnno);

                    // 개체번호와 일치하는 EpdValue가 있을 경우에만 추가 데이터 추가
                    if (matchedEpdValue != null)
                    {
                        string temp = "";
                        temp = cowData + "|" + matchedEpdValue.EPD_1.ToString() +
                             "|" + matchedEpdValue.EPD_2.ToString() +
                              "|" + matchedEpdValue.EPD_3.ToString() +
                               "|" + matchedEpdValue.EPD_4.ToString() +
                                "|" + matchedEpdValue.AUC_PRG_SQ.ToString();

                        return temp;
                    }
                }

            }
            return cowData;

        }

        public gValues InsertEPDValue(gValues gv, string[] data)  //AuctionList
        {
            //EPD값 개체
            if (data.Length > 41 && data[41].Length > 0)
            {
                if (data[0].ToUpper().Equals("SV"))
                {
                    gv.bodyWeightInColdNum = data[40] != null ? data[40] : "";//냉도에서의 체중
                    gv.bodyWeightInColdString = data[39] != null ? data[39] : "";
                    gv.longestMuscleCrossSectionNum = data[42] != null ? data[42] : ""; //근육 최장 단면적
                    gv.longestMuscleCrossSectionString = data[41] != null ? data[41] : "";
                    gv.fatThicknessOnBackNum = data[44] != null ? data[44] : ""; //등지방 두께
                    gv.fatThicknessOnBackString = data[43] != null ? data[43] : "";
                    gv.intramuscularFatContentNum = data[46] != null ? data[46] : ""; //근내지방 함량
                    gv.intramuscularFatContentString = data[45] != null ? data[45] : "";
                }
                else
                {
                    gv.bodyWeightInColdNum = data[42] != null ? data[42] : "";//냉도에서의 체중
                    gv.bodyWeightInColdString = data[41] != null ? data[41] : "";
                    gv.longestMuscleCrossSectionNum = data[44] != null ? data[44] : ""; //근육 최장 단면적
                    gv.longestMuscleCrossSectionString = data[43] != null ? data[43] : "";
                    gv.fatThicknessOnBackNum = data[46] != null ? data[46] : ""; //등지방 두께
                    gv.fatThicknessOnBackString = data[45] != null ? data[45] : "";
                    gv.intramuscularFatContentNum = data[48] != null ? data[48] : ""; //근내지방 함량
                    gv.intramuscularFatContentString = data[47] != null ? data[47] : "";
                }
                
            }

            return gv;
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
