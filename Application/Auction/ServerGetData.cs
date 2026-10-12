using CowAuctionSmall.Infra.Logging;
using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Domain.Auction;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using UserInfo = CowAuctionSmall.Domain.Settings.UserInfo;

namespace CowAuctionSmall.Application.Auction
{
    /// <summary>
    /// 서버에서 받아온 데이터를 처리 
    /// 고정적으로 데이터를 받아오고 처리 , 화면에 뿌려줄 객체 선별 후 전달 (메인뷰로 전달)
    /// </summary>
    public class ServerGetData : IDisposable
    {
        public static ServerGetData? Instance { get; private set; }

        private readonly IAuctionSessionRuntime _runtime;
        private readonly IAuctionDataSource _dataSource;
        private readonly AuctionPollingLifetime _polling = new();
        private readonly CancellationToken _lifetimeToken;
        private int _stopRequested;
        private string? _appliedSnapshotDate;
        public Task PollingCompletion => _polling.Completion;
        private NettyAsyncMsgProcess _nettyAsync;
        public AuctionIngress Ingress => _nettyAsync.Ingress;
        public bool ShouldShutdownForNight(DateTime localTime) => NightShutdownPolicy.ShouldShutdown(localTime,
            Volatile.Read(ref _runRunSipNumber) != -1 || Volatile.Read(ref _batchRunningState),
            Ingress.NeedsResync || Ingress.IsBusy, _nettyAsync.TimeSinceAuctionSignal);
        private UserInfo? _userInfo;
        private string? _token;


        // 여기 구간은 시험용으로
        private bool _isRunning; // 서버 데이터 처리가 실행 중인지 여부, 에러가 생길시 동작 정지

        private Stopwatch stopwatch = new Stopwatch();  // Stopwatch 객체 생성
        private readonly object _lockObj = new object(); // 락 오브젝트 선언

        public static List<gValues>? _latestAuctionDataList; //최신 경매 데이터 리스트
        private static List<gValues>? _beforeAuctionDataList; //과거 경매 데이터 리스트

        public static int _runRunSipNumber = -1; // 현재 진행중인 경매 번호 (경매진행중일때 는 다른화면으로 전환 x 하려고) -1을 초기값으로 한 이유는 개발서버에서 0번 경매도 생성가능해서
        public static bool _batchRunningState = false; // 일괄 경매 진행중인지 여부

        private readonly AuctionSnapshotParser _snapshots;
        //
        private string? _currentRefreshDate = null;
        private readonly AuctionItemStore _itemStore = new();
        private readonly AuctionDisplayUpdateBatcher _displayBatch;
        private BoardSourceStatistics? _publishedStatistics;
        private int _auctionMethod;
        private long _observedResyncVersion;
        private string? _stateDate;
        private bool HasCurrentSnapshot => _stateDate == null || _appliedSnapshotDate == _stateDate;

        private AuctionSectionContext? SectionContext()
        {
            if (string.IsNullOrEmpty(_token) || string.IsNullOrEmpty(_userInfo?.Auction?.AuctionHouseCode)) return null;
            string? date = !string.IsNullOrWhiteSpace(_currentRefreshDate) ? _currentRefreshDate : _userInfo.CurrentInfo?.Date;
            if (string.IsNullOrWhiteSpace(date)) date = DateTime.Today.ToString("yyyyMMdd");
            return DateOnly.TryParseExact(date, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var selected)
                ? new AuctionSectionContext(_userInfo.Auction.AuctionHouseCode, selected) : null;
        }

        private readonly WeakReferenceMessenger _messenger;    

        private readonly WeakReferenceMessenger _messenger8007;         //경매 종료시 , 메시지를 보내는 곳(NettyAsyncMsgProcess)
        private readonly WeakReferenceMessenger _messengerStringArr;    //스페이스바 누를때, 메시지를 보내는 곳(NettyAsyncMsgProcess)
        private readonly WeakReferenceMessenger _messengerStArrAF_SD;   //단일 유찰시, 메시지를 보내는 곳(NettyAsyncMsgProcess)

        private readonly IAppLog logger;

        /// <summary>
        /// 서버 통신과 메시지 핸들러를 초기화한다.
        /// </summary>
        /// <param name="runtime">시작·연결 외부 경계</param>
        /// <param name="nettyAsyncMsg"></param> 비동기적으로 오는 메시지처리(경매 프로그램에 의한 비동기적 메시지)
        public ServerGetData(IAuctionSessionRuntime runtime, NettyAsyncMsgProcess nettyAsyncMsg, IAuctionDataSource dataSource, AuctionSnapshotParser snapshots, IAppLog log)
        {

            Instance = this;
            _lifetimeToken = _polling.Token;

            logger = log;
            _runtime = runtime;
            _dataSource = dataSource;
            _nettyAsync = nettyAsyncMsg;
            if (dataSource is IAuctionSectionSource sections)
                _nettyAsync.ConfigureSectionLookup(SectionContext, sections.ReadSectionAsync, _lifetimeToken);

            _snapshots = snapshots;
            _displayBatch = new AuctionDisplayUpdateBatcher(action => Ingress.TryPost(action),
                rows =>
                {
                    if (Volatile.Read(ref _disposeState) == 0 && Volatile.Read(ref _stopRequested) == 0)
                        WeakReferenceMessenger.Default.Send(new DataChangedMessage(rows));
                });

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
            // 화면 구독 준비 후 InitializeAsync에서 시작한다.

        }


        /// <summary>
        /// 초기 설정을 읽고 서버 통신을 시작한다.
        /// </summary>
        private int _initializeState;

