using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.Models.XMLParser;
using CowAuctionSmall.NetProto.netty;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using UserInfo = CowAuctionSmall.Models.Structures.UserInfo;

namespace CowAuctionSmall.Services
{
    /// <summary>
    /// 서버에서 받아온 데이터를 처리 
    /// 고정적으로 데이터를 받아오고 처리 , 화면에 뿌려줄 객체 선별 후 전달 (메인뷰로 전달)
    /// </summary>
    public class ServerGetData : IDisposable
    {
        public static ServerGetData? Instance { get; private set; }

        private ServerConn _conn;
        private XmlParserCont _xmlParserCont;
        private NettyAsyncMsgProcess _nettyAsync;
        private UserInfo? _userInfo;
        private string? _token;


        // 여기 구간은 시험용으로
        private List<string> mAPIList = new List<string>(); //고정적으로 데이터 온 메시지, 가공되지 않은 String []
        private bool _isRunning; // 서버 데이터 처리가 실행 중인지 여부, 에러가 생길시 동작 정지

        private Stopwatch stopwatch = new Stopwatch();  // Stopwatch 객체 생성
        private readonly object _lockObj = new object(); // 락 오브젝트 선언

        public static List<gValues>? _latestAuctionDataList; //최신 경매 데이터 리스트
        private static List<gValues>? _beforeAuctionDataList; //과거 경매 데이터 리스트

        public static int _runRunSipNumber = -1; // 현재 진행중인 경매 번호 (경매진행중일때 는 다른화면으로 전환 x 하려고) -1을 초기값으로 한 이유는 개발서버에서 0번 경매도 생성가능해서
        public static bool _batchRunningState = false; // 일괄 경매 진행중인지 여부

        private AnimalParseData _animParseData; //데이터 파싱용, 여러군데 써서 따로 빼둠
        //
        private string? _currentRefreshDate = null;

        private readonly WeakReferenceMessenger _messenger;    

        private readonly WeakReferenceMessenger _messenger8007;         //경매 종료시 , 메시지를 보내는 곳(NettyAsyncMsgProcess)
        private readonly WeakReferenceMessenger _messengerStringArr;    //스페이스바 누를때, 메시지를 보내는 곳(NettyAsyncMsgProcess)
        private readonly WeakReferenceMessenger _messengerStArrAF_SD;   //단일 유찰시, 메시지를 보내는 곳(NettyAsyncMsgProcess)

        private NLogger logger;

        /// <summary>
        /// 서버 통신과 메시지 핸들러를 초기화한다.
        /// </summary>
        /// <param name="conn"></param> 서버 접속관련
        /// <param name="xmlParserCont"></param> XML 파싱
        /// <param name="nettyAsyncMsg"></param> 비동기적으로 오는 메시지처리(경매 프로그램에 의한 비동기적 메시지)
        public ServerGetData(ServerConn conn, XmlParserCont xmlParserCont, NettyAsyncMsgProcess nettyAsyncMsg)
        {

            Instance = this;

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
            Task.Run(() => init());

        }


        /// <summary>
        /// 초기 설정을 읽고 서버 통신을 시작한다.
        /// </summary>
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
            string tempToken = await _conn.IssueToken(_userInfo);
            if (string.IsNullOrWhiteSpace(tempToken))
            {
                logger.LogError("init: await _conn.IssueToken(_userInfo) 토큰값이 없습니다.");
                return;
            }

            _token = tempToken;
            await StartAsync();
        }

        /// <summary>
        /// 네트워크 연결이 될 때까지 대기한다.
        /// </summary>
        private async Task WaitForNetwork()
        {
            while (!NetworkInterface.GetIsNetworkAvailable())
            {
                Debug.WriteLine("네트워크 연결 대기 중...(인터넷 연결이 되어 있는지 확인)");
                logger.LogInfo("네트워크 연결 대기 중...(인터넷 연결이 되어 있는지 확인)");
                await Task.Delay(2000); // 2초 대기 후 다시 확인
            }
        }

