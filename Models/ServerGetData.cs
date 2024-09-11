using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.Models.XMLParser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using UserInfo = CowAuctionSmall.Models.Structures.UserInfo;
using static CowAuctionSmall.Models.Structures.AuctionStatus;
using System.Threading;
using System.Windows.Shell;
using CowAuctionSmall.NetProto.netty;

namespace CowAuctionSmall.Models
{
    /// <summary>
    /// 서버에서 받아온 데이터를 처리
    /// </summary>
    public class ServerGetData : IDisposable
    {
        private ServerConn _conn;
        private XmlParserCont _xmlParserCont;
        private NettyAsyncMsgProcess _nettyAsync;
        private UserInfo _userInfo;
        private String _token;

        private bool _isRunning;

        private List<string> mAPIList = new List<string>(); //고정적으로 데이터 온 메시지
        private List<string> beforemAPIList;
        private bool _isSame = true; //mAPIList 와 beforemAPIList 큐 비교용

        private List<EpdValue> epdList = new List<EpdValue>();

        public ConcurrentQueue<string> mNetMessageList = new ConcurrentQueue<string>();// 비동기적 메시지

        private readonly WeakReferenceMessenger _messenger;
        private readonly WeakReferenceMessenger _messenger8007;
        private readonly WeakReferenceMessenger _messengerStringArr;

        private AnimalParseData _animParseData;

        private NLogger logger;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param> 서버 접속관련
        /// <param name="xmlParserCont"></param> XML 파싱
        /// <param name="nettyAsyncMsg"></param> 비동기적으로 오는 메시지처리(경매 프로그램에 의한 비동기적 메시지)
        public ServerGetData(ServerConn conn, XmlParserCont xmlParserCont, NettyAsyncMsgProcess nettyAsyncMsg)
        {
            logger = NLogger.Instance;
            _conn = conn;
            _xmlParserCont = xmlParserCont;
            _nettyAsync = nettyAsyncMsg;

            _animParseData = new AnimalParseData();

            _messenger = WeakReferenceMessenger.Default;
            _messenger.Register<DataToServerGetMsg>(this, OnStringMsg);

            _messengerStringArr = WeakReferenceMessenger.Default;
            _messengerStringArr.Register<DataToServerGetArrMsg>(this, OnStringArrMsg);

            _messenger8007 = WeakReferenceMessenger.Default;
            _messenger8007.Register<DataStringMessage8007>(this, OnStringMsg8007);
            

            Task.Run(()=> init()) ;
        }

        private async Task init()
        {
            var result = _xmlParserCont.XmlPaserResult();
            _userInfo = result.userInfo;
            if (_userInfo == null)
            {
                logger.LogError("init: _userInfo가 null입니다.");
                return;
            }

            string tempToken = await _conn.IssueTocken(_userInfo);
            if (tempToken != null)
            {
                _token = tempToken;
                await StartAsync();
            }
            else
            {
                logger.LogError("init: await _conn.IssueTocken(_userInfo); 부분 null 반환");
            }
        }

        private bool runProcessMessageAsync = true;
        private bool firstSetup = true;
        public async Task StartAsync()
        {
            _isRunning = true;
            var firstSetupResetTimer = new System.Timers.Timer(30 * 1000); // 30초 타이머
            firstSetupResetTimer.Elapsed += (sender, e) => firstSetup = true; // 30초마다 firstSetup을 true로 초기화
            firstSetupResetTimer.Start();

            await Task.Run(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        if (runProcessMessageAsync)
                        {
                            await ProcessMessageAsync();
                            await Task.Delay(1000); // 1초 대기
                        }

                        if (firstSetup)
                        {
                            var currentTime = DateTime.Now;
                            if (currentTime.Hour < 2 || currentTime.Hour >= 4)
                            {
                                await Task.Delay(1000); // 1초 대기
                                bool isActiveNetty = AuctionDelegate.getInstance().isActive();

                                if (!isActiveNetty)
                                {
                                    // 네티 연결이 활성화되지 않았을 경우, 즉시 재연결 시도
                                    _conn.NettyComm(_userInfo, _token);
                                    logger.LogInfo("네티 연결 시도 중...");
                                }
                                else
                                {
                                    // 연결이 성공하면 firstSetup을 false로 설정
                                    firstSetup = false;
                                    logger.LogInfo("네티 연결 성공!");
                                }

                                logger.LogInfo("네티 연결상태 : " + isActiveNetty);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 예외 처리: 네티 연결 실패 시 5초 후 재시도
                        Debug.WriteLine("ProcessMessageAsync에서 예외 발생: " + ex.Message);
                        logger.LogError("ProcessMessageAsync에서 예외 발생 : " + ex.Message);
                        await Task.Delay(5000); // 5초 후 다시 시도
                    }
                }
            });