        public async Task InitializeAsync(Action<UserInfo> onAuthenticated)
        {
            if (Interlocked.Exchange(ref _initializeState, 1) != 0 || Volatile.Read(ref _disposeState) != 0)
                return;

            try
            {
                _userInfo = _runtime.LoadUserSettings();
                if (_userInfo?.Auction == null)
                    throw new InvalidOperationException();

                string? expectedCode = _userInfo.Auction.AuctionHouseCode?.Trim();
                await WaitForNetwork();
                if (Volatile.Read(ref _disposeState) != 0)
                    return;

                var login = await _runtime.AuthenticateAsync(_userInfo).WaitAsync(_lifetimeToken);
                if (Volatile.Read(ref _disposeState) != 0)
                    return;
                if (login == null)
                {
                    WeakReferenceMessenger.Default.Send(new DataStringMessage("로그인 정보 또는 사업장 코드를 확인할 수 없어 연결을 시작하지 않았습니다."));
                    return;
                }

                _token = login.AccessToken;
                onAuthenticated(_userInfo);
                if (Volatile.Read(ref _disposeState) != 0)
                    return;

                if (!string.IsNullOrWhiteSpace(expectedCode))
                {
                    string codeMessage = string.Equals(expectedCode, login.AuctionHouseCode, StringComparison.Ordinal)
                        ? "[코드 일치]"
                        : $"[코드 불일치]\n로그인 시 코드: {login.AuctionHouseCode}\n작성한 코드: {expectedCode}\n로그인한 사업장으로 진행합니다.";
                    WeakReferenceMessenger.Default.Send(new DataStringMessage(codeMessage));
                }
                var cached = await _runtime.ReadCachedAuctionAsync(_userInfo).WaitAsync(_lifetimeToken);
                if (cached != null && cached.Count > 0 && Volatile.Read(ref _disposeState) == 0)
                {
                    var cacheSnapshot = cached.Select(item => item.Clone()).ToList();
                    Ingress.TryPost(() =>
                    {
                        if (_beforeAuctionDataList != null) return;
                        _beforeAuctionDataList = cacheSnapshot;
                        _appliedSnapshotDate = _userInfo.CurrentInfo?.Date;
                        WeakReferenceMessenger.Default.Send(new DataChangedMessage(cacheSnapshot, isFullSnapshot: true));
                    });
                    WeakReferenceMessenger.Default.Send(new DataStringMessage("당일 경매 캐시를 표시했습니다. 서버 최신 정보를 확인합니다."));
                }
                await StartAsync();
            }
            catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested) { }
            catch (Exception)
            {
                logger.LogError("InitializeAsync: 로그인 또는 화면 초기화에 실패했습니다.");
                if (Volatile.Read(ref _disposeState) == 0)
                    WeakReferenceMessenger.Default.Send(new DataStringMessage("로그인 또는 화면 초기화에 실패하여 연결을 중단했습니다."));
            }
        }