        private bool runProcessMessageAsync = true;
        private bool _timersStoppedByNoAuction = false;
        private bool isConnecting = false; // 중복 연결 방지용 락 변수
        private bool isAuctionDate = false; // 경매일인지 여부
        private static readonly TimeSpan DuplicateRetryCooldown = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan QcnCheckIntervalWhenExists = TimeSpan.FromHours(1);
        private static readonly TimeSpan QcnCheckIntervalWhenMissing = TimeSpan.FromMinutes(10);
        private DateTime _duplicateCooldownUntilUtc = DateTime.MinValue;
        private bool _duplicateCooldownLogged;
        private int _disposeState = 0;
        private string? _qcnCacheDate;
        private Qcn? _cachedQcn;
        private DateTime _nextQcnCheckUtc = DateTime.MinValue;
        // 💡 [추가] API 모니터링 로그 주기적 출력을 위한 시각 기록 변수
        private DateTime _lastApiLogTime = DateTime.MinValue;
        /// <summary>
        /// 주기적으로 서버 데이터를 수신하는 루프를 시작한다.
        /// </summary>
        public async Task StartAsync()
        {
            _isRunning = true;

            await Task.Run(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        if (_userInfo == null || string.IsNullOrWhiteSpace(_token))
                        {
                            logger.LogError("StartAsync: 토큰값이 없습니다.");
                            await Task.Delay(1000);
                            continue;
                        }

                        var currentUser = _userInfo;
                        var currentToken = _token;
                        // 경매가 실제로 진행 중일 때만 1초 주기로 동작하고, 경매 대기 중이거나 새벽 시간대에는 3~10초 주기로 동적으로 변환하도록 수정
                        if (runProcessMessageAsync)
                        {
                            var sw = Stopwatch.StartNew();
                            string date = !string.IsNullOrWhiteSpace(_currentRefreshDate)
                                ? _currentRefreshDate
                                : (string.IsNullOrWhiteSpace(currentUser.CurrentInfo?.Date)
                                    ? DateTime.Today.ToString("yyyyMMdd")
                                    : currentUser.CurrentInfo.Date);

                            if (ShouldRefreshQcn(date))
                            {
                                await RefreshQcnStateAsync(currentUser, currentToken, date);
                            }
                            else if (HasCachedQcn(date))
                            {
                                await EnsureNettyConnectionAsync(currentUser, currentToken);
                            }

                            if (!HasCachedQcn(date))
                            {
                                if (!_timersStoppedByNoAuction)
                                {
                                    logger.LogInfo("******** 오늘 차수데이터가 없습니다 ********");
                                    WeakReferenceMessenger.Default.Send(new DataStringMessage("오늘 차수데이터가 없습니다"));
                                    WeakReferenceMessenger.Default.Send(new DisplaySelectRefresh("StopTimer"));
                                    _timersStoppedByNoAuction = true;
                                }
                                isAuctionDate = false;
                                await Task.Delay(GetDelayUntilNextQcnCheck());
                            }
                            else
                            {
                                if (_timersStoppedByNoAuction)
                                {
                                    WeakReferenceMessenger.Default.Send(new DisplaySelectRefresh("Refresh"));
                                    _timersStoppedByNoAuction = false;
                                }
                                isAuctionDate = true;
                                await ProcessMessageAsync();
                            }

                            sw.Stop();

                            int elapsed = (int)sw.ElapsedMilliseconds;
                            // -------------------------------------------------------------------
                            // 💡 [추가] 10분 간격으로 현재까지 누적 API 호출 건수 NLog 파일에 기록
                            // -------------------------------------------------------------------
                            if ((DateTime.Now - _lastApiLogTime).TotalMinutes >= 10)
                            {
                                _lastApiLogTime = DateTime.Now;
                                logger.LogInfo($"[API 모니터링] 현재까지 누적 API 호출 건수: {ServerConn.TotalApiCount}회");
                            }
                            // -------------------------------------------------------------------
                            // 💡 [안성축협 API 호출 제한 방지] 상황별 동적 폴링 주기 설정
                            // -------------------------------------------------------------------
                            int targetCycle = 1000; // 기본 1초 (1000ms)

                            // 1. 실제 응찰/경매 진행 중인 출품우가 있거나 일괄 경매 진행 중일 때 -> 1초 유지
                            if (_runRunSipNumber != -1 || _batchRunningState)
                            {
                                targetCycle = 1000;
                            }
                            // 2. 새벽 시간대 (04시 ~ 08시 등 경매 개시 전 대기) -> 10초로 완화
                            else if (DateTime.Now.Hour >= 4 && DateTime.Now.Hour < 8)
                            {
                                targetCycle = 10000;
                            }
                            // 3. 경매 당일 대기 상태 (진행 중인 소가 없음) -> 3초로 완화
                            else
                            {
                                targetCycle = 3000;
                            }
                            // -------------------------------------------------------------------

                            int delay = Math.Max(50, targetCycle - elapsed);

                            Debug.WriteLine($"[ProcessMessageAsync] 소요: {elapsed}ms, 대기: {delay}ms, 주기: {targetCycle}ms");

                            await Task.Delay(delay);
                        }
                        else
                        {
                            // 메시지가 없을 때 CPU 점유율을 낮추기 위한 대기
                            await Task.Delay(200);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ProcessMessageAsync에서 예외 발생: " + ex.Message);
                        logger.LogError("ProcessMessageAsync에서 예외 발생 : " + ex.Message);

                        // 에러 발생 시 재시도 전 대기
                        await Task.Delay(5000);

                        // 상태값 리셋 (외부에서 재연결을 유도하거나 루프 조건 제어)
                        isConnecting = false;
                    }
                }
            });
        }

        private bool ShouldRefreshQcn(string date)
        {
            if (string.IsNullOrWhiteSpace(date))
            {
                return true;
            }

            if (!string.Equals(_qcnCacheDate, date, StringComparison.Ordinal))
            {
                return true;
            }

            return DateTime.UtcNow >= _nextQcnCheckUtc;
        }

        private bool HasCachedQcn(string date)
        {
            return string.Equals(_qcnCacheDate, date, StringComparison.Ordinal) && _cachedQcn != null;
        }

        private async Task RefreshQcnStateAsync(UserInfo currentUser, string currentToken, string date)
        {
            await EnsureNettyConnectionAsync(currentUser, currentToken);

            var qcn = await _conn.PostQcn(currentUser, currentToken, date);
            var interval = qcn != null ? QcnCheckIntervalWhenExists : QcnCheckIntervalWhenMissing;

            _qcnCacheDate = date;
            _cachedQcn = qcn;
            _nextQcnCheckUtc = DateTime.UtcNow.Add(interval);

            logger.LogInfo($"RefreshQcnStateAsync: date={date}, hasQcn={qcn != null}, nextCheckInMinutes={interval.TotalMinutes:0}");
        }

        private TimeSpan GetDelayUntilNextQcnCheck()
        {
            if (_nextQcnCheckUtc == DateTime.MinValue)
            {
                return QcnCheckIntervalWhenMissing;
            }

            var remain = _nextQcnCheckUtc - DateTime.UtcNow;
            if (remain <= TimeSpan.Zero)
            {
                return TimeSpan.FromSeconds(1);
            }

            return remain;
        }

        /// <summary>
        /// 네티 연결이 끊어졌을 때 재연결을 시도한다.
        /// </summary>
        private async Task EnsureNettyConnectionAsync(UserInfo currentUser, string currentToken)
        {
            if (currentUser == null || string.IsNullOrWhiteSpace(currentToken))
                return;

            if (AuctionDelegate.getInstance().isActive())
                return;

            DateTime nowUtc = DateTime.UtcNow;
            if (_duplicateCooldownUntilUtc != DateTime.MinValue && nowUtc >= _duplicateCooldownUntilUtc)
            {
                _duplicateCooldownUntilUtc = DateTime.MinValue;
                _duplicateCooldownLogged = false;
                logger.LogInfo("EnsureNettyConnectionAsync: duplicate cooldown 만료, 재연결 시도 재개");
            }

            if (_duplicateCooldownUntilUtc != DateTime.MinValue && nowUtc < _duplicateCooldownUntilUtc)
            {
                if (!_duplicateCooldownLogged)
                {
                    var remain = _duplicateCooldownUntilUtc - nowUtc;
                    logger.LogWarn($"EnsureNettyConnectionAsync: duplicate cooldown active ({Math.Ceiling(remain.TotalSeconds)}초 남음)");
                    _duplicateCooldownLogged = true;
                }
                return;
            }

            while (isConnecting)
            {
                await Task.Delay(200);
                if (AuctionDelegate.getInstance().isActive())
                    return;
            }

            isConnecting = true;
            try
            {
                await AuctionDelegate.getInstance().disposeClients();
                await Task.Delay(300);
                NettyConnectResult connResult = await _conn.NettyComm(currentUser, currentToken);

                if (connResult == NettyConnectResult.Connected)
                {
                    _duplicateCooldownUntilUtc = DateTime.MinValue;
                    _duplicateCooldownLogged = false;
                    return;
                }

                if (connResult == NettyConnectResult.Duplicate)
                {
                    _duplicateCooldownUntilUtc = DateTime.UtcNow.Add(DuplicateRetryCooldown);
                    _duplicateCooldownLogged = false;
                    WeakReferenceMessenger.Default.Send(new DataStringMessage("응찰 서버 중복 접속 감지, 잠시 후 재시도"));
                    logger.LogWarn($"EnsureNettyConnectionAsync: netty result = {connResult}, cooldown={DuplicateRetryCooldown.TotalSeconds:0}s");
                    await AuctionDelegate.getInstance().disposeClients();
                    return;
                }

                WeakReferenceMessenger.Default.Send(new DataStringMessage("응찰 서버 연결 실패"));
                logger.LogWarn($"EnsureNettyConnectionAsync: netty result = {connResult}");
            }
            catch (Exception ex)
            {
                logger.LogError($"EnsureNettyConnectionAsync error: {ex.Message}");
            }
            finally
            {
                isConnecting = false;
            }
        }

        // 2024-07-13 수정된 내용: StopAsync 함수를 추가하여 비동기 작업을 중단
        /// <summary>
        /// 서버 처리 루프를 중단한다.
        /// </summary>
        public async Task StopAsync()
        {
            _isRunning = false;
            // 💡 [추가] 정지 시점 최종 API 누적 건수 기록
            logger.LogInfo($"[API 모니터링 종료] 최종 누적 API 호출 건수: {ServerConn.TotalApiCount}회");

            await Task.Delay(1000).ConfigureAwait(false); // 비동기 작업이 안전하게 종료될 시간을 준다.
        }

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // SemaphoreSlim 초기화
        private DateTime _now = DateTime.Now;
        /// <summary>
        /// 서버에서 받은 데이터를 비교하고 변경사항을 브로드캐스트한다.
        /// </summary>
        private async Task ProcessMessageAsync()
        {
            await _semaphore.WaitAsync(); // 세마포어 진입
            try
            {
                List<gValues> currentSyncList = new List<gValues>();
                List<gValues> latestSnapshot = new List<gValues>();
                List<gValues>? beforeSnapshot = null;

                try
                {
                    //시작 전 싹 지우고 시작
                    if (mAPIList != null && _latestAuctionDataList != null)
                    {
                        if (_latestAuctionDataList.Count > 0)
                        {

                        }
                        _latestAuctionDataList.Clear();
                    }

                    // 매초마다 서버에서 받아오는 목록들 List<string> 
                    var currentUser = _userInfo;
                    var currentToken = _token;
                    if (currentUser != null && currentToken != null)
                    {
                        mAPIList = await _conn.SvInfoRequest(currentUser, currentToken);
                    }

                    if (mAPIList != null && mAPIList.Count > 0) //서버에서 온 데이터와 EPD 데이터 합치기
                    {

                        //더 빠르게 정렬
                        var bag = new ConcurrentBag<gValues>();

                        if (currentUser != null)
                        {
                            Parallel.ForEach(mAPIList, message =>
                            {
                                var parsed = _animParseData.Parse_PacketApi(message, currentUser, _conn);
                                bag.Add(parsed);
                            });
                        }

                        // 정렬을 병렬로 처리
                        var parsedData = bag.AsParallel().OrderBy(x => x.EntityNumber).ToList();


                        lock (_lockObj)
                        {
                            _latestAuctionDataList = parsedData;
                            latestSnapshot = new List<gValues>(_latestAuctionDataList);
                            beforeSnapshot = _beforeAuctionDataList != null
                                ? new List<gValues>(_beforeAuctionDataList)
                                : null;
                        }
                    }
                    else
                    {
                        lock (_lockObj)
                        {
                            latestSnapshot = _latestAuctionDataList != null
                                ? new List<gValues>(_latestAuctionDataList)
                                : new List<gValues>();
                            beforeSnapshot = _beforeAuctionDataList != null
                                ? new List<gValues>(_beforeAuctionDataList)
                                : null;
                        }
                    }

                    //============================== 시 작 =================================================
                    if (beforeSnapshot == null) //처음 시작
                    {
                        var initialSnapshot = latestSnapshot.OrderBy(x => x.EntityNumber).ToList();
                        lock (_lockObj)
                        {
                            _beforeAuctionDataList = new List<gValues>(initialSnapshot);
                        }
                        InsertDatas(initialSnapshot);
                        return;
                    }

                    var beforeMap = BuildEntityNumberMap(beforeSnapshot);
                    var latestMap = BuildEntityNumberMap(latestSnapshot);

                    var removedItems = beforeMap.Keys
                        .Except(latestMap.Keys)
                        .Select(key => beforeMap[key])
                        .ToList();

                    var addedItems = latestMap.Keys
                        .Except(beforeMap.Keys)
                        .Select(key => latestMap[key])
                        .ToList();

                    var updateItems = new List<gValues>();
                    var moveItems = new List<gValues>();

                    var commonKeys = beforeMap.Keys.Intersect(latestMap.Keys);
                    foreach (var key in commonKeys)
                    {
                        var before = beforeMap[key];
                        var latest = latestMap[key];

                        if (!string.Equals(before.SpaceIndex, latest.SpaceIndex, StringComparison.Ordinal))
                        {
                            moveItems.Add(latest);
                            continue;
                        }

                        if (!before.Equals(latest))
                        {
                            updateItems.Add(latest);
                        }
                    }

                    DataDeletes(removedItems, latestSnapshot);
                    InsertDatas(addedItems);
                    ModifiedData(updateItems, moveItems, beforeMap, latestSnapshot);

                    // _beforeAuctionDataList 업데이트
                    var nextBeforeSnapshot = latestSnapshot.OrderBy(x => x.EntityNumber).ToList();
                    lock (_lockObj)
                    {
                        _beforeAuctionDataList = new List<gValues>(nextBeforeSnapshot);
                    }
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


        /// <summary>
        /// 삭제된 데이터를 로고로 치환해 알린다.
        /// </summary>
        private void DataDeletes(List<gValues> deleteDatas, List<gValues> latestSnapshot)
        {
            deleteDatas = FilterOutRunningEntity(deleteDatas, "delete");
            if (deleteDatas == null || deleteDatas.Count == 0)
            {
                return;
            }

            // 삭제된 SpaceIndex가 남아있는 개체가 있으면 그 개체를 표출
            // 없으면 로고로 대체
            var updates = new List<gValues>();
            var affectedSpaceIndexes = deleteDatas
                .Select(item => item.SpaceIndex)
                .Distinct()
                .ToList();

            foreach (var spaceIndex in affectedSpaceIndexes)
            {
                var candidates = latestSnapshot.Where(item => item.SpaceIndex == spaceIndex);

                var winner = CowAuctionSmall.Utils.CowDisplaySelector.SelectForSpaceIndex(candidates);
                if (winner != null)
                {
                    updates.Add(winner);
                }
                else
                {
                    updates.Add(new gValues
                    {
                        SpaceIndex = spaceIndex,
                        AuctionResultStatus = "00"
                    });
                }
            }

            Debug.WriteLine("삭제된 데이터 갯수 : " + deleteDatas.Count);
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(updates));
        }

        /// <summary>
        /// 추가된 데이터를 화면에 전달한다.
        /// </summary>
        private void InsertDatas(List<gValues> insertDatas)
        {
            insertDatas = FilterOutRunningEntity(insertDatas, "insert");
            if (insertDatas.Count == 0)
            {
                return;
            }
            Debug.WriteLine("추가된 데이터 갯수 : " + insertDatas.Count);
            // MainWindowViewModel 쪽으로 데이터 전달
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(insertDatas));
        }

        /// <summary>
        /// 수정된 데이터를 비교해 화면 갱신 목록을 만든다.
        /// </summary>
        //값이 변경된 데이터 처리
        private void ModifiedData(
            List<gValues> updateItems,
            List<gValues> moveItems,
            Dictionary<string, gValues> oldMapByEntityNumber,
            List<gValues> latestSnapshot)
        {
            var o1 = updateItems;
            var o2 = moveItems;

            o1 = FilterOutRunningEntity(o1, "update");
            o2 = FilterOutRunningEntity(o2, "move");

            Debug.WriteLine($"수정된 데이터 갯수 o1 : {o1.Count}    ,    o2 : {o2.Count}");

            // 개체번호,계류대 위치는 동일한데, 다른 정보가 변경된 경우, 어미, 산차, 비고 등
            if (o1.Count > 0)
            {
                WeakReferenceMessenger.Default.Send(new DataChangedMessage(o1));
            }

            //개체번호 동일,계류대 위치가 다른 경우
            if (o2.Count > 0)
            {
                // 이동 발생 시 old/new SpaceIndex 모두를 최신 스냅샷 기준으로 다시 결정한다.
                // (기존 old space를 무조건 로고로 보내면, 같은 위치에 남아있는 다른 개체를 지워버릴 수 있음)
                var beforeItems = o2
                    .Where(item => oldMapByEntityNumber.ContainsKey(item.EntityNumber))
                    .Select(item => oldMapByEntityNumber[item.EntityNumber])
                    .ToList();

                Debug.WriteLine("\n계류대 위치가 바뀐 데이터 갯수 : " + beforeItems.Count);
                for (int i = 0; i < o2.Count; i++)
                {
                    Debug.WriteLine($"원래 계류대 위치 : {beforeItems[i].SpaceIndex}\n바뀐 계류대 위치 :  {o2[i].SpaceIndex}");
                    logger.LogInfo($"원래 계류대 위치 : {beforeItems[i].SpaceIndex}\n바뀐 계류대 위치 :  {o2[i].SpaceIndex}");
                }

                var affectedSpaceIndexes = beforeItems
                    .Select(item => item.SpaceIndex)
                    .Concat(o2.Select(item => item.SpaceIndex))
                    .Where(spaceIndex => !string.IsNullOrWhiteSpace(spaceIndex))
                    .Distinct()
                    .ToList();

                var updates = new List<gValues>();
                foreach (var spaceIndex in affectedSpaceIndexes)
                {
                    var candidates = latestSnapshot.Where(item => item.SpaceIndex == spaceIndex);

                    var winner = CowAuctionSmall.Utils.CowDisplaySelector.SelectForSpaceIndex(candidates);
                    if (winner != null)
                    {
                        updates.Add(winner);
                    }
                    else
                    {
                        updates.Add(new gValues
                        {
                            SpaceIndex = spaceIndex,
                            AuctionResultStatus = "00"
                        });
                    }
                }

                Debug.WriteLine("이동 반영 최종 갱신 갯수 : " + updates.Count);
                WeakReferenceMessenger.Default.Send(new DataChangedMessage(updates));
            }

        }

        private static Dictionary<string, gValues> BuildEntityNumberMap(List<gValues>? source)
        {
            var map = new Dictionary<string, gValues>(StringComparer.Ordinal);
            if (source == null || source.Count == 0)
            {
                return map;
            }

            foreach (var item in source)
            {
                if (item == null)
                {
                    continue;
                }

                var key = item.EntityNumber;
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                map[key] = item;
            }

            return map;
        }

        private List<gValues> FilterOutRunningEntity(List<gValues>? source, string reason)
        {
            if (source == null || source.Count == 0)
            {
                return new List<gValues>();
            }

            if (_runRunSipNumber == -1)
            {
                return source;
            }

            string runningSip = _runRunSipNumber.ToString();
            var blocked = source
                .Where(item => item != null && string.Equals(item.SipNumber, runningSip, StringComparison.Ordinal))
                .ToList();

            if (blocked.Count > 0)
            {
                string blockedEntityNumbers = string.Join(",",
                    blocked
                        .Select(item => item.EntityNumber)
                        .Where(entityNumber => !string.IsNullOrWhiteSpace(entityNumber))
                        .Distinct()
                        .Take(5));

                string logMsg =
                    $"blocked-running-entity-change reason={reason}, sip={runningSip}, count={blocked.Count}, entities={blockedEntityNumbers}";
                Debug.WriteLine(logMsg);
                logger.LogInfo(logMsg);
            }

            return source
                .Where(item => item != null && item.SipNumber != runningSip)
                .ToList();
        }

        /// <summary>
        /// 단일경매 종료를 즉시 반영하고 진행 상태를 해제한다.
        /// </summary>
        private void CompleteSingleAuction(string sipNumber, string? resultCode, string source, bool updateResultStatus)
        {
            if (string.IsNullOrWhiteSpace(sipNumber))
            {
                logger.LogWarn($"CompleteSingleAuction: sipNumber가 비어 있습니다. source={source}, result={resultCode ?? "-"}");
                return;
            }

            lock (_lockObj)
            {
                List<gValues> updates = new List<gValues>();

                if (_beforeAuctionDataList != null)
                {
                    updates = _beforeAuctionDataList
                        .Where(item => item != null && item.SipNumber == sipNumber)
                        .ToList();
                }

                if (_latestAuctionDataList != null)
                {
                    foreach (var latestItem in _latestAuctionDataList.Where(item => item != null && item.SipNumber == sipNumber))
                    {
                        latestItem.IsRunning = false;
                        if (updateResultStatus && !string.IsNullOrWhiteSpace(resultCode))
                        {
                            latestItem.AuctionResultStatus = resultCode;
                        }
                    }
                }

                foreach (var item in updates)
                {
                    item.IsRunning = false;
                    if (updateResultStatus && !string.IsNullOrWhiteSpace(resultCode))
                    {
                        item.AuctionResultStatus = resultCode;
                    }
                }

                if (_runRunSipNumber.ToString() == sipNumber)
                {
                    _runRunSipNumber = -1;
                }

                logger.LogInfo(
                    $"single-auction-complete source={source}, sip={sipNumber}, result={resultCode ?? "-"}, updates={updates.Count}, clear-running={_runRunSipNumber == -1}");

                if (updates.Count > 0)
                {
                    WeakReferenceMessenger.Default.Send(new DataChangedMessage(updates));
                }
                else
                {
                    logger.LogWarn($"CompleteSingleAuction: 종료 대상이 없습니다. source={source}, sip={sipNumber}, result={resultCode ?? "-"}");
                }
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

        /// <summary>
        /// 스페이스바/단일경매 메시지를 처리한다.
        /// </summary>
        private async void OnStringArrMsg(object recipient, DataToServerGetArrMsg message)
        {
            List<gValues> currentSyncList = new List<gValues>();
            var beforeList = _beforeAuctionDataList;

            string code = message.Data[1]; //AS,SV [1]
            if (code.Contains("SV"))
            {
                code = "SV";
            }
            else if (message.Refresh== "refresh")
            {
                var newData = new string[message.Data.Length + 1];
                newData[0] = "20";
                Array.Copy(message.Data, 0, newData, 1, message.Data.Length);

                message = new DataToServerGetArrMsg(newData, message.Refresh);
                code = "refresh";
            }
            else
            {

            }
            string msgString = string.Join(", ", message.Data);
            Debug.WriteLine("스페이스바 땡 누름 : " + msgString);



            if (message.Data[0].Equals("20") && (beforeList != null || code == "refresh")) //단일 경매
            {
                if (code != "refresh" && beforeList == null)
                {
                    logger.LogError("OnStringArrMsg: _beforeAuctionDataList가 null입니다.");
                    return;
                }

                switch (code)
                {
                    case "AS":
                        string? autctionState = null;
                        if (message.Data.Length == 5)
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
                                    logger.LogInfo($"==================================== 경매 종료 {message.Data.ToString()}");
                                    CompleteSingleAuction(message.Data[2], null, "AS8006", updateResultStatus: false);
                                    break;
                                case "8004": //경매 진행

                                    if (message.Data[3].Equals("refresh") == false)
                                    {
                                        var tempList = beforeList.Where(item => item.SipNumber == message.Data[2]); //경매번호만 같은거

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
                                                logger.LogInfo("가격이 다름\n" + cowAS.toString() + "\n" + message.Data[3]);
                                                cowAS.IsRunning = false;
                                            }

                                            _runRunSipNumber = int.Parse(cowAS.SipNumber);
                                            currentSyncList.Add(cowAS);
                                        }

                                        WeakReferenceMessenger.Default.Send(new DataChangedMessage(currentSyncList));
                                        currentSyncList.Clear();
                                    }
                                    break;

                                case "8001":

                                    if (message.Data[3].Equals("refresh") && _runRunSipNumber == -1)
                                    {
                                        //오늘 경매날이라면 새로고침 x
                                        if(isAuctionDate == true)
                                        {
                                            Debug.WriteLine("==================================== 새로고침 신호 무시 (경매일)");
                                            break;
                                        }

                                        Debug.WriteLine("***************************************************새로고침 시작 신호***************************************************");
                                        logger.LogInfo("***************************************************새로고침 시작 신호***************************************************");
                                        
                                        WeakReferenceMessenger.Default.Send(new DisplaySelectRefresh("Refresh"));
                                        WeakReferenceMessenger.Default.Send(new DataStringMessage($"{DateTime.Now} 새로고침 시작 신호 {message.Data[0]} {message.Data[1]} {message.Data[2]} {message.Data[3]}"));
                                        
                                        startRefesh = true;
                                    }

                                    break;
                                
                            }
                            break;
                        }

                    case "SV": // 새로고침할 출품 , 단발 건으로 경매프로그램에서 지정한 날짜에 갯수 만큼 호출되어짐
                        
                        if (beforeList != null && message.Data.Length > 2) // 대량 새로고침
                        {
                            var currentUser = _userInfo;
                            if (currentUser == null)
                            {
                                logger.LogError("OnStringArrMsg: _userInfo가 null입니다.");
                                return;
                            }

                            if (beforeList.Count <= 0) //if (temp.Result.Count() <= 0)
                            {
                                List<gValues> _refreshAuchList = new List<gValues>();
                                foreach (var item in message.Data.Skip(1))
                                {
                                    gValues cowSV = new gValues();
                                    string msgString2 = item;
                                    //msgString2 = _conn.JoinEpdnDataSV(msgString2, epdList);
                                    cowSV = _animParseData.Parse_PacketApi(msgString2, currentUser, _conn);
                                    _refreshAuchList.Add(cowSV);
                                }
                                WeakReferenceMessenger.Default.Send(new DataChangedMessage(_refreshAuchList));
                                startRefesh = false;
                                WeakReferenceMessenger.Default.Send(new DataStringMessage("새로고침 완료, 총갯수 : " + _refreshAuchList.Count + ", 변경시간 : " + DateTime.Now));
                            }
                            else
                            {
                                //DisplaySelectRefresh
                                if (startRefesh)
                                {
                                    //WeakReferenceMessenger.Default.Send(new DisplaySelectRefresh("Refresh"));
                                    WeakReferenceMessenger.Default.Send(new DataStringMessage("새로고침 X 동일 데이터 , 완료시간 : " + DateTime.Now));
                                    startRefesh = false;
                                }
                                
                            }

                        }
                        else // 단발 건
                        {
                        }



                        break;
                    case "refresh":
                        List<gValues> _refreshAuchList2 = new List<gValues>();
                        var refreshUser = _userInfo;
                        if (refreshUser == null)
                        {
                            logger.LogError("OnStringArrMsg: _userInfo가 null입니다.");
                            return;
                        }

                        foreach (var item in message.Data.Skip(1))
                        {
                            gValues cowSV = new gValues();
                            string msgString2 = item;
                            //msgString2 = _conn.JoinEpdnDataSV(msgString2, epdList);
                            cowSV = _animParseData.Parse_PacketApi(msgString2, refreshUser, _conn);
                            _refreshAuchList2.Add(cowSV);
                        }
                        WeakReferenceMessenger.Default.Send(new DataChangedMessage(_refreshAuchList2));
                        startRefesh = false;
                        WeakReferenceMessenger.Default.Send(new DataStringMessage("새로고침 완료, 총갯수 : " + _refreshAuchList2.Count + ", 변경시간 : " + DateTime.Now));
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
                        if (message.Data[2].Equals("8004") && beforeList != null) //경매 진행상태라면
                        {
                            var tempList = beforeList.Where(item => item.AuctionResultStatus.Equals("11")); //경매 진행중인것만
                            foreach (gValues cow in tempList)
                            {
                                currentSyncList.Add(cow);
                            }
                            WeakReferenceMessenger.Default.Send(new DataChangedMessage(currentSyncList));
                            Debug.WriteLine("\n**************\n**************\n일괄 경매 시작\n**************\n**************");
                            _batchRunningState = true;
                        }
                        else if (message.Data[2].Equals("8006") && beforeList != null)
                        {
                            Debug.WriteLine("\n**************\n**************\n일괄 경매 끝\n**************\n**************");
                            _batchRunningState = false;

                            // 💡 [추가] 일괄경매 종료 신호 수신 즉시 API 데이터를 재조회하여 화면을 낙찰/유찰 상태로 전환
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(300); // 서버 DB 결과 반영 대기
                                await ProcessMessageAsync(); // 최신 데이터 수신 및 화면 갱신
                            });
                        }
                        break;
                    case "SZ":
                        //`SZ|8808990657202|20240906|0|1|P|1|900`
                        //서버에서 보내주는 데이터에는 날짜가 있어, 경매 전날에 데이터를 확인하려면 이 방법으로 확인 할수 밖에 없다...?
                        Debug.WriteLine("일괄경매 경매대상 표시 표시");
                        var currentUser = _userInfo;
                        var currentToken = _token;
                        if (currentUser == null || string.IsNullOrWhiteSpace(currentToken))
                        {
                            logger.LogError("OnStringArrMsg(SZ): _userInfo 또는 _token이 null입니다.");
                            break;
                        }

                        string? szDate = message.Data.Length > 3 ? message.Data[3] : null;
                        if (string.IsNullOrWhiteSpace(szDate) || szDate.Length != 8)
                        {
                            logger.LogWarn($"OnStringArrMsg(SZ): 유효하지 않은 날짜입니다. szDate={szDate ?? "null"}");
                            break;
                        }

                        try
                        {
                            logger.LogInfo($"OnStringArrMsg(SZ): 재조회 시작 date={szDate}");
                            var qcn = await _conn.PostQcn(currentUser, currentToken, szDate);
                            if (qcn == null)
                            {
                                logger.LogInfo($"OnStringArrMsg(SZ): 차수데이터 없음 date={szDate}");
                                WeakReferenceMessenger.Default.Send(new DataStringMessage($"SZ 날짜({szDate}) 차수데이터가 없습니다."));
                                break;
                            }

                            var szRawList = await _conn.SvInfoRequest(currentUser, currentToken, szDate);
                            if (szRawList == null || szRawList.Count == 0)
                            {
                                logger.LogInfo($"OnStringArrMsg(SZ): 조회된 데이터가 없습니다. date={szDate}");
                                WeakReferenceMessenger.Default.Send(new DataStringMessage($"SZ 날짜({szDate}) 조회 데이터가 없습니다."));
                                break;
                            }

                            var parsedList = new List<gValues>(szRawList.Count);
                            foreach (var raw in szRawList)
                            {
                                var cow = _animParseData.Parse_PacketApi(raw, currentUser, _conn);
                                if (cow != null)
                                {
                                    parsedList.Add(cow);
                                }
                            }

                            if (parsedList.Count == 0)
                            {
                                logger.LogInfo($"OnStringArrMsg(SZ): 파싱된 데이터가 없습니다. date={szDate}");
                                break;
                            }

                            _beforeAuctionDataList = parsedList.OrderBy(x => x.EntityNumber).ToList();
                            WeakReferenceMessenger.Default.Send(new DisplaySelectRefresh("Refresh"));
                            WeakReferenceMessenger.Default.Send(new DataChangedMessage(new List<gValues>(_beforeAuctionDataList)));
                            WeakReferenceMessenger.Default.Send(new DataStringMessage($"SZ 날짜({szDate}) 재조회 완료, 총갯수 : {_beforeAuctionDataList.Count}, 변경시간 : {DateTime.Now}"));
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"OnStringArrMsg(SZ): 재조회 예외 date={szDate}, err={ex.Message}");
                        }

                        break;
                    default:
                        Debug.WriteLine("OnStringArrMsg 에서 code값이 AS,SV 이외의 {0} 값이 나옴 {1}", code, message.Data.ToString());
                        break;
                }
            }
        }



        public bool startRefesh = false;




        /// <summary>
        /// 단순 문자열 메시지를 처리한다.
        /// </summary>
        private void OnStringMsg(object recipient, DataToServerGetMsg message)
        {
            if (message.Data.Equals("F"))
            {
                _runRunSipNumber = -1;
                return;
            }
        }

        /// <summary>
        /// 회차 종료 등 상태 메시지를 처리한다.
        /// </summary>
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
        /// <summary>
        /// 단일 경매 유찰/취소 메시지를 처리한다.
        /// </summary>
        private void OnStringArrAF_SD(object recipient, DataToServerGetAF_SD message)
        {
            string[] msg = message.Data;
            string code = msg[0];

            switch (code)
            {
                case "AF":
                    Debug.WriteLine($"AF 온 값 : {msg.ToString}");
                    if (msg.Length < 4)
                    {
                        logger.LogWarn("OnStringArrAF_SD(AF): 데이터 길이가 부족합니다.");
                        break;
                    }

                    switch (msg[3])
                    {
                        case "22":
                            CompleteSingleAuction(msg[2], msg[3], "AF", updateResultStatus: true);
                            break;
                        case "23":
                            CompleteSingleAuction(msg[2], msg[3], "AF", updateResultStatus: true);
                            break;
                        case "24":
                            CompleteSingleAuction(msg[2], msg[3], "AF", updateResultStatus: false);
                            break;
                        default:
                            logger.LogInfo($"OnStringArrAF_SD(AF): 종료 처리 대상이 아닌 코드입니다. code={msg[3]}");
                            break;
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
        /// <summary>
        /// 리소스와 메시지 구독을 정리한다.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposeState, 1) == 1)
            {
                return;
            }

            try
            {
                Task.Run(StopAsync).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logger.LogError($"ServerGetData.Dispose: StopAsync 실패 - {ex.Message}");
            }

            _messenger.UnregisterAll(this);
            _messengerStringArr.UnregisterAll(this);
            _messenger8007.UnregisterAll(this);
            _messengerStArrAF_SD.UnregisterAll(this);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// 최종자에서 정리 루틴을 호출한다.
        /// </summary>
        ~ServerGetData()
        {
            Dispose();
        }
        
        /// <summary>
        /// 낙찰,유찰시 해당 화면 변경 (그냥 서버로 오는 값을 변경하면 되는거 아닌가 할수도 있지만 서버로 부터 오는건 축약된 정보 발송 , 낙찰자 이름 X)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <summary>
        /// 낙찰/유찰 결과를 재조회해 화면 정보를 보정한다.
        /// </summary>
        public async Task ReCheckSoldItemAsync(string[] data)
        {
            // data[0] : 코드
            // 1. 축협코드
            // 경매번호
            // 경매상태 코드
            // ?
            // 낙찰자 참가번호
            // 낙찰가격

            //AF|8808990656458|32|22|2160|16|379
            //
            Debug.WriteLine("****************************************************************************************************************************");
            await Task.Delay(100);
            if (_userInfo == null || string.IsNullOrWhiteSpace(_token))
            {
                logger.LogError("ReCheckSoldItemAsync: _userInfo 또는 _token이 null입니다.");
                return;
            }
            if (_beforeAuctionDataList == null)
            {
                logger.LogError("ReCheckSoldItemAsync: _beforeAuctionDataList가 null입니다.");
                return;
            }

            mAPIList = await _conn.SvInfoRequest(_userInfo, _token);
            if (mAPIList == null)
            {
                logger.LogError("ReCheckSoldItemAsync: SvInfoRequest 결과가 null입니다.");
                return;
            }
            string? bidderItem = mAPIList.FirstOrDefault(cow => cow.Split("|")[2].Equals(data[2]) );
            if (bidderItem != null && bidderItem.Length>0)
            {
                gValues bidderCow = _animParseData.Parse_PacketApi(bidderItem, _userInfo, _conn);
                bidderCow.IsRunning = false;
                if (data.Length > 3 && (data[3] == "22" || data[3] == "23"))
                {
                    // AF를 최우선 결과 신호로 사용한다. API 반영이 늦어도 낙/유찰 상태는 되돌리지 않는다.
                    bidderCow.AuctionResultStatus = data[3];
                }

                lock (_lockObj)
                {
                    _beforeAuctionDataList.Remove(_beforeAuctionDataList.FirstOrDefault(cow => cow.SipNumber.Equals(bidderCow.SipNumber)));
                    _beforeAuctionDataList.Add(bidderCow);
                    _beforeAuctionDataList = _beforeAuctionDataList.OrderBy(x => x.EntityNumber).ToList();
                }

                if (bidderCow.Bidder.Equals("-"))
                {
                    Debug.WriteLine($"ReCheckSoldItem 경매번호: {bidderCow.SipNumber} 유찰");
                    logger.LogError($"ReCheckSoldItem 경매번호: {bidderCow.SipNumber} 유찰");
                }
                else
                {
                    Debug.WriteLine($"ReCheckSoldItem 경매번호: {bidderCow.SipNumber} 낙찰자: {bidderCow.Bidder}");
                    logger.LogInfo($"ReCheckSoldItem 경매번호: {bidderCow.SipNumber} 낙찰자: {bidderCow.Bidder}");
                }

                List<gValues> currentSyncList = new List<gValues>();
                currentSyncList.Add(bidderCow);
                WeakReferenceMessenger.Default.Send(new DataChangedMessage(currentSyncList));

                currentSyncList.Clear();
            }
            else
            {
                Debug.WriteLine($"ReCheckSoldItem bidderItem 값이 없음");
            }
            Debug.WriteLine("****************************************************************************************************************************");
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