            firstSetupResetTimer.Stop();
            firstSetupResetTimer.Dispose();
        }



        // 2024-07-13 수정된 내용: StopAsync 함수를 추가하여 비동기 작업을 중단
        public async Task StopAsync()
        {
            _isRunning = false;
            await Task.Delay(1000); // 비동기 작업이 안전하게 종료될 시간을 준다.
        }

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // SemaphoreSlim 초기화
        private bool _IsDataDelete = false;
        private async Task ProcessMessageAsync()
        {
            await _semaphore.WaitAsync(); // 세마포어 진입
            try
            {
                List<gValues> currentSyncList = new List<gValues>();
                ConcurrentQueue<string>? differenceQueue = null; // 변경할 데이터 목록
                ConcurrentQueue<string>? logoQueue = null; // 기존 표출 항목에서 변경 후 로고 화면을 띄워야 하는 경우

                try
                {
                    //시작 전 싹 지우고 시작
                    string message = "";
                    if (mAPIList != null)
                    {
                        mAPIList.Clear();
                    }

                    // 매초마다 서버에서 받아오는 목록들 List<string> 
                    mAPIList = await _conn.SvInfoRequest(_userInfo, _token);

                    if (epdList != null || epdList.Count > 0) //서버에서 온 데이터와 EPD 데이터 합치기
                    {
                        mAPIList = _conn.JoinEpdnData(mAPIList, epdList);
                    }

                    //============================== 시 작 =================================================
                    if (beforemAPIList == null || beforemAPIList.Count <= 0) //처음 시작
                    {
                        // 새 ConcurrentQueue 생성
                        beforemAPIList = new List<string>(mAPIList.Select(x => x));
                        epdList = await _conn.GetCurrentInfoEPD(_userInfo, _token);
                        if (epdList != null)
                        {
                            beforemAPIList = _conn.JoinEpdnData(beforemAPIList, epdList);
                            mAPIList = _conn.JoinEpdnData(mAPIList, epdList);
                        }
                        InsertDatas(new ConcurrentQueue<string>(mAPIList));
                        return;
                    }
                    else if (mAPIList.Count < beforemAPIList.Count) //서버에서 온 데이터가 줄어들었을때 즉, 데이터 삭제됨
                    {
                        differenceQueue = new ConcurrentQueue<string>(beforemAPIList.Except(mAPIList)); //삭제할 데이터
                        DataDeletes(differenceQueue);
                        return;
                    }
                    else if (mAPIList.Count > beforemAPIList.Count) // 서버에서 온 데이터가 늘어났을 때
                    {
                        differenceQueue = new ConcurrentQueue<string>(mAPIList.Except(beforemAPIList)); //추가된 데이터
                        InsertDatas(differenceQueue);
                        return;
                    }
                    else //서버에서 온 데이터가 정보가 그대로 유지되었거나 또는 개체의 데이터가 변경되었을때
                    {

                        _isSame = beforemAPIList.SequenceEqual(mAPIList);
                        if (_isSame)
                        {
                            return;
                        }
                        else
                        {
                            Debug.WriteLine("두 값이 틀림 =======");

                            ModifiedData(new ConcurrentQueue<string>(mAPIList), new ConcurrentQueue<string>(beforemAPIList)); // 변경된 데이터 처리

                        }
                    }
                    // beforemAPIList 업데이트
                    beforemAPIList = new List<string>(mAPIList.Select(x => x));
                }
                catch (Exception ex)
                {
                    logger.LogError("ProcessMessageAsync 상단 예외 발생: " + ex.Message);
                }
            }
            finally
            {
                _semaphore.Release(); // 세마포어 해제
            }
        }