        /// <summary>
        /// 네트워크 연결이 될 때까지 대기한다.
        /// </summary>
        private async Task WaitForNetwork()
        {
            while (Volatile.Read(ref _disposeState) == 0 && !_runtime.IsNetworkAvailable)
            {
                AsyncDebugOutput.WriteLine("네트워크 연결 대기 중...(인터넷 연결이 되어 있는지 확인)");
                logger.LogInfo("네트워크 연결 대기 중...(인터넷 연결이 되어 있는지 확인)");
                await Task.Delay(2000, _lifetimeToken); // 2초 대기 후 다시 확인
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
        private AuctionRoundInfo? _cachedQcn;
        private DateTime _nextQcnCheckUtc = DateTime.MinValue;
        // 💡 [추가] API 모니터링 로그 주기적 출력을 위한 시각 기록 변수
        private DateTime _lastApiLogTime = DateTime.MinValue;
        /// <summary>
        /// 주기적으로 서버 데이터를 수신하는 루프를 시작한다.
        /// </summary>
        private async Task StartAsync()
        {
            if (_token == null || Volatile.Read(ref _disposeState) != 0 || _lifetimeToken.IsCancellationRequested)
                return;
            _isRunning = true;

            await _polling.StartAsync(async () =>
            {
                while (_isRunning && Volatile.Read(ref _disposeState) == 0)
                {
                    try
                    {
                        if (_userInfo == null || string.IsNullOrWhiteSpace(_token))
                        {
                            logger.LogError("StartAsync: 토큰값이 없습니다.");
                            await Task.Delay(1000, _lifetimeToken);
                            continue;
                        }

                        var currentUser = _userInfo;
                        var currentToken = _token;
                        // 경매가 실제로 진행 중일 때만 1초 주기로 동작하고, 경매 대기 중이거나 새벽 시간대에는 3~10초 주기로 동적으로 변환하도록 수정
                        if (runProcessMessageAsync || Ingress.NeedsResync)
                        {
                            var sw = Stopwatch.StartNew();
                            string date = !string.IsNullOrWhiteSpace(_currentRefreshDate)
                                ? _currentRefreshDate
                                : (string.IsNullOrWhiteSpace(currentUser.CurrentInfo?.Date)
                                    ? DateTime.Today.ToString("yyyyMMdd")
                                    : currentUser.CurrentInfo.Date);

                            if (ShouldRefreshQcn(date))
                            {
                                if (!await RefreshQcnStateAsync(currentUser, currentToken, date))
                                {
                                    await Task.Delay(200, _lifetimeToken);
                                    continue;
                                }
                            }
                            else if (HasCachedQcn(date))
                            {
                                await EnsureNettyConnectionAsync(currentUser, currentToken);
                            }

                            if (!HasCachedQcn(date))
                            {
                                await Task.Delay(GetDelayUntilNextQcnCheck(), _lifetimeToken);
                            }
                            else
                            {
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
                                logger.LogInfo($"[API 모니터링] 현재까지 누적 API 호출 건수: {_runtime.TotalApiCount}회");
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

                            if (AsyncDebugOutput.DiagnosticsEnabled)
                                AsyncDebugOutput.WriteDiagnostic($"[ProcessMessageAsync] 소요: {elapsed}ms, 대기: {delay}ms, 주기: {targetCycle}ms");

                            await Task.Delay(delay, _lifetimeToken);
                        }
                        else
                        {
                            // 메시지가 없을 때 CPU 점유율을 낮추기 위한 대기
                            await Task.Delay(200, _lifetimeToken);
                        }
                    }
                    catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested) { break; }
                    catch (Exception ex)
                    {
                        if (AsyncDebugOutput.Enabled)
                            AsyncDebugOutput.WriteLine("ProcessMessageAsync에서 예외 발생: " + ex.Message);
                        logger.LogError("ProcessMessageAsync에서 예외 발생 : " + ex.Message);

                        // 에러 발생 시 재시도 전 대기
                        await Task.Delay(5000, _lifetimeToken);

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

        private async Task<bool> RefreshQcnStateAsync(UserInfo currentUser, string currentToken, string date)
        {
            await EnsureNettyConnectionAsync(currentUser, currentToken);
            return await QueueQcnRequestAsync(currentUser, currentToken, date).ConfigureAwait(false);
        }

        private async Task<bool> QueueQcnRequestAsync(UserInfo currentUser, string currentToken, string date)
        {
            var submitted = new TaskCompletionSource<Task<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!Ingress.TryPost(() =>
            {
                string effectiveDate = !string.IsNullOrWhiteSpace(_currentRefreshDate)
                    ? _currentRefreshDate
                    : string.IsNullOrWhiteSpace(currentUser.CurrentInfo?.Date)
                        ? DateTime.Today.ToString("yyyyMMdd")
                        : currentUser.CurrentInfo.Date;
                if (!string.Equals(date, effectiveDate, StringComparison.Ordinal))
                {
                    submitted.TrySetResult(Task.FromResult(false));
                    return;
                }

                submitted.TrySetResult(Ingress.RequestAsync(async cancellation =>
                {
                    var qcn = await _dataSource.ReadQcnAsync(currentUser, currentToken, date).ConfigureAwait(false);
                    cancellation.ThrowIfCancellationRequested();
                    return () => ApplyQcnState(qcn, date);
                }));
            })) return false;

            var response = await submitted.Task.WaitAsync(_lifetimeToken).ConfigureAwait(false);
            return await response.WaitAsync(_lifetimeToken).ConfigureAwait(false);
        }

        private void ApplyQcnState(AuctionRoundInfo? qcn, string date)
        {
            var interval = qcn != null ? QcnCheckIntervalWhenExists : QcnCheckIntervalWhenMissing;
            _qcnCacheDate = date;
            _cachedQcn = qcn;
            _nextQcnCheckUtc = DateTime.UtcNow.Add(interval);
            isAuctionDate = qcn != null;
            if (qcn == null && !_timersStoppedByNoAuction)
            {
                logger.LogInfo("******** 오늘 차수데이터가 없습니다 ********");
                WeakReferenceMessenger.Default.Send(new DataStringMessage("오늘 차수데이터가 없습니다"));
                WeakReferenceMessenger.Default.Send(new DisplaySelectRefresh("StopTimer"));
                _timersStoppedByNoAuction = true;
            }
            else if (qcn != null && _timersStoppedByNoAuction)
            {
                WeakReferenceMessenger.Default.Send(new DisplaySelectRefresh("Refresh"));
                _timersStoppedByNoAuction = false;
            }
            logger.LogInfo($"RefreshQcnStateAsync: hasQcn={qcn != null}, nextCheckInMinutes={interval.TotalMinutes:0}");
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

            if (_runtime.IsConnected)
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
                await Task.Delay(200, _lifetimeToken);
                if (_runtime.IsConnected)
                    return;
            }

            isConnecting = true;
            try
            {
                await _runtime.DisconnectAsync();
                await Task.Delay(300, _lifetimeToken);
                AuctionConnectionResult connResult = await _runtime.ConnectAsync(currentUser, currentToken);

                if (connResult == AuctionConnectionResult.Connected)
                {
                    _duplicateCooldownUntilUtc = DateTime.MinValue;
                    _duplicateCooldownLogged = false;
                    return;
                }

                if (connResult == AuctionConnectionResult.Duplicate)
                {
                    _duplicateCooldownUntilUtc = DateTime.UtcNow.Add(DuplicateRetryCooldown);
                    _duplicateCooldownLogged = false;
                    WeakReferenceMessenger.Default.Send(new DataStringMessage("응찰 서버 중복 접속 감지, 잠시 후 재시도"));
                    logger.LogWarn($"EnsureNettyConnectionAsync: netty result = {connResult}, cooldown={DuplicateRetryCooldown.TotalSeconds:0}s");
                    await _runtime.DisconnectAsync();
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
            RequestStop();
            try { await Task.WhenAll(_polling.Completion, Ingress.Completion).ConfigureAwait(false); }
            catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested) { }
        }

        private void RequestStop()
        {
            if (Interlocked.Exchange(ref _stopRequested, 1) != 0) return;
            _isRunning = false;
            _polling.Dispose();
            _displayBatch.Dispose();
            Ingress.Dispose();
            logger.LogInfo($"[API 모니터링 종료] 최종 누적 API 호출 건수: {_runtime.TotalApiCount}회");
        }

        private DateTime _now = DateTime.Now;
        /// <summary>
        /// 서버에서 받은 데이터를 비교하고 변경사항을 브로드캐스트한다.
        /// </summary>
        private Task ProcessMessageAsync()
        {
            Ingress.Dispatch(() => QueueSnapshotRequest(_currentRefreshDate));
            return Task.CompletedTask;
        }

        private void QueueSnapshotRequest(string? date, bool refreshDisplay = false, int delayMilliseconds = 0)
        {
            var currentUser = _userInfo;
            var currentToken = _token;
            if (currentUser == null || string.IsNullOrWhiteSpace(currentToken)) return;
            string queryDate = date ?? currentUser.CurrentInfo?.Date ?? DateTime.Today.ToString("yyyyMMdd");
            long requestRevision = _itemStore.Revision;
            Ingress.TryRequest(async cancellation =>
            {
                long started = Stopwatch.GetTimestamp();
                if (delayMilliseconds > 0) await Task.Delay(delayMilliseconds, cancellation).ConfigureAwait(false);
                var raw = await _dataSource.ReadSnapshotAsync(currentUser, currentToken, queryDate).ConfigureAwait(false);
                cancellation.ThrowIfCancellationRequested();
                var parsed = ParseSnapshot(raw, currentUser);
                return () =>
                {
                    if (refreshDisplay) WeakReferenceMessenger.Default.Send(new DisplaySelectRefresh("Refresh"));
                    ApplySnapshotCore(parsed, queryDate, requestRevision);
                    if (AuctionSignalSummary.DiagnosticsEnabled)
                        logger.LogInfo($"auction-snapshot-applied rows={parsed.Count}, elapsed-ms={Stopwatch.GetElapsedTime(started).TotalMilliseconds:0}");
                    if (refreshDisplay) WeakReferenceMessenger.Default.Send(new DataStringMessage("경매 정보 새로고침을 완료했습니다."));
                };
            }, recovers: true, key: refreshDisplay || delayMilliseconds > 0 ? null : "poll",
                isCurrent: () => queryDate == SelectedSnapshotDate());
        }

        private string SelectedSnapshotDate() => _currentRefreshDate ?? _userInfo?.CurrentInfo?.Date ?? DateTime.Today.ToString("yyyyMMdd");

        private void SelectSnapshotDate(string date)
        {
            if (date != (_stateDate ?? _appliedSnapshotDate ?? SelectedSnapshotDate())) ResetAuctionState();
            _stateDate = date;
            _currentRefreshDate = date;
        }

        private List<gValues> ParseSnapshot(List<string> raw, UserInfo user)
        {
            return _snapshots.Parse(raw, user);
        }

        private void ApplySnapshot(List<gValues> snapshot, string? date = null)
            => ApplySnapshotCore(snapshot, date, _itemStore.Revision);

        private void ApplySnapshotCore(List<gValues> snapshot, string? date, long requestRevision)
        {
            FlushDisplayUpdates();
            bool canPreserveRunning = HasCurrentSnapshot;
            var latestSnapshot = snapshot.Select(item => item.Clone()).ToList();
            var beforeSnapshot = _beforeAuctionDataList;
            var sourceStatistics = BoardSourceStatistics.From(latestSnapshot);
            string? previousDate = _stateDate ?? _appliedSnapshotDate ?? _userInfo?.CurrentInfo?.Date;
            bool changesDate = date != null && previousDate != null && date != previousDate;
            string? previousRound = beforeSnapshot?.Select(item => item.AuctionRound).FirstOrDefault(value => !string.IsNullOrEmpty(value));
            string? nextRound = latestSnapshot.Select(item => item.AuctionRound).FirstOrDefault(value => !string.IsNullOrEmpty(value));
            bool changesRound = previousRound != null && nextRound != null && previousRound != nextRound && canPreserveRunning;
            bool recovers = Ingress.NeedsResync;
            if ((recovers && _observedResyncVersion != Ingress.CurrentResyncVersion) || changesDate || changesRound)
                ResetAuctionState();
            _observedResyncVersion = Ingress.CurrentResyncVersion;
            if (recovers) WeakReferenceMessenger.Default.Send(new DisplaySelectRefresh("Refresh"));
            if (date != null) _appliedSnapshotDate = _stateDate = date;
            bool full = beforeSnapshot == null || changesDate || changesRound || !canPreserveRunning || recovers;
            _itemStore.Bind(full ? null : beforeSnapshot);

            for (int index = 0; index < latestSnapshot.Count; index++)
            {
                var item = _itemStore.PreserveNewerIdentity(latestSnapshot[index], requestRevision);
                latestSnapshot[index] = item;
                var existing = !full ? _itemStore.Find(item.SipNumber) : null;
                bool replacesAnimal = existing != null && !string.IsNullOrEmpty(existing.TraceNumber) &&
                    !string.IsNullOrEmpty(item.TraceNumber) && existing.TraceNumber != item.TraceNumber;
                if (replacesAnimal)
                {
                    if (existing!.IsRunning || item.SipNumber == _runRunSipNumber.ToString())
                    {
                        latestSnapshot[index] = existing.Clone();
                        continue;
                    }
                    _itemStore.RemovePending(item.SipNumber);
                    _resultOverrides.Remove(item.SipNumber);
                }
                if (!full) _itemStore.MergeAbsentFields(item);
                _itemStore.Protect(item, requestRevision);
                if (_runRunSipNumber != -1 && item.SipNumber == _runRunSipNumber.ToString())
                {
                    var running = !full ? _itemStore.Find(item.SipNumber) : null;
                    if (running != null)
                    {
                        item.SpaceIndex = running.SpaceIndex;
                        item.LowestPrice = running.LowestPrice;
                    }
                    item.IsRunning = true;
                    item.AuctionResultStatus = "11";
                    ClearBidResult(item);
                }
            }
            _itemStore.ProtectMissing(latestSnapshot, requestRevision);
            if (!full && _runRunSipNumber != -1 && !latestSnapshot.Any(item => item.SipNumber == _runRunSipNumber.ToString()))
            {
                var running = _itemStore.Find(_runRunSipNumber.ToString());
                if (running != null) latestSnapshot.Add(running.Clone());
            }
            ApplyResultOverrides(latestSnapshot, full ? null : beforeSnapshot);
            var affected = new HashSet<string>(StringComparer.Ordinal);
            var nextBySip = latestSnapshot.GroupBy(item => AuctionItemStore.Key(item.SipNumber))
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
            if (!full)
            {
                foreach (var before in beforeSnapshot!)
                {
                    if (!nextBySip.TryGetValue(AuctionItemStore.Key(before.SipNumber), out var next) || !before.Equals(next))
                    {
                        affected.Add(before.SpaceIndex);
                        if (next != null) affected.Add(next.SpaceIndex);
                    }
                }
                foreach (var next in latestSnapshot)
                    if (_itemStore.Find(next.SipNumber) == null) affected.Add(next.SpaceIndex);
            }
            _latestAuctionDataList = latestSnapshot;
            _beforeAuctionDataList = latestSnapshot.OrderBy(item => item.EntityNumber).ToList();
            _itemStore.Bind(_beforeAuctionDataList);
            var updates = full ? (IReadOnlyList<gValues>)_beforeAuctionDataList : _itemStore.SelectSpaces(affected);
            bool statisticsChanged = _publishedStatistics != sourceStatistics;
            _publishedStatistics = sourceStatistics;
            if (full || updates.Count > 0 || statisticsChanged)
                WeakReferenceMessenger.Default.Send(new DataChangedMessage(updates, isFullSnapshot: full,
                    sourceStatistics: full || statisticsChanged ? sourceStatistics : null));
        }

        private void FlushDisplayUpdates() => _displayBatch.Flush();

        private sealed record ConfirmedBid(string Bidder, string Number, string Name, string Price, string RawNumber);
        private sealed record PendingResult(string Status, string? BidderNumber, string? Price, ConfirmedBid? Confirmed = null);
        // 재경매/날짜 전환 전에는 오래된 API 결과가 새 낙찰 정보를 덮지 않게 한다.
        private readonly Dictionary<string, PendingResult> _resultOverrides = new();
        private void ApplyResultOverrides(List<gValues> latest, List<gValues>? before)
        {
            foreach (var entry in _resultOverrides.ToArray())
            {
                var items = latest.Where(item => item.SipNumber == entry.Key).ToList();
                foreach (var item in items)
                    ApplyResultToItem(entry.Key, item);
            }
        }

        private static PendingResult ReadResult(string[] fields)
        {
            string? bidder = fields.Length > 5 && long.TryParse(fields[5], out long number) && number > 0
                ? number.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
            string? price = fields.Length > 6 && TryReadPrice(fields[6], out string parsed) ? parsed : null;
            return new PendingResult(fields[3], bidder, price);
        }

        private static bool ResultMatches(gValues item, PendingResult result)
        {
            if (item.AuctionResultStatus != result.Status) return false;
            if (result.Status == "23") return true;
            if (!long.TryParse(item.ResultBidderNumber, out long bidder) || bidder <= 0 ||
                !TryReadPrice(item.BidPrice, out string price)) return false;
            return (result.BidderNumber == null || bidder.ToString(System.Globalization.CultureInfo.InvariantCulture) == result.BidderNumber)
                && (result.Price == null || price == result.Price);
        }

        private void StoreResult(string sip, string[] fields)
        {
            var result = ReadResult(fields);
            if (_resultOverrides.TryGetValue(sip, out var previous) && previous.Status == result.Status &&
                (result.BidderNumber == null || previous.BidderNumber == result.BidderNumber) &&
                (result.Price == null || previous.Price == result.Price)) return;
            _resultOverrides[sip] = result;
        }

        private void ApplyResultToItem(string sip, gValues item, bool fromSnapshot = true)
        {
            if (!_resultOverrides.TryGetValue(sip, out var result)) return;
            bool confirmed = fromSnapshot && ResultMatches(item, result);
            item.IsRunning = false;
            item.AuctionResultStatus = result.Status;
            if (result.Status == "22" && confirmed)
            {
                TryReadPrice(item.BidPrice, out string price);
                result = result with
                {
                    BidderNumber = long.Parse(item.ResultBidderNumber).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Price = price,
                    Confirmed = new ConfirmedBid(item.Bidder, item.BidderNum, item.BidderString, item.BidPrice, item.ResultBidderNumber)
                };
                _resultOverrides[sip] = result;
            }
            if (result.Status == "22" && result.Confirmed is { } known)
            {
                item.Bidder = known.Bidder;
                item.BidderNum = known.Number;
                item.BidderString = known.Name;
                item.BidPrice = known.Price;
                item.ResultBidderNumber = known.RawNumber;
                return;
            }
            ClearBidResult(item);
            if (result.Status == "23")
            {
                item.BidPrice = "유찰";
                return;
            }
            item.BidPrice = result.Price ?? "-";
            string mode = item.BidderName?.Trim().ToUpperInvariant() ?? string.Empty;
            if (mode == "B") item.BidderNum = result.BidderNumber ?? string.Empty;
            else if (mode != "Y" && mode != "X") item.Bidder = result.BidderNumber ?? "-";
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

                var winner = CowAuctionSmall.Domain.Auction.CowDisplaySelector.SelectForSpaceIndex(candidates);
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

            if (AsyncDebugOutput.Enabled)
                AsyncDebugOutput.WriteLine("삭제된 데이터 갯수 : " + deleteDatas.Count);
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
            if (AsyncDebugOutput.Enabled)
                AsyncDebugOutput.WriteLine("추가된 데이터 갯수 : " + insertDatas.Count);
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

                if (AsyncDebugOutput.Enabled)
                    AsyncDebugOutput.WriteLine("\n계류대 위치가 바뀐 데이터 갯수 : " + beforeItems.Count);
                for (int i = 0; i < o2.Count; i++)
                {
                    AsyncDebugOutput.WriteLine($"원래 계류대 위치 : {beforeItems[i].SpaceIndex}\n바뀐 계류대 위치 :  {o2[i].SpaceIndex}");
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

                    var winner = CowAuctionSmall.Domain.Auction.CowDisplaySelector.SelectForSpaceIndex(candidates);
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

                if (AsyncDebugOutput.Enabled)
                    AsyncDebugOutput.WriteLine("이동 반영 최종 갱신 갯수 : " + updates.Count);
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
                AsyncDebugOutput.WriteLine(logMsg);
                logger.LogInfo(logMsg);
            }

            return source
                .Where(item => item != null && item.SipNumber != runningSip)
                .ToList();
        }

        /// <summary>
        /// 단일경매 종료를 즉시 반영하고 진행 상태를 해제한다.
        /// </summary>
        private static bool TryReadPrice(string? value, out string price)
        {
            price = string.Empty;
            if (!decimal.TryParse(value, System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out decimal amount) || amount < 0)
                return false;
            price = amount.ToString("0.############################", System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }

        private void ResetAuctionState()
        {
            FlushDisplayUpdates();
            _displayBatch.Clear();
            _itemStore.Reset();
            _runRunSipNumber = -1;
            _batchRunningState = false;
            _resultOverrides.Clear();
            var updates = (_beforeAuctionDataList ?? new List<gValues>()).Where(item => item.IsRunning).ToList();
            foreach (var item in updates) item.IsRunning = false;
            foreach (var item in _latestAuctionDataList ?? new List<gValues>()) item.IsRunning = false;
            if (updates.Count > 0) WeakReferenceMessenger.Default.Send(new DataChangedMessage(updates));
        }

        private void ObserveResyncBoundary()
        {
            long version = Ingress.CurrentResyncVersion;
            if (_observedResyncVersion == version) return;
            ResetAuctionState();
            _observedResyncVersion = version;
        }

        private static void ClearBidResult(gValues item)
        {
            item.BidPrice = "-";
            item.Bidder = "-";
            item.BidderNum = string.Empty;
            item.BidderString = string.Empty;
            item.ResultBidderNumber = string.Empty;
        }

        private void ApplyReceivedPrice(string[] data)
        {
            if (data.Length < 36) { logger.LogWarn("sv-update-rejected: incomplete packet"); return; }
            _itemStore.Bind(HasCurrentSnapshot ? _beforeAuctionDataList : null);
            var previous = _itemStore.Find(data[3]);
            AuctionItemUpdate update;
            try
            {
                update = _snapshots.DecodeUpdate(string.Join("|", data.Skip(1)),
                    _userInfo ?? new UserInfo { Auction = new CowAuctionSmall.Domain.Settings.Auction() }, true,
                    previous != null && previous.TraceNumber == data[6] ? previous : null);
            }
            catch (FormatException)
            {
                logger.LogWarn("sv-update-rejected: invalid fields");
                return;
            }
            string? expectedHouse = _userInfo?.Auction?.AuctionHouseCode;
            if (string.IsNullOrEmpty(update.Values.TraceNumber) && !string.IsNullOrEmpty(previous?.TraceNumber))
            {
                logger.LogWarn("sv-update-rejected: missing animal identity");
                return;
            }
            if (!string.IsNullOrEmpty(expectedHouse) && update.AuctionHouseCode != expectedHouse)
            {
                logger.LogWarn("sv-update-rejected: different auction house");
                return;
            }
            if (previous != null && !string.IsNullOrEmpty(previous.AuctionRound) && previous.AuctionRound != update.Round)
            {
                QueueSnapshotRequest(_currentRefreshDate);
                return;
            }
            if (previous?.IsRunning == true && !string.IsNullOrEmpty(previous.TraceNumber) &&
                previous.TraceNumber != update.Values.TraceNumber)
            {
                QueueSnapshotRequest(_currentRefreshDate);
                return;
            }
            if (previous != null && !string.IsNullOrEmpty(previous.TraceNumber) && previous.TraceNumber != update.Values.TraceNumber)
            {
                _itemStore.RemovePending(update.Values.SipNumber);
                _resultOverrides.Remove(update.Values.SipNumber);
            }
            bool protectResult = previous?.IsRunning == true || update.Values.SipNumber == _runRunSipNumber.ToString() ||
                _resultOverrides.ContainsKey(update.Values.SipNumber);
            var changed = _itemStore.Apply(update, protectResult);
            _displayBatch.Add(changed);
        }

        private void ApplySingleStart(string[] data)
        {
            if (data.Length < 5 || !int.TryParse(data[2], out int number) || number < 0)
            {
                logger.LogWarn("single-start-rejected: invalid entry");
                return;
            }
            string sip = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _itemStore.ClearResult(sip);
            _resultOverrides.Remove(sip);
            _runRunSipNumber = number;
            var updates = new List<gValues>();
            foreach (var item in HasCurrentSnapshot ? _beforeAuctionDataList ?? Enumerable.Empty<gValues>() : Enumerable.Empty<gValues>())
            {
                if (int.TryParse(item.SipNumber, out int itemNumber) && itemNumber == number)
                {
                    item.SipNumber = sip;
                    item.AuctionResultStatus = "11";
                    item.IsRunning = true;
                    ClearBidResult(item);
                    updates.Add(item);
                }
                else if (item.IsRunning)
                {
                    item.IsRunning = false;
                    updates.Add(item);
                }
            }
            if (updates.Count > 0)
                WeakReferenceMessenger.Default.Send(new DataChangedMessage(updates));
            if (!updates.Any(item => item.SipNumber == sip))
                QueueSnapshotRequest(_currentRefreshDate, refreshDisplay: true);
        }

        private void CompleteSingleAuction(string sipNumber, string? resultCode, string source, bool updateResultStatus)
        {
            if (!int.TryParse(sipNumber, out int parsedSip) || parsedSip < 0)
            {
                logger.LogWarn($"CompleteSingleAuction: sipNumber가 비어 있습니다. source={source}, result={resultCode ?? "-"}");
                return;
            }

            sipNumber = parsedSip.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _itemStore.ClearResult(sipNumber);
            if (resultCode == "24") _resultOverrides.Remove(sipNumber);

            lock (_lockObj)
            {
                List<gValues> updates = new List<gValues>();

                if (HasCurrentSnapshot && _beforeAuctionDataList != null)
                {
                    updates = _beforeAuctionDataList
                        .Where(item => item != null && item.SipNumber == sipNumber)
                        .ToList();
                }

                if (HasCurrentSnapshot && _latestAuctionDataList != null)
                {
                    foreach (var latestItem in _latestAuctionDataList.Where(item => item != null && item.SipNumber == sipNumber))
                    {
                        latestItem.IsRunning = false;
                        if (updateResultStatus && !string.IsNullOrWhiteSpace(resultCode))
                        {
                            ApplyResultToItem(sipNumber, latestItem, fromSnapshot: false);
                        }
                    }
                }

                foreach (var item in updates)
                {
                    item.IsRunning = false;
                    if (updateResultStatus && !string.IsNullOrWhiteSpace(resultCode))
                    {
                        ApplyResultToItem(sipNumber, item, fromSnapshot: false);
                    }
                }

                if (_runRunSipNumber.ToString() == sipNumber)
                {
                    _runRunSipNumber = -1;
                }

                logger.LogInfo(
                    $"single-auction-complete source={source}, result={resultCode ?? "-"}, updates={updates.Count}, clear-running={_runRunSipNumber == -1}");

                if (updates.Count > 0)
                {
                    WeakReferenceMessenger.Default.Send(new DataChangedMessage(updates));
                }
                else
                {
                    logger.LogWarn($"CompleteSingleAuction: 종료 대상이 없습니다. source={source}, result={resultCode ?? "-"}");
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
        private void OnStringArrMsg(object recipient, DataToServerGetArrMsg message)
        {
            var copy = new DataToServerGetArrMsg((string[])message.Data.Clone(), message.Refresh);
            Ingress.Dispatch(() => HandleStringArrMsg(copy), invalidates: true);
        }

        private void HandleStringArrMsg(DataToServerGetArrMsg message)
        {
            ObserveResyncBoundary();
            if (message.Data.Length < 2 || message.Data[1] != "SV") FlushDisplayUpdates();
            if (message.Refresh == "refresh")
            {
                if (_userInfo == null) return;
                var refreshed = ParseSnapshot(message.Data.ToList(), _userInfo);
                ApplySnapshot(refreshed);
                startRefesh = false;
                return;
            }
            if (message.Data.Length < 3) return;
            if (message.Data[1] == "SV")
            {
                ApplyReceivedPrice(message.Data);
                return;
            }
            if (message.Data[1] == "AS" && message.Data[2] == "8001" && message.Data.Length == 4 && message.Data[3] == "refresh")
            {
                QueueSnapshotRequest(_currentRefreshDate, refreshDisplay: true);
                return;
            }
            if (message.Data[1] == "AT")
            {
                if (!int.TryParse(message.Data[0], out int method) || (method != 10 && method != 20)) return;
                if (_auctionMethod != 0 && _auctionMethod != method) ResetAuctionState();
                _auctionMethod = method;
                return;
            }
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



            if (message.Data[0].Equals("20")) //단일 경매
            {
                if (code != "AS" && code != "refresh" && beforeList == null)
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
                                    logger.LogInfo("[경매 처리] AS|8006| 단일경매 진행 종료 처리");
                                    CompleteSingleAuction(message.Data[2], null, "AS8006", updateResultStatus: false);
                                    break;
                                case "8003": // 예정가 변경 또는 시작 준비
                                    // 예정가는 SV[27]에서 반영한다. AS[4]로 덮어쓰지 않는다.
                                    break;
                                case "8004": //경매 진행
                                    ApplySingleStart(message.Data);
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
                                    cowSV = _snapshots.DecodePacket(msgString2, currentUser);
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
                            cowSV = _snapshots.DecodePacket(msgString2, refreshUser);
                            _refreshAuchList2.Add(cowSV);
                        }
                        WeakReferenceMessenger.Default.Send(new DataChangedMessage(_refreshAuchList2, isFullSnapshot: true));
                        startRefesh = false;
                        WeakReferenceMessenger.Default.Send(new DataStringMessage("새로고침 완료, 총갯수 : " + _refreshAuchList2.Count + ", 변경시간 : " + DateTime.Now));
                        break;

                    default:
                        AsyncDebugOutput.WriteLine("OnStringArrMsg 에서 code값이 AS,SV 이외의 {0} 값이 나옴 {1}", code, message.Data.ToString());
                        break;
                }

            }
            else //일괄 경매 방식(msg = 경매방식, 코드, 경매상태)
            {
                switch (code)
                {
                    case "AS":
                        if (message.Data[2].Equals("8004")) //경매 진행상태라면
                        {
                            var tempList = (beforeList ?? Enumerable.Empty<gValues>()).Where(item => item.AuctionResultStatus.Equals("11")); //경매 진행중인것만
                            foreach (gValues cow in tempList)
                            {
                                currentSyncList.Add(cow);
                            }
                            WeakReferenceMessenger.Default.Send(new DataChangedMessage(currentSyncList));
                            AsyncDebugOutput.WriteLine("\n**************\n**************\n일괄 경매 시작\n**************\n**************");
                            _batchRunningState = true;
                        }
                        else if (message.Data[2].Equals("8006"))
                        {
                            AsyncDebugOutput.WriteLine("\n**************\n**************\n일괄 경매 끝\n**************\n**************");
                            _batchRunningState = false;

                            // 💡 [추가] 일괄경매 종료 신호 수신 즉시 API 데이터를 재조회하여 화면을 낙찰/유찰 상태로 전환
                            QueueSnapshotRequest(_currentRefreshDate, delayMilliseconds: 300);
                        }
                        break;
                    case "SZ":
                        string? requestedDate = message.Data.Length > 3 ? message.Data[3] : null;
                        if (DateTime.TryParseExact(requestedDate, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out _))
                        {
                            SelectSnapshotDate(requestedDate);
                            QueueSnapshotRequest(requestedDate, refreshDisplay: true);
                        }
                        else logger.LogWarn("OnStringArrMsg(SZ): 유효하지 않은 날짜입니다.");
                        break;
                    default:
                        AsyncDebugOutput.WriteLine("OnStringArrMsg 에서 code값이 AS,SV 이외의 {0} 값이 나옴 {1}", code, message.Data.ToString());
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
            Ingress.Dispatch(() => HandleStringMsg(message), invalidates: true);
        }

        private void HandleStringMsg(DataToServerGetMsg message)
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
            Ingress.Dispatch(() => HandleStringMsg8007(message));
        }

        private void HandleStringMsg8007(DataStringMessage8007 message)
        {
            ObserveResyncBoundary();
            if (message.Data.Equals("FINISH")) //회차 종료
            {
                ResetAuctionState();
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
            var copy = new DataToServerGetAF_SD((string[])message.Data.Clone());
            Ingress.Dispatch(() => HandleStringArrAF_SD(copy), invalidates: true);
        }

        private void HandleStringArrAF_SD(DataToServerGetAF_SD message)
        {
            ObserveResyncBoundary();
            FlushDisplayUpdates();
            if (message.Data.Length == 0) return;
            string[] msg = message.Data;
            string code = msg[0];

            switch (code)
            {
                case "AF":
                    if (msg.Length < 4 || !int.TryParse(msg[2], out int resultSip) || resultSip < 0)
                    {
                        logger.LogWarn("OnStringArrAF_SD(AF): 데이터 길이가 부족합니다.");
                        break;
                    }

                    switch (msg[3])
                    {
                        case "22":
                            StoreResult(resultSip.ToString(System.Globalization.CultureInfo.InvariantCulture), msg);
                            CompleteSingleAuction(msg[2], msg[3], "AF", updateResultStatus: true);
                            break;
                        case "23":
                            StoreResult(resultSip.ToString(System.Globalization.CultureInfo.InvariantCulture), msg);
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
            _displayBatch.Dispose();

            try
            {
                RequestStop();
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
        public void RequestSoldItemRecheck(string[] data)
        {
            var fields = (string[])data.Clone();
            if (fields.Length < 4 || (fields[3] != "22" && fields[3] != "23")) return;
            if (!int.TryParse(fields[2], out int requestedSip) || requestedSip < 0) return;
            string sipNumber = requestedSip.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Ingress.Dispatch(() =>
            {
                var user = _userInfo;
                var token = _token;
                if (user == null || string.IsNullOrWhiteSpace(token)) return;
                string date = SelectedSnapshotDate();
                long requestRevision = _itemStore.Revision;
                Ingress.TryRequest(async cancellation =>
                {
                    await Task.Delay(100, cancellation).ConfigureAwait(false);
                    var raw = await _dataSource.ReadSnapshotAsync(user, token, date).ConfigureAwait(false);
                    cancellation.ThrowIfCancellationRequested();
                    string? row = raw.FirstOrDefault(item =>
                    {
                        if (string.IsNullOrEmpty(item)) return false;
                        var values = item.Split('|');
                        return values.Length > 2 && int.TryParse(values[2], out int rowSip) && rowSip == requestedSip;
                    });
                    if (row == null) return null;
                    var snapshot = ParseSnapshot(raw, user);
                    var cow = snapshot.First(item => item.SipNumber == sipNumber);
                    if (cow.SipNumber != sipNumber) throw new FormatException("Auction result has an inconsistent entry number.");
                    cow.IsRunning = false;
                    return () =>
                    {
                        if (!_resultOverrides.ContainsKey(sipNumber)) return;
                        if (!HasCurrentSnapshot || _beforeAuctionDataList == null)
                        {
                            ApplySnapshotCore(snapshot, date, requestRevision);
                            return;
                        }
                        FlushDisplayUpdates();
                        _itemStore.Bind(_beforeAuctionDataList);
                        var before = _itemStore.Find(sipNumber);
                        if (before != null && !string.IsNullOrEmpty(before.AuctionRound) && before.AuctionRound != cow.AuctionRound)
                            return;
                        if (before != null && !string.IsNullOrEmpty(before.TraceNumber) && before.TraceNumber != cow.TraceNumber)
                            return;
                        _itemStore.MergeAbsentFields(cow);
                        _itemStore.Protect(cow, requestRevision);
                        ApplyResultToItem(sipNumber, cow);
                        _beforeAuctionDataList ??= new List<gValues>();
                        _beforeAuctionDataList.RemoveAll(item => item.SipNumber == cow.SipNumber);
                        _beforeAuctionDataList.Add(cow);
                        _beforeAuctionDataList = _beforeAuctionDataList.OrderBy(item => item.EntityNumber).ToList();
                        _itemStore.Bind(_beforeAuctionDataList);
                        if (before == null || !before.Equals(cow))
                            WeakReferenceMessenger.Default.Send(new DataChangedMessage(_itemStore.SelectSpaces(new[] { before?.SpaceIndex, cow.SpaceIndex })));
                    };
                }, isCurrent: () => date == SelectedSnapshotDate());
            });
        }

        /// <summary>디코더의 차수 지정 Refresh도 동일 요청 세대와 적용 경계를 사용한다.</summary>
        public void RequestRefresh(string qcn)
        {
            if (string.IsNullOrWhiteSpace(qcn)) return;
            Ingress.TryPost(() =>
            {
                var user = _userInfo;
                var token = _token;
                if (user == null || string.IsNullOrWhiteSpace(token)) return;
                long requestRevision = _itemStore.Revision;
                Ingress.TryRequest(async cancellation =>
                {
                    var round = await _dataSource.ReadQcnAsync(user, token, null, qcn).ConfigureAwait(false);
                    cancellation.ThrowIfCancellationRequested();
                    string? date = round?.AuctionDate;
                    if (!DateTime.TryParseExact(date, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out _)) return null;
                    string today = DateTime.Today.ToString("yyyyMMdd");
                    if (date == today) return null;
                    var todayRows = await _dataSource.ReadSnapshotAsync(user, token, today).ConfigureAwait(false);
                    cancellation.ThrowIfCancellationRequested();
                    if (todayRows.Count > 0) return null;
                    var rows = await _dataSource.ReadSnapshotAsync(user, token, date).ConfigureAwait(false);
                    cancellation.ThrowIfCancellationRequested();
                    var parsed = ParseSnapshot(rows, user);
                    return () =>
                    {
                        SelectSnapshotDate(date);
                        WeakReferenceMessenger.Default.Send(new DisplaySelectRefresh("Refresh"));
                        ApplySnapshotCore(parsed, date, requestRevision);
                        if (user.CurrentInfo != null) user.CurrentInfo.Date = date;
                        startRefesh = false;
                    };
                }, recovers: true);
            }); // AS8001이 이미 요청 세대를 갱신했다. 후속 SV가 즉시 조회를 취소하지 않게 한다.
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
