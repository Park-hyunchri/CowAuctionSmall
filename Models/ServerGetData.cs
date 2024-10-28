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
using System.Windows.Interop;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json.Linq;
using DocumentFormat.OpenXml.Office2016.Drawing.Command;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using DocumentFormat.OpenXml.Office2016.Excel;
using System.Net.NetworkInformation;

namespace CowAuctionSmall.Models
{
    /// <summary>
    /// 서버에서 받아온 데이터를 처리 
    /// 고정적으로 데이터를 받아오고 처리 , 화면에 뿌려줄 객체 선별 후 전달 (메인뷰로 전달)
    /// </summary>
    public class ServerGetData : IDisposable
    {
        private ServerConn _conn;
        private XmlParserCont _xmlParserCont;
        private NettyAsyncMsgProcess _nettyAsync;
        private UserInfo? _userInfo;
        private string? _token;


        // 여기 구간은 시험용으로
        private List<string> mAPIList = new List<string>(); //고정적으로 데이터 온 메시지, 가공되지 않은 String []
        private bool _isSame = true; //_latestAuctionDataList 와 _beforeAuctionDataList 큐 비교용
        private List<EpdValue> epdList = new List<EpdValue>(); // EPD 데이터 처음 한번만 받아옴, 이유는 수정될경우가 없다고 판단
        private bool _isRunning; // 서버 데이터 처리가 실행 중인지 여부, 에러가 생길시 동작 정지

        private Stopwatch stopwatch = new Stopwatch();  // Stopwatch 객체 생성
        private readonly object _lockObj = new object(); // 락 오브젝트 선언

        private List<gValues>? _latestAuctionDataList; //최신 경매 데이터 리스트
        private List<gValues>? _beforeAuctionDataList; //과거 경매 데이터 리스트
        private int _runRunSipNumber = -1 ; // 현재 진행중인 경매 번호 (경매진행중일때 는 다른화면으로 전환 x 하려고) -1을 초기값으로 한 이유는 개발서버에서 0번 경매도 생성가능해서

        private AnimalParseData _animParseData; //데이터 파싱용, 여러군데 써서 따로 빼둠
        //

        private readonly WeakReferenceMessenger _messenger;
        private readonly WeakReferenceMessenger _messenger8007;         //경매 종료시 , 메시지를 보내는 곳(NettyAsyncMsgProcess)
        private readonly WeakReferenceMessenger _messengerStringArr;    //스페이스바 누를때, 메시지를 보내는 곳(NettyAsyncMsgProcess)
        private readonly WeakReferenceMessenger _messengerStArrAF_SD;   //단일 유찰시, 메시지를 보내는 곳(NettyAsyncMsgProcess)


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

            _messengerStArrAF_SD = WeakReferenceMessenger.Default;
            _messengerStArrAF_SD.Register<DataToServerGetAF_SD>(this, OnStringArrAF_SD);

            //시험용
            _latestAuctionDataList = new List<gValues>();//최신 경매 데이터 리스트

            //
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
            await WaitForNetwork();
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

        private async Task WaitForNetwork()
        {
            while (!NetworkInterface.GetIsNetworkAvailable())
            {
                Debug.WriteLine("네트워크 연결 대기 중...(인터넷 연결이 되어 있는지 확인)");
                logger.LogInfo("네트워크 연결 대기 중...(인터넷 연결이 되어 있는지 확인)");
                await Task.Delay(5000); // 5초 대기 후 다시 확인
            }
        }

        private bool runProcessMessageAsync = true;
        private bool firstSetup = true; // 주기적으로 네티 서버와 연결을 시도후 상태에 따라 메인화면 하단에 메시지 전달
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
                            await Task.Delay(800); // 0.8초 대기
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
                                    if (_userInfo != null && _token != null)
                                    {
                                        _conn.NettyComm(_userInfo, _token);
                                    }
                                    logger.LogInfo("네티 연결 시도 중...");
                                }
                                else
                                {
                                    // 연결이 성공하면 firstSetup을 false로 설정
                                    firstSetup = false;
                                    logger.LogInfo("네티 연결 성공!");
                                }