        private void DataDeletes(ConcurrentQueue<string> logoQueue)
        {
            List<gValues> deleteDatas = new List<gValues>();
            if (logoQueue != null && logoQueue.Any())
            {
                var logoItems = logoQueue.ToArray();
                foreach (var logoItem in logoItems)
                {
                    gValues gValues = new gValues
                    {
                        SpaceIndex = logoItem.Split('|')[34],
                        AuctionResultStatus = "00"
                    };
                    deleteDatas.Add(gValues);
                }
            }
            // MainWindowViewModel 쪽으로 데이터 전달
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(deleteDatas));
        }

        private void InsertDatas(ConcurrentQueue<string> differenceQueue)
        {
            List<gValues> insertDatas = new List<gValues>();
            // 안전하게 큐 복사 후 처리
            var differenceItems = differenceQueue.ToArray();
            foreach (var item in differenceItems)
            {
                insertDatas.Add(_animParseData.Parse_PacketApi(item, _userInfo, _conn));
            }
            // MainWindowViewModel 쪽으로 데이터 전달
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(insertDatas));
        }

        private void ModifiedData(ConcurrentQueue<string> newAPIList, ConcurrentQueue<string> oldAPIList)
        {
            List<gValues> syncList = new List<gValues>();

            // 계류대 번호가 같은 항목을 Dictionary로 변환
            var oldItemDict = oldAPIList.ToDictionary(item => item.Split('|')[5], item => item);
            var newItemDict = newAPIList.ToDictionary(item => item.Split('|')[5], item => item);

            // new와 old의 계류대 번호가 동일하지만 다른 데이터를 가진 항목을 찾음
            var changedItems = newItemDict
                .Where(newItem => oldItemDict.ContainsKey(newItem.Key) && oldItemDict[newItem.Key] != newItem.Value)
                .Select(newItem => newItem.Value);

            // ConcurrentQueue에 변경된 항목 추가
            var changedItemsQueue = new ConcurrentQueue<string>(changedItems);

            // 여기서 계류대 위치가 바뀐 항목을 찾음 (식별자는 동일하지만, 위치[34]가 다른 경우)
            var changedItemsSpace = newItemDict
                .Where(newItem => oldItemDict.ContainsKey(newItem.Key)
                                && newItem.Value.Split('|')[34] != oldItemDict[newItem.Key].Split('|')[34])
                .Select(newItem => newItem.Value);

            // 여기서 계류대 위치가 바뀐 항목을 찾음 logoQueue 기존데이터 기준 즉, 로고가 띄워질 데이터
            var logoQueue = oldItemDict
                .Where(oldItem => newItemDict.ContainsKey(oldItem.Key)
                                && oldItem.Value.Split('|')[34] != newItemDict[oldItem.Key].Split('|')[34])
                .Select(oldItem => oldItem.Value);

            if (changedItems.Count() > 0 && changedItemsSpace.Count() == 0) //순번이 안 바뀐경우 비고 , 산차, 어미 등 정보만 바뀌었을 경우
            {
                // 안전하게 큐 복사 후 처리
                var copyItems = changedItems.ToArray();
                foreach (var item in copyItems)
                {
                    syncList.Add(_animParseData.Parse_PacketApi(item, _userInfo, _conn));
                }
                // MainWindowViewModel 쪽으로 데이터 전달
                WeakReferenceMessenger.Default.Send(new DataChangedMessage(syncList));
                syncList.Clear();
                return;
            }
            else if (changedItemsSpace.Count() > 0) //순번이 바뀐경우
            {
                // 안전하게 큐 복사 후 처리
                var copyItems = changedItemsSpace.ToArray();
                foreach (var item in copyItems)
                {
                    syncList.Add(_animParseData.Parse_PacketApi(item, _userInfo, _conn));
                }

                var logoItems = logoQueue.ToArray();
                foreach (var logoItem in logoItems)
                {
                    gValues gValues = new gValues
                    {
                        SpaceIndex = logoItem.Split('|')[34],
                        AuctionResultStatus = "00"
                    };

                    // 중복 제거 (같은 이름의 거치대 숫자 제거)
                    bool isDuplicate = syncList.Any(cow => cow.SpaceIndex == gValues.SpaceIndex);
                    if (!isDuplicate)
                    {
                        syncList.Add(gValues);
                    }
                    else if (syncList.Count == 1)
                    {
                        syncList.Add(gValues);
                    }
                }

                // MainWindowViewModel 쪽으로 데이터 전달
                WeakReferenceMessenger.Default.Send(new DataChangedMessage(syncList));
                syncList.Clear();
                return;
            }
        }



        /// <summary>
        /// 경매 스페이스바 눌렀을때
        /// 경매방식, 코드(AS,SV) , 경매번호, 현재가격 , 경매상태
        /// </summary>
        private void OnStringArrMsg(object recipient, DataToServerGetArrMsg message)
        {
            List<gValues> currentSyncList = new List<gValues>();
            gValues cow = null;
            string msgString = string.Join(", ", message.Data);
            Debug.WriteLine("스페이스바 땡 누름 : " + msgString);

            string code = message.Data[1];

            if (message.Data[0].Equals("20") && beforemAPIList != null) //단일 경매
            {
                switch (code)
                {
                    case "AS":
                        
                        var tempList = beforemAPIList.Where(item => item.Split('|')[2] == message.Data[2]); //경매번호만 같은거

                        // 해당 개체번호를 출력
                        foreach (var item in tempList)
                        {
                            cow = _animParseData.Parse_PacketApi(item, _userInfo, _conn);
                            if (cow.LowestPrice.Equals(message.Data[3]))
                            {
                                cow.AuctionResultStatus = !cow.AuctionResultStatus.Equals("11") ? "11" : cow.AuctionResultStatus;
                                cow.IsRunning = true;
                            }
                            else
                            {
                                Debug.WriteLine("가격이 다름\n" + item + "\n" + message.Data[3]);
                                cow.IsRunning = false;
                            }

                            runningSipNumber = cow.SipNumber;
                            currentSyncList.Add(cow);
                        }

                        WeakReferenceMessenger.Default.Send(new DataChangedMessage(currentSyncList));

                        break;

                    case "SV":

                        if(_userInfo.Auction.IsGoatAuction.ToUpper().Equals("N"))
                        {
                            return;
                        }

                        string msgString2 = string.Join("|", message.Data.Skip(1));
                        msgString2 = _conn.JoinEpdnDataSV(msgString2,epdList);
                        cow = _animParseData.Parse_PacketApi(msgString2, _userInfo, _conn);
                        cow.Code = code;
                        currentSyncList.Add(cow);
                        Debug.WriteLine("SV : " + msgString2);
                        int index = beforemAPIList.FindIndex(item => item.Contains(cow.EntityNumber.Replace(" ","")));

                        if (index != -1) //변경된 최저가 값이 있을경우
                        {
                            string changePriceCow = beforemAPIList[index];

                            // 구분자로 문자열 분할
                            string[] parts = changePriceCow.Split('|');

                            // 특정 위치의 값을 변경
                            int targetIndex = 27; // 최저값 위치
                            parts[targetIndex] = cow.LowestPrice;

                            // 변경된 문자열 생성
                            string modifiedString = string.Join("|", parts);

                            beforemAPIList.RemoveAt(index);
                            beforemAPIList.Insert(index, modifiedString);
                        }
                        // MainWindowViewModel 쪽으로 데이터 전달
                        WeakReferenceMessenger.Default.Send(new DataChangedMessage(currentSyncList));

                        break;
                    default:
                        Debug.WriteLine("OnStringArrMsg 에서 code값이 AS,SV 이외의 {0} 값이 나옴 {1}", code, message.Data.ToString());
                        break;
                }

            }
            else //일괄 경매 방식(msg = 경매방식, 코드, 경매상태)
            {
                switch (code)
                {
                    case "AS":
                        if (message.Data[2].Equals("8004") && beforemAPIList != null) //경매 진행상태라면
                        {
                            var tempList = beforemAPIList.Where(item => item.Split('|')[29].Equals("11")); //경매 진행중인것만
                            foreach (string item in tempList)
                            {
                                cow = _animParseData.Parse_PacketApi(item, _userInfo, _conn);
                                currentSyncList.Add(cow);
                            }

                            WeakReferenceMessenger.Default.Send(new DataChangedMessage(currentSyncList));
                        }
                        break;
                    default:
                        Debug.WriteLine("OnStringArrMsg 에서 code값이 AS,SV 이외의 {0} 값이 나옴 {1}", code, message.Data.ToString());
                        break;
                }
            }
        }
        private string runningSipNumber = null;

        private void OnStringMsg(object recipient, DataToServerGetMsg message)
        {
            List<gValues> currentSyncList = new List<gValues>();

            if (runningSipNumber != null && runningSipNumber.Length > 0)
            {
                var tempList = beforemAPIList.Where(item => item.Split('|')[2] == runningSipNumber); //경매번호만 같은거

                foreach (var item in tempList)
                {
                    currentSyncList.Add(_animParseData.Parse_PacketApi(item, _userInfo, _conn));
                }

                WeakReferenceMessenger.Default.Send(new DataChangedMessage(currentSyncList));
            }
        }

        private void OnStringMsg8007(object recipient, DataStringMessage8007 message)
        {
            if (message.Data.Equals("FINISH")) //회차 종료
            {
                runProcessMessageAsync = false;
                logger.LogInfo("프로그램 회차 종료");
            }
            else
            {
                runProcessMessageAsync = true;
            }

        }

        // 2024-07-13 추가된 Dispose 메소드
        public void Dispose()
        {
            StopAsync().Wait(); // 비동기 작업이 안전하게 종료될 시간을 주기 위해 Wait() 사용
            _messenger.UnregisterAll(this);
            _messengerStringArr.UnregisterAll(this);
            GC.SuppressFinalize(this);
        }


        ~ServerGetData()
        {
            Dispose();
        }
    }
}