                                logger.LogInfo("네티 연결상태 : " + isActiveNetty);
                                if (isActiveNetty ==false)
                                {
                                    WeakReferenceMessenger.Default.Send(new DataStringMessage("서버 연결 실패"));
                                }
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
        private DateTime _now = DateTime.Now;
        private async Task ProcessMessageAsync()
        {
            await _semaphore.WaitAsync(); // 세마포어 진입
            try
            {
                List<gValues> currentSyncList = new List<gValues>();

                try
                {
                    //시작 전 싹 지우고 시작
                    if (mAPIList != null && _latestAuctionDataList !=null)
                    {
                        mAPIList.Clear();
                        if (DateTime.Now.Minute % 10 == 0)
                        {
                            epdList.Clear();
                        }
                        
                        _latestAuctionDataList.Clear();
                    }

                    // 매초마다 서버에서 받아오는 목록들 List<string> 
                    if (_userInfo != null && _token != null)
                    {
                        mAPIList = await _conn.SvInfoRequest(_userInfo, _token);
                    }

                    if (mAPIList != null && mAPIList.Count > 0) //서버에서 온 데이터와 EPD 데이터 합치기
                    {
                        if (epdList.Count==0)
                        {
                            epdList = await _conn.GetCurrentInfoEPD(_userInfo, _token);
                        }
                        
                        mAPIList = _conn.JoinEpdnData(mAPIList, epdList);

                        Parallel.ForEach(mAPIList, message =>
                        {
                            var gv = _animParseData.Parse_PacketApi(message, _userInfo, _conn);
                            // 추가 처리
                            lock (_lockObj)
                            {
                                _latestAuctionDataList.Add(gv);
                            }
                        });
                        _latestAuctionDataList = _latestAuctionDataList.OrderBy(x => x.EntityNumber).ToList();
                    }

                    //============================== 시 작 =================================================
                    if (_beforeAuctionDataList==null) //처음 시작
                    {
                        // 새 복사 생성
                        _beforeAuctionDataList = new List<gValues>(_latestAuctionDataList.Count);
                        _beforeAuctionDataList.AddRange(_latestAuctionDataList);
                        InsertDatas(_beforeAuctionDataList);
                        _beforeAuctionDataList = _beforeAuctionDataList.OrderBy(x => x.EntityNumber).ToList();
                        return;
                    }

                    if (_latestAuctionDataList.Count < _beforeAuctionDataList.Count) //서버에서 온 데이터가 줄어들었을때 즉, 데이터 삭제됨
                    {
                        // HashSet을 사용한 예시
                        var latestAuctionDataSet = new HashSet<gValues>(_latestAuctionDataList, new EntityNumberComparer());
                        var removedItems = _beforeAuctionDataList
                                            .Where(item => !latestAuctionDataSet.Contains(item))
                                            .ToList();

                        DataDeletes(removedItems);

                    }
                    else if (_latestAuctionDataList.Count > _beforeAuctionDataList.Count) // 서버에서 온 데이터가 늘어났을 때
                    {
                        var addedItems = _latestAuctionDataList
                                            .Where(item => !_beforeAuctionDataList.Contains(item))
                                            .ToList();
                        InsertDatas(addedItems);//추가된 데이터
                    }
                    else //서버에서 온 데이터가 정보가 그대로 유지되었거나 또는 개체의 데이터가 변경되었을때
                    {

                        _isSame = _beforeAuctionDataList.SequenceEqual(_latestAuctionDataList);
                        if (_isSame)
                        {
                            //Debug.WriteLine("두 값이 같음 =======");
                            return;
                        }
                        else
                        {
                            Debug.WriteLine("두 값이 틀림 =======");
                            ModifiedData(_latestAuctionDataList, _beforeAuctionDataList); // 값이 변경된 데이터 처리
                        }
                    }

                    // _beforeAuctionDataList 업데이트
                    _beforeAuctionDataList = new List<gValues>(_latestAuctionDataList.Count);
                    _beforeAuctionDataList.AddRange(_latestAuctionDataList);
                    _beforeAuctionDataList = _beforeAuctionDataList.OrderBy(x => x.EntityNumber).ToList();
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


        private void DataDeletes(List<gValues> deleteDatas)
        {
            //DisplaySelect 11(대기),22(낙찰),23(유찰)이 아니면 로고로 대체
            foreach (var item in deleteDatas)
            {
                item.AuctionResultStatus = "00";
            }

            Debug.WriteLine("삭제된 데이터 갯수 : " + deleteDatas.Count);
            // MainWindowViewModel 쪽으로 데이터 전달
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(deleteDatas));
        }

        private void InsertDatas(List<gValues> insertDatas)
        {
            Debug.WriteLine("추가된 데이터 갯수 : " + insertDatas.Count);
            // MainWindowViewModel 쪽으로 데이터 전달
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(insertDatas));
        }

        //값이 변경된 데이터 처리
        private void ModifiedData(List<gValues> newAPIList, List<gValues> oldAPIList)
        {

            // 1. 개체번호,계류대 위치는 동일한데, 다른 정보가 변경된 경우
            // 2. 개체번호 동일,계류대 위치가 다른 경우

            // 개체번호,계류대 위치는 동일한데, 다른 정보가 변경된 경우
            var o1 = newAPIList
                                .Where(latest =>
                                    oldAPIList.Any(before =>
                                        before.EntityNumber == latest.EntityNumber && before.SpaceIndex == latest.SpaceIndex &&
                                        !before.Equals(latest)))
                                .ToList();

            //개체번호 동일,계류대 위치가 다른 경우
            var o2 = newAPIList
                                .Where(latest =>
                                    oldAPIList.Any(before =>
                                        before.EntityNumber == latest.EntityNumber && before.SpaceIndex != latest.SpaceIndex))
                                .ToList();

            Debug.WriteLine($"수정된 데이터 갯수 o1 : {o1.Count}    ,    o2 : {o2.Count}");

            //수정된 데이터가 있는데 그 번호가 현재 경매진행 중이라면 수정된 데이터는 무시
            //어쩌피 경매가 끝나고 다시 데이터를 받게 될때는 그 번호가 경매진행이 아니니 수정할거임
            if (o1.Count==1 && o1[0].SipNumber.Equals(_runRunSipNumber.ToString()))
            {
                Debug.WriteLine($"경매번호 : {o1[0].SipNumber} 경매 진행중");
                return;
            }

            // 개체번호,계류대 위치는 동일한데, 다른 정보가 변경된 경우, 어미, 산차, 비고 등
            if (o1.Count > 0)
            {
                WeakReferenceMessenger.Default.Send(new DataChangedMessage(o1));
            }

            //개체번호 동일,계류대 위치가 다른 경우
            if (o2.Count > 0)
            {
                //계류대 위치가 바뀌었음으로 , 기존 구역은 로고로 대체, 새로운 구역은 새로운 데이터로 대체, *2배를 해야함, 중복제거도 포함

                //기존 계류대 위치
                var beforeSpaceIndex = oldAPIList
                                    .Where(before =>
                                        o2.Any(newSpaceInx =>
                                        newSpaceInx.EntityNumber == before.EntityNumber))
                                    .ToList();
                Debug.WriteLine("\n계류대 위치가 바뀐 데이터 갯수 : " + beforeSpaceIndex.Count);
                for(int i=0; i<o2.Count; i++)
                {
                    Debug.WriteLine($"원래 계류대 위치 : {beforeSpaceIndex[i].SpaceIndex}\n바뀐 계류대 위치 :  {o2[i].SpaceIndex}");
                }

                // 로고 데이터 추가
                var logoItems = beforeSpaceIndex.Select(item => new gValues
                {
                    EntityNumber = item.EntityNumber,
                    SpaceIndex = item.SpaceIndex,
                    AuctionResultStatus = "00" // 로고로 대체
                }).ToList();

                // 로고 데이터와 새로 바뀐 데이터 추가 및 중복제거
                var combinedList = logoItems.Concat(o2).Distinct().ToList();

                Debug.WriteLine("최종 갯수 : " + combinedList.Count);
                WeakReferenceMessenger.Default.Send(new DataChangedMessage(combinedList));
            }

        }



        /// <summary>
        /// 경매 스페이스바 눌렀을때
        /// 경매방식, 코드(AS,SV) , 경매번호, 현재가격 , 경매상태
        /// </summary>

        //유찰 : AS|8808990657202|2|329|260|0|8006||||1|-1|0|0
        //       AS|8808990657202|2|329|260|0|8006||||4|-4|0|0
        //       AS|8808990657202|2|329|260|0|8006||||5|-5|0|0
        // 낙찰 :AS|8808990657202|2|329|260|0|8006||||6|-6|0|0

        //단일 일때 찐유찰, 낙찰신호
        //AF|8808990657202|2|23|||0
        //AF|8808990657202|2|22|1636|444|272

        private void OnStringArrMsg(object recipient, DataToServerGetArrMsg message)
        {
            List<gValues> currentSyncList = new List<gValues>();
            string msgString = string.Join(", ", message.Data);
            Debug.WriteLine("스페이스바 땡 누름 : " + msgString);

            string code = message.Data[1]; //AS,SV [1]

            if (message.Data[0].Equals("20") && _beforeAuctionDataList != null) //단일 경매
            {
                switch (code)
                {
                    case "AS":
                        string autctionState = null;
                        if (message.Data.Length ==5)
                        {
                            autctionState = message.Data[4]; //20 AS 7 240 8004 (단일/일괄 , 코드 , 경매번호 , 8001~8006)
                        }
                        else // Length == 4
                        {
                            //새고로침 20 AS 8001 refresh
                            autctionState = message.Data[2]; //20 AS 7 8004 (단일/일괄 , 코드 , 경매번호 , 8001~8006)
                        }

                        lock (_lockObj)
                        {
                            switch (autctionState)
                            {
                                case "8006":// 경매 종료
                                    Debug.WriteLine($"==================================== 경매 종료 {message.Data.ToString()}");
                                    var resultCow = _beforeAuctionDataList.FirstOrDefault(cow => cow.SipNumber == _runRunSipNumber.ToString());
                                    _runRunSipNumber = -1; // 진행중인 경매번호 초기화.
                                    break;
                                case "8004": //경매 진행

                                    if (message.Data[3].Equals("refresh") == false)
                                    {
                                        var tempList = _beforeAuctionDataList.Where(item => item.SipNumber == message.Data[2]); //경매번호만 같은거

                                        // 해당 개체번호를 출력
                                        foreach (var cowAS in tempList)
                                        {
                                            if (cowAS.LowestPrice.Equals(message.Data[3]))
                                            {
                                                cowAS.AuctionResultStatus = !cowAS.AuctionResultStatus.Equals("11") ? "11" : cowAS.AuctionResultStatus;
                                                _runRunSipNumber = int.Parse(cowAS.SipNumber);
                                                cowAS.IsRunning = true;
                                            }
                                            else
                                            {
                                                Debug.WriteLine("가격이 다름\n" + cowAS.toString() + "\n" + message.Data[3]);
                                                cowAS.IsRunning = false;
                                            }

                                            _runRunSipNumber = int.Parse(cowAS.SipNumber);
                                            currentSyncList.Add(cowAS);
                                        }

                                        WeakReferenceMessenger.Default.Send(new DataChangedMessage(currentSyncList));
                                    }
                                    break;

                                case "8001":
                                    if (message.Data[3].Equals("refresh"))
                                    {
                                        Debug.WriteLine("***************************************************새로고침 시작 신호***************************************************");
                                        WeakReferenceMessenger.Default.Send(new DataStringMessage("새로고침 시작 신호"));
                                        _startRefesh = true;
                                    }
                                        
                                    break;
                            }
                            break;
                        }

                    case "SV": // 새로고침할 출품 , 단발 건으로 경매프로그램에서 지정한 날짜에 갯수 만큼 호출되어짐

                        if(_userInfo.Auction.IsGoatAuction.ToUpper().Equals("N"))
                        {
                            return;
                        }
                        gValues cowSV = new gValues();
                        string msgString2 = string.Join("|", message.Data.Skip(1));
                        msgString2 = _conn.JoinEpdnDataSV(msgString2,epdList);

                        cowSV = _animParseData.Parse_PacketApi(msgString2, _userInfo, _conn);
                        Debug.WriteLine("SV : " + msgString2);

                        // 기존 목록에서 EntityNumber가 같은 항목을 찾습니다.
                        var existingItem = _beforeAuctionDataList.FirstOrDefault(item => item.SpaceIndex == cowSV.SpaceIndex);

                        if (existingItem != null)
                        {
                            //예정가 낮추기로 인한 최저가 변경
                            existingItem.LowestPrice = cowSV.LowestPrice;
                        }

                        // 새로고침 신호가 왔을때 (대량 새로고침)
                        if (_startRefesh == true)
                        {
                            AddRefrechList(cowSV);
                        }
                        else
                        {
                            // 1개만 새로고침 , 주로 예정가 낮추기를 눌렀을때 발생
                            WeakReferenceMessenger.Default.Send(new DataChangedMessage(new List<gValues>() { existingItem }));
                        }
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
                        if (message.Data[2].Equals("8004") && _beforeAuctionDataList != null) //경매 진행상태라면
                        {
                            var tempList = _beforeAuctionDataList.Where(item => item.AuctionResultStatus.Equals("11")); //경매 진행중인것만
                            foreach (gValues cow in tempList)
                            {
                                currentSyncList.Add(cow);
                            }

                            WeakReferenceMessenger.Default.Send(new DataChangedMessage(currentSyncList));
                        }
                        break;
                    case "SZ":
                            //`SZ|8808990657202|20240906|0|1|P|1|900`
                            //서버에서 보내주는 데이터에는 날짜가 있어, 경매 전날에 데이터를 확인하려면 이 방법으로 확인 할수 밖에 없다...?
                            Debug.WriteLine("일괄경매 경매대상 표시 표시"); 
                            foreach (var item in _beforeAuctionDataList)
                            {
                                item.AuctionResultStatus = "00";
                            }
                            WeakReferenceMessenger.Default.Send(new DisplaySelectRefresh("Refresh"));
                            WeakReferenceMessenger.Default.Send(new DataChangedMessage(_beforeAuctionDataList)); //로고로 대체
                            _beforeAuctionDataList.Clear();
                        break;
                    default:
                        Debug.WriteLine("OnStringArrMsg 에서 code값이 AS,SV 이외의 {0} 값이 나옴 {1}", code, message.Data.ToString());
                        break;
                }
            }
        }


        private List<gValues> _refreshAuchList = new List<gValues>();
        private Timer? _refreshTimer;
        private bool _refreshTRunning = false;  // 타이머가 실행 중인지 확인하는 플래그
        private bool _startRefesh = false;
        //새로고침 신호가 오면 호출될 함수 (단일경매 대량의 SV)
        private void AddRefrechList(gValues item)
        {
            // 항목을 리스트에 추가
            lock (_lockObj)
            {
                _refreshAuchList.Add(item);

                // 타이머가 실행 중이면 더 이상 타이머를 추가하지 않음
                if (!_refreshTRunning)
                {
                    _refreshTRunning = true; // 타이머 시작을 표시
                    Debug.WriteLine("타이머 시작됨: 10초 후 실행");

                    // 타이머 시작 (10초 후 단발성 실행)
                    _refreshTimer = new Timer(OnTimerElapsed, null, 10000, Timeout.Infinite);

                    
                }
                else
                {
                    Debug.WriteLine("타이머가 이미 실행 중입니다. 추가된 항목은 현재 타이머 완료 후 처리됩니다.");
                }
            }

                
            //10초 뒤 실행됨
        }

        // 타이머가 10초 후 실행할 작업
        private void OnTimerElapsed(object state)
        {
            lock (_lockObj)
            {
                if (_refreshAuchList.Count > 0)
                {
                    // 데이터 전달 및 리스트 초기화
                    Debug.WriteLine("새로고침 할 총 갯수 : "+ _refreshAuchList.Count);
                    WeakReferenceMessenger.Default.Send(new DataChangedMessage(_refreshAuchList));
                    _refreshAuchList.Clear();
                    Debug.WriteLine("새로고침 실행 완료 및 리스트 초기화");
                }
            }

            // 타이머를 종료하고, 실행 중 플래그를 리셋
            _refreshTimer?.Dispose();
            _refreshTimer = null;
            _refreshTRunning = false; // 타이머가 종료되었으므로 플래그 리셋
            _startRefesh = false;
            Debug.WriteLine("***************************************************새로고침 완료***************************************************");
            //메인뷰로 날림 메인뷰 맨밑의 TextBox 
            WeakReferenceMessenger.Default.Send(new DataStringMessage("새로고침 완료"));
            //
            WeakReferenceMessenger.Default.Send(new DisplaySelectRefresh("Refresh"));
        }


        private void OnStringMsg(object recipient, DataToServerGetMsg message)
        {
            if (message.Data.Equals("F"))
            {
                _runRunSipNumber = -1;
                return;
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

        //단일 유찰일 경우 (3번취소) AF|8808990657202|3|24|||0:
        private void OnStringArrAF_SD(object recipient, DataToServerGetAF_SD message)
        {
            string[] msg = message.Data;
            string code = msg[0];

            switch(code)
            {
                case "AF":
                    Debug.WriteLine("AF 온 값 : "+msg.ToString);
                    if (msg[3].Equals("24") || msg[3].Equals("23"))
                    {
                        lock(_lockObj) // 경매 시작후 바로 Esc를 누르면 화면이 안바뀌는 현상 씹히는 현상이 있음 그래서 lock을 걸어줌 근데 개선은 됬지만 완벽한 해결은 아님
                        {
                            if(_beforeAuctionDataList.Count > 0) //경매 날짜가 오늘인경우에만 _beforeAuctionDataList가 존재함
                            {
                                //ESC 취소 버튼 누름
                                var caceledCow = _beforeAuctionDataList.FirstOrDefault(item => item.SipNumber == msg[2]);
                                caceledCow.IsRunning = false;
                                logger.Equals("취소된 경매번호 : " + caceledCow.SipNumber);
                                WeakReferenceMessenger.Default.Send(new DataChangedMessage(new List<gValues>() { caceledCow }));
                            }
                            else //경매 날짜가 오늘이 아닌경우 즉, 다른날짜의 경매 데이터를 보고 싶은 경우
                            {
                                return;
                            }
                            
                        }
                        
                    }
                    break;
                case "SD":
                    Debug.WriteLine("SD 온 값 : " + msg.ToString);
                    break;

                default:
                    break;

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

        /*
         두 값이 같음 =======
[2024-09-25 오전 10:40:53] 88개의 API 경매 정보가 수신되었습니다.

두 값이 같음 =======
[2024-09-25 오전 10:40:53]MSG>>AS|8808990657202|3|329|70|0|8002||||29|-29|0|0
AS|8808990657202|3|329|70|0|8002||||29|-29|0|0: OnCurrentAuctionData 에서 호출         {0} 
 
OnStringArrMsg OnStringArrMsg OnStringArrMsg
data array has less than 7 elements!
[2024-09-25 오전 10:40:53]MSG>>AS|8808990657202|3|329|70|0|8003||||29|-29|0|0
AS|8808990657202|3|329|70|0|8003||||29|-29|0|0: OnCurrentAuctionData 에서 호출         {0} 
 
OnStringArrMsg OnStringArrMsg OnStringArrMsg
data array has less than 7 elements!
[2024-09-25 오전 10:40:53]MSG>>AS|8808990657202|3|329|70|0|8004||||29|-29|0|0
AS|8808990657202|3|329|70|0|8004||||29|-29|0|0: OnCurrentAuctionData 에서 호출         {0} 
 
OnStringArrMsg OnStringArrMsg OnStringArrMsg
스페이스바 땡 누름 : 20, AS, 3, 70, 8004
------------ 1
진행중 화면 3
진행중 화면 3
[2024-09-25 오전 10:40:54] 88개의 API 경매 정보가 수신되었습니다.

두 값이 틀림 =======
수정된 데이터 갯수 o1 : 1    ,    o2 : 0
경매번호 : 3 경매 진행중
[2024-09-25 오전 10:40:55] 88개의 API 경매 정보가 수신되었습니다.

두 값이 같음 =======
[2024-09-25 오전 10:40:55]MSG>>SD|8808990657202|F|-1
SD|8808990657202|F|-1: OnCurrentAuctionData 에서 호출         {0} 
 
OnStringArrMsg OnStringArrMsg OnStringArrMsg
[2024-09-25 오전 10:40:55]MSG>>AF|8808990657202|3|24|||0
AF|8808990657202|3|24|||0: OnCurrentAuctionData 에서 호출         {0} 
 
OnStringArrMsg OnStringArrMsg OnStringArrMsg
[2024-09-25 오전 10:40:55]MSG>>AS|8808990657202|3|329|70|0|8002||||30|-30|0|0
AS|8808990657202|3|329|70|0|8002||||30|-30|0|0: OnCurrentAuctionData 에서 호출         {0} 
 
OnStringArrMsg OnStringArrMsg OnStringArrMsg
data array has less than 7 elements!
[2024-09-25 오전 10:40:56] 88개의 API 경매 정보가 수신되었습니다.

두 값이 같음 =======
         */
    }
}
