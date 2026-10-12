using CowAuctionSmall.Presentation.Assets;
using CowAuctionSmall.Infra.Logging;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Infra.Synchronization;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;
using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views.SIze_160_64;
using CowAuctionSmall.Views.Size_320_64;
using CowAuctionSmall.Views.Size128_128.Running;
using CowAuctionSmall.Views.Size128_64;
using CowAuctionSmall.Views.Size128_64.Running;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using UserInfo = CowAuctionSmall.Domain.Settings.UserInfo;

namespace CowAuctionSmall.Presentation.Display
{
    
    public class DisplaySelect : IDisposable
    {
        private readonly DisplaySizeParser.DisplaySize _displaySize;

        // 진행 중인 뷰모델을 관리하는 ObservableCollection
        public ObservableCollection<AuctionContPanelViewModel> TodayAuctionItems { get; private set; } //경매날 총 진행 중인 아이템들

        private UserInfo _userInfo;
        private BoardList _boardinfo;
        private string _nhCode = string.Empty; // 축협 사업장 코드

        // 페이지 시간 목록
        private List<int> pageTime = new List<int>();
        private int _totalRunningPage = 0;
        private int _requestedRunningPage = 1;

        // 화면 전환을 위한 타이머
        //private DispatcherTimer _timer;
        private System.Timers.Timer? _timer;
        private int _rotationIndex = 0;

        private Timer? _initTimer; // 매 정각마다 초기화 타이머
        private readonly PageTimerSync _pageTimerSync;
        private string? _lastRunningStartKey;
        private DateTime _lastRunningStartUtc = DateTime.MinValue;
        private bool _lockFirstPageUntilRefresh;
        private bool _stoppedBySignal;
        private DateTime _lastPageSyncReceivedUtc = DateTime.MinValue;
        private bool _isSubFallbackActive;
        private bool _isSubFallbackEnabled = true;
        private TimeSpan _subSyncTimeout = TimeSpan.FromSeconds(4);

        // 단일 경매 진행 시 플래그
        private bool singleAuctionmethodFlag = true;

        private readonly WeakReferenceMessenger _msgRefreshString;
        private SetCustomDisplay _setCustomDisplay;
        private AuctionViewFactory? _viewFactory;
        private AuctionViewFactory ViewFactory => _viewFactory ??= new AuctionViewFactory(_setCustomDisplay);

        private NLogger logger; // 로그용

        private LogoManager _logoManager;
        private readonly DisplayPanelStore _panelStates;
        private bool _isDisposed;

        private static int ParseIntOrDefault(string? value, int fallback)
        {
            return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
        }

        private static bool ParseBoolOrDefault(string? value, bool fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            var normalized = value.Trim().ToUpperInvariant();
            return normalized switch
            {
                "Y" or "YES" or "TRUE" or "1" => true,
                "N" or "NO" or "FALSE" or "0" => false,
                _ => fallback
            };
        }

        /// <summary>
        /// 디스플레이 선택 로직과 페이지 회전 상태를 초기화한다.
        /// </summary>
        public DisplaySelect(UserInfo userInfo, BoardList boardinfo)
        {
            logger = NLogger.Instance;

            _displaySize = DisplaySizeParser.Parse(boardinfo.Size ?? string.Empty);

            _msgRefreshString = WeakReferenceMessenger.Default;
            _msgRefreshString.Register<DisplaySelectRefresh>(this, OnRefreshMsg);

            _userInfo = userInfo;
            _boardinfo = boardinfo;

            var auction = _userInfo.Auction ??= new Auction();
            _nhCode = auction.AuctionHouseCode ?? string.Empty;

            _setCustomDisplay = new SetCustomDisplay();

            _logoManager = new LogoManager(_boardinfo);

            // 경매 페이지 수 설정
            var pageSetting = auction.PageSetting;
            UpdateRunningPageCount();

            int page1 = ParseIntOrDefault(pageSetting?.BoardPageTime ?? auction.BoardPageTime, 0);
            int page2 = ParseIntOrDefault(pageSetting?.BoardPageTime2 ?? auction.BoardPageTime2, 0);
            int page3 = ParseIntOrDefault(pageSetting?.BoardPageTime3 ?? auction.BoardPageTime3, 0);
            int page4 = ParseIntOrDefault(pageSetting?.BoardPageTime4 ?? auction.BoardPageTime4, 0);

            // 페이지 시간 설정
            pageTime.Add(page1 == 0 ? 25 : page1);
            pageTime.Add(page2 == 0 ? 7 : page2);
            pageTime.Add(page3 == 0 ? 5 : page3);
            pageTime.Add(page4 == 0 ? 5 : page4);

            TodayAuctionItems = new ObservableCollection<AuctionContPanelViewModel>();
            _panelStates = new DisplayPanelStore(TodayAuctionItems);
            TodayAuctionItems.Clear();

            _initTimer = new Timer(InitTimer_Tick, null, 0, 1000);

            var pageTimerPort = ParseIntOrDefault(pageSetting?.PageTimerPort ?? auction.PageTimerPort, 45123);
            var pageTimerHeartbeat = 1000;
            var pageTimerTimeout = 5000;
            var enableSubFallbackValue = pageSetting?.EnableSubFallback ?? auction.EnableSubFallback;
            var subFallbackTimeoutMsValue = pageSetting?.SubFallbackTimeoutMs ?? auction.SubFallbackTimeoutMs;

            _isSubFallbackEnabled = ParseBoolOrDefault(enableSubFallbackValue, true);
            var subFallbackTimeoutMs = ParseIntOrDefault(subFallbackTimeoutMsValue, 4000);
            _subSyncTimeout = TimeSpan.FromMilliseconds(Math.Clamp(subFallbackTimeoutMs, 1000, 20000));

            var pageDispatcher = System.Windows.Application.Current.Dispatcher;
            _pageTimerSync = new PageTimerSync(
                action => pageDispatcher.BeginInvoke(action),
                OnPageSyncReceived,
                OnMasterChanged,
                LogMessage,
                pageTimerPort,
                pageTimerHeartbeat,
                pageTimerTimeout);
            _pageTimerSync.Start();
            LogMessage($"[PageSync] config fallback-enabled={_isSubFallbackEnabled}, timeout-ms={(int)_subSyncTimeout.TotalMilliseconds}");

            // 타이머로 StartPageRotation 호출 지연
            DispatcherTimer delayTimer = new DispatcherTimer();
            //delayTimer.Interval = TimeSpan.FromSeconds(8); // 5초 지연
            delayTimer.Tick += (sender, e) =>
            {
                delayTimer.Stop(); // 타이머 중지
                StartPageRotation(); // 페이지 회전 시작
            };
            delayTimer.Start();
        }

        /// <summary>
        /// 최초 조회 전에 로그인한 사업장 정보를 화면 분기에 적용한다.
        /// </summary>
        public void ApplyAuthenticatedUserInfo(UserInfo userInfo)
        {
            System.Windows.Application.Current.Dispatcher.VerifyAccess();
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(DisplaySelect));
            if (string.IsNullOrWhiteSpace(userInfo.Auction?.AuctionHouseCode))
                throw new ArgumentException("로그인 사업장 코드가 없습니다.", nameof(userInfo));

            _userInfo = userInfo;
            _nhCode = userInfo.Auction.AuctionHouseCode;
            UpdateRunningPageCount();
        }

        /// <summary>
        /// 페이지 회전 타이머를 시작한다.
        /// </summary>
        private void StartPageRotation(bool allowSubFallback = false)
        {
            if (IsAuctionInProgress())
            {
                _rotationIndex = 0;
                ApplyRunningPageToAll(1);
                _timer?.Stop();
                LogRotationState("start-skip:auction-running");
                return;
            }
            if (_pageTimerSync != null && !_pageTimerSync.IsMaster && !allowSubFallback)
            {
                LogRotationState("start-skip:sub-mode");
                return;
            }
            if (_lockFirstPageUntilRefresh)
            {
                LogRotationState("start-skip:lock-first-page");
                return;
            }

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= Timer_Elapsed;
                _timer.Dispose();
                _timer = null;
            }

            _rotationIndex = 0; // 반드시 초기화
            _timer = new System.Timers.Timer(pageTime[_rotationIndex] * 1000); // 초기 Interval 설정
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = false; // Interval 재조정 필요
            _stoppedBySignal = false;
            _timer.Start();

            SyncCurrentPage();
            LogRotationState(allowSubFallback ? "start:fallback" : "start:normal");
        }

        private void StopPageRotation(bool forceFirstPage = false)
        {
            if (_timer == null)
            {
                if (forceFirstPage)
                {
                    _rotationIndex = 0;
                    ApplyRunningPageToAll(1);
                }
                LogRotationState(forceFirstPage ? "stop:force-first(no-timer)" : "stop:no-timer");
                return;
            }

            _timer.Stop();
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Dispose();
            _timer = null;

            if (forceFirstPage)
            {
                _rotationIndex = 0;
                ApplyRunningPageToAll(1);
            }

            LogRotationState(forceFirstPage ? "stop:force-first" : "stop");
        }

        private void OnMasterChanged(bool isMaster)
        {
            LogMessage($"[PageSync] {(isMaster ? "MASTER" : "SUB")} mode");
            if (isMaster)
            {
                _isSubFallbackActive = false;
            }
            if (isMaster)
            {
                if (ShouldFreezeRunningRotation())
                {
                    _timer?.Stop();
                    ApplyRunningPageToAll(1);
                    LogRotationState("role-change:master(freeze)");
                }
                else
                {
                    StartPageRotation();
                }
            }
            else
            {
                _lastPageSyncReceivedUtc = DateTime.UtcNow;
                _isSubFallbackActive = false;
                StopPageRotation();
            }
        }

        private void OnPageSyncReceived(PageSyncState state)
        {
            if (_pageTimerSync != null && _pageTimerSync.IsMaster)
                return;

            _lastPageSyncReceivedUtc = DateTime.UtcNow;
            if (_isSubFallbackActive)
            {
                _isSubFallbackActive = false;
                LogRotationState("fallback-exit:sync-received");
            }

            if (ShouldFreezeRunningRotation())
            {
                _rotationIndex = 0;
                ApplyRunningPageToAll(1);
                LogRotationState("sync-recv:freeze-first-page");
                return;
            }

            var pageIndex = Math.Clamp(state.PageIndex, 1, _totalRunningPage);
            var beforeIndex = _rotationIndex;
            _rotationIndex = pageIndex - 1;
            ApplyRunningPageToAll(pageIndex);

            if (beforeIndex != _rotationIndex)
            {
                LogRotationState($"sync-recv:page-{pageIndex}");
            }
        }

        private void SyncCurrentPage()
        {
            if (ShouldFreezeRunningRotation())
            {
                _rotationIndex = 0;
                ApplyRunningPageToAll(1);
                return;
            }

            var (index, secondsLeft) = RunningPageSchedule.Calculate(DateTime.UtcNow, _totalRunningPage, pageTime);
            _rotationIndex = index;
            ApplyRunningPageToAll(index + 1);
            _pageTimerSync?.UpdateState(new PageSyncState(index + 1, _totalRunningPage, secondsLeft, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
        }

        private void ApplyRunningPageToAll(int pageIndex)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(() => ApplyRunningPageToAll(pageIndex));
                return;
            }

            if (ShouldFreezeRunningRotation())
            {
                pageIndex = 1;
            }

            var todayItemsSnapshot = TodayAuctionItems.ToList();

            if (_userInfo.Auction?.AuctionHouseCode == "8808990657103")
            {
                foreach (AuctionContPanelViewModel viewModel in todayItemsSnapshot)
                {
                    if (viewModel.CowInfo.CowDistinction != "2")
                    {
                        DisplayRunningPageNum(viewModel._panel, viewModel.CowInfo, pageIndex);
                    }
                    else if (pageIndex <= 2)
                    {
                        DisplayRunningPageNum(viewModel._panel, viewModel.CowInfo, pageIndex);
                    }
                }
                PublishPageIndicatorState();
                return;
            }

            foreach (AuctionContPanelViewModel viewModel in todayItemsSnapshot)
            {
                DisplayRunningPageNum(viewModel._panel, viewModel.CowInfo, pageIndex);
            }
            PublishPageIndicatorState();
        }

        private void LogMessage(string message)
        {
            logger.LogInfo(message);
            WeakReferenceMessenger.Default.Send(new DataStringMessage(message));
        }

        private void LogRotationState(string reason)
        {
            var role = _pageTimerSync != null && _pageTimerSync.IsMaster ? "MASTER" : "SUB";
            var timerState = _timer != null && _timer.Enabled ? "on" : "off";
            var currentPage = _rotationIndex + 1;
            var maxPage = Math.Clamp(_totalRunningPage, 1, pageTime.Count);
            LogMessage($"[Rotation] {reason} role={role} fallback={_isSubFallbackActive} lock={_lockFirstPageUntilRefresh} running={IsAuctionInProgress()} timer={timerState} page={currentPage}/{maxPage}");
            PublishPageIndicatorState();
        }

        private void PublishPageIndicatorState()
        {
            var maxPage = Math.Clamp(_totalRunningPage, 1, pageTime.Count);
            var currentPage = Math.Clamp(_rotationIndex + 1, 1, maxPage);
            var isMaster = _pageTimerSync != null && _pageTimerSync.IsMaster;
            var snapshot = PageRotationSnapshot.Create(currentPage, maxPage, isMaster,
                _lockFirstPageUntilRefresh, IsAuctionInProgress(), _stoppedBySignal, _isSubFallbackActive,
                _timer?.Enabled == true);
            WeakReferenceMessenger.Default.Send(
                new PageIndicatorStateMessage(
                    snapshot.CurrentPage,
                    snapshot.TotalPages,
                    snapshot.IsMaster,
                    snapshot.IsFrozen,
                    snapshot.IsSubFallbackActive,
                    snapshot.FreezeReason,
                    snapshot.IsTimerRunning));
        }

        private void MonitorSubSyncFallback()
        {
            if (_pageTimerSync == null || _pageTimerSync.IsMaster)
            {
                if (_isSubFallbackActive)
                {
                    _isSubFallbackActive = false;
                    LogRotationState("fallback-exit:became-master");
                }
                return;
            }

            if (!_isSubFallbackEnabled)
            {
                return;
            }

            if (_totalRunningPage <= 1 || ShouldFreezeRunningRotation())
            {
                return;
            }

            if (_lastPageSyncReceivedUtc == DateTime.MinValue)
            {
                return;
            }

            var elapsed = DateTime.UtcNow - _lastPageSyncReceivedUtc;
            if (_isSubFallbackActive || elapsed < _subSyncTimeout)
            {
                return;
            }

            _isSubFallbackActive = true;
            LogMessage($"[PageSync] SUB fallback enter (sync-miss {elapsed.TotalSeconds:0.0}s)");
            StartPageRotation(allowSubFallback: true);
        }

        private string GetRunningStartKey(gValues cowInfo)
        {
            if (!string.IsNullOrWhiteSpace(cowInfo.SipNumber))
                return cowInfo.SipNumber;
            if (!string.IsNullOrWhiteSpace(cowInfo.SpaceIndex))
                return cowInfo.SpaceIndex;
            return cowInfo.UpdateSignature().ToString();
        }

        private static bool IsAuctionInProgress()
        {
            return ServerGetData._runRunSipNumber != -1;
        }

        private bool ShouldFreezeRunningRotation()
        {
            return _lockFirstPageUntilRefresh || IsAuctionInProgress();
        }

        private void ResetPageRotationNow(bool notifyMaster)
        {
            _rotationIndex = 0;
            ApplyRunningPageToAll(1);

            if (notifyMaster && _timer != null)
            {
                _timer.Interval = pageTime[0] * 1000;
                _timer.Stop();
                _timer.Start();
            }

            if (notifyMaster)
            {
                _pageTimerSync?.UpdateState(new PageSyncState(1, _totalRunningPage, pageTime[0], DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
            }
            PublishPageIndicatorState();
        }

        private void LockToFirstPageUntilRefresh(bool notifyMaster)
        {
            _stoppedBySignal = false;
            _lockFirstPageUntilRefresh = true;
            _rotationIndex = 0;
            ApplyRunningPageToAll(1);

            if (_timer != null)
            {
                _timer.Stop();
            }

            if (notifyMaster)
            {
                _pageTimerSync?.UpdateState(new PageSyncState(1, _totalRunningPage, pageTime[0], DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
            }

            LogRotationState("lock-first-page");
        }



        private DateTime _lastHourSync = DateTime.MinValue; // 마지막 동기화 시간 기록
        /// <summary>
        /// 정시 동기화 타이머를 처리한다.
        /// </summary>
        private void InitTimer_Tick(object? state)
        {
            if (_totalRunningPage == 1 || singleAuctionmethodFlag == false) //총 보여줄 페이지가 1개이거나 단일경매 진행했다면
            {
                StopInitTimer();
                return;
            }

            // 현재 시간이 정각 50분인지 확인
            var currentTime = DateTime.Now;

            var isSyncTime =
                currentTime.Minute == 34 &&
                currentTime.Hour != _lastHourSync.Hour &&
                currentTime.Second <= 2;

            MonitorSubSyncFallback();

            AsyncDebugOutput.WriteDiagnostic("InitTimer_Tick");
            if (isSyncTime)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    //ShowFirstPage();
                });
                _lastHourSync = currentTime;

                AsyncDebugOutput.WriteLine("1시간 동기화: 페이지를 첫 번째로 초기화했습니다. 34분에 한번씩");
                //                logger.LogInfo("1시간 동기화: 페이지를 첫 번째로 초기화했습니다.34분에 한번씩");
            }
        }

        // 로고를 여러 개 보여줘야 할 경우 한 줄마다 다르게 보여주기 위한 메서드

        /// <summary>
        /// 지정 패널에 로고 화면을 표시한다.
        /// </summary>
        public void DisplayLogo(VirtualizingStackPanel panel)
        {
            var state = _panelStates.Get(panel);
            var logoPath = _logoManager.GetLogoPath(panel.Name);
            if (!File.Exists(logoPath))
            {
                var fallbackPath = _logoManager.GetDefaultLogoPath();
                if (File.Exists(fallbackPath))
                {
                    logoPath = fallbackPath;
                }
                else
                {
                    logger.LogWarn($"로고 파일이 없어 표시를 건너뜁니다. panel={panel.Name}, path={logoPath}, fallback={fallbackPath}");
                    _panelStates.ClearRunning(state, panel);
                    panel.Children.Clear();
                    state.Mode = PanelDisplayMode.Logo;
                    state.LastData = null;
                    return;
                }
            }

            if (state.Mode == PanelDisplayMode.Logo &&
                state.LogoView != null &&
                panel.Children.Count == 1 &&
                panel.Children[0] == state.LogoView &&
                string.Equals(state.LogoPath, logoPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            // 💡 잔여 뷰모델 및 NoteHost 완벽 정리
            _panelStates.ClearRunning(state, panel);
            panel.Children.Clear();

            try
            {
                if (state.LogoView == null ||
                    !string.Equals(state.LogoPath, logoPath, StringComparison.OrdinalIgnoreCase))
                {
                    state.LogoView = ImageCacheHelper.CreateLogoImage(logoPath, _logoManager.GetDefaultLogoPath());
                    state.LogoPath = logoPath;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"로고 로드 실패 panel={panel.Name}, path={logoPath}, err={ex.Message}");
                state.Mode = PanelDisplayMode.Logo;
                state.LastData = null;
                return;
            }

            panel.Children.Add(state.LogoView);
            state.Mode = PanelDisplayMode.Logo;
            state.LastData = null;
        }

        // 패널에서 뷰모델을 제거하는 메서드
        /// <summary>
        /// 패널에 연결된 진행 뷰모델을 제거한다.
        /// </summary>
        private void RemoveViewModelFromPanel(VirtualizingStackPanel panel)
        {
            var viewModel = TodayAuctionItems.FirstOrDefault(vm => vm._panel == panel);
            if (viewModel != null)
            {
                TodayAuctionItems.Remove(viewModel);

                // 메모리 해제
                viewModel.Dispose();
                panel.DataContext = null;

                // 패널의 모든 UI 요소 제거
                panel.Children.Clear();
            }
        }

        /// <summary>
        /// 진행할 화면 넣기 추후 여러 페이지 보여주기
        /// </summary>
        public void DisplayRunning(VirtualizingStackPanel panel, gValues cowInfo, int auctionmethod)
        {
            var state = _panelStates.Get(panel);
            var runningKey = BuildRunningKey(cowInfo);
            var isSameRunning = state.Mode == PanelDisplayMode.Running && state.RunningViewModel != null && state.RunningKey == runningKey;

            if (cowInfo.IsRunning)
            {
                var startKey = GetRunningStartKey(cowInfo);
                var now = DateTime.UtcNow;
                if (!string.Equals(_lastRunningStartKey, startKey, StringComparison.Ordinal) ||
                    (now - _lastRunningStartUtc).TotalSeconds >= 2)
                {
                    _lastRunningStartKey = startKey;
                    _lastRunningStartUtc = now;
                    var notifyMaster = _pageTimerSync != null && _pageTimerSync.IsMaster;
                    LockToFirstPageUntilRefresh(notifyMaster);
                }
            }

            if (!isSameRunning)
            {
                _panelStates.ClearRunning(state, panel);
                panel.Children.Clear();

                var viewModel = new AuctionContPanelViewModel(cowInfo, this, panel, pageTime, _totalRunningPage);
                TodayAuctionItems.Add(viewModel);
                panel.DataContext = viewModel;
                state.RunningViewModel = viewModel;
                state.RunningKey = runningKey;
                state.Mode = PanelDisplayMode.Running;
                state.LastData = cowInfo.Clone();

                InitializePages(panel, viewModel, _requestedRunningPage, cowInfo.Is_Nh_QQuri, cowInfo.Is_Ｎh_Excellent, cowInfo.Is_Mother_Ｎh_Excellent, cowInfo.CowDistinction);
            }
            else
            {
                if (state.LastData == null || !state.LastData.Equals(cowInfo))
                {
                    state.RunningViewModel!.UpdateCowInfo(cowInfo);
                    state.LastData = cowInfo.Clone();
                }
                if (state.RunningPages == null || state.RunningPages.Count != _totalRunningPage)
                {
                    InitializePages(panel, state.RunningViewModel!, _requestedRunningPage, cowInfo.Is_Nh_QQuri, cowInfo.Is_Ｎh_Excellent, cowInfo.Is_Mother_Ｎh_Excellent, cowInfo.CowDistinction);
                }
            }

            DisplayRunningPageNum(panel, cowInfo, 1);

            // 염소 경매인 경우
            if (cowInfo.CowDistinction.Equals("5"))
            {
                DisplayRunningPageNum(panel, cowInfo, 1);
                if (_timer != null)
                {
                    _timer.Stop();
                }

                StopInitTimer();
                PublishPageIndicatorState();
            }

            switch (auctionmethod)
            {
                case 10: // 일괄 경매
                    break;
                case 20: // 단일 경매
                    HandleSingleAuction(cowInfo);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// 단일 경매 진행 시 타이머를 중지하고 첫 페이지를 고정한다.
        /// </summary>
        private void HandleSingleAuction(gValues cowInfo)
        {
            if (!cowInfo.IsRunning || !singleAuctionmethodFlag)
                return;

            // 안전하게 타이머 중지 시도
            if (_timer != null)
            {
                try
                {
                    if (_timer.Enabled)
                    {
                        _timer.Stop();
                        AsyncDebugOutput.WriteLine("[단일경매] 타이머 중지 완료");
                    }
                    else
                    {
                        AsyncDebugOutput.WriteLine("[단일경매] 타이머는 이미 중지됨");
                    }
                }
                catch (Exception ex)
                {
                    AsyncDebugOutput.WriteLine($"[단일경매] 타이머 중지 실패: {ex.Message}");
                    return; // 타이머 중지 실패 시 flag 유지
                }
            }
            else
            {
                AsyncDebugOutput.WriteLine("[단일경매] 타이머 객체가 null입니다.");
                return; // 타이머가 없으면 중지 못하므로 flag 유지
            }

            // 타이머 중지 성공한 경우에만 flag 변경
            singleAuctionmethodFlag = false;

            // 첫 페이지 고정 출력
            foreach (AuctionContPanelViewModel vm in TodayAuctionItems.ToList())
            {
                DisplayRunningPageNum(vm._panel, vm.CowInfo, 1);
            }

            AsyncDebugOutput.WriteLine("[단일경매] 첫 페이지 고정 완료");
            PublishPageIndicatorState();
        }



        /// <summary>
        /// 진행 페이지 컨트롤을 생성하거나 캐시를 갱신한다.
        /// </summary>
        public void InitializePages(VirtualizingStackPanel panel, AuctionContPanelViewModel viewModel, int totalRunningPage, string is_QQuri,string is_Ｎh_Excellent, string is_Mother_Ｎh_Excellent, string CowDistinction)
        {
            totalRunningPage = Math.Clamp(totalRunningPage, 1, 4);

            var state = _panelStates.Get(panel);
            var existingPages = state.RunningPages ?? panel.Children.OfType<UserControl>().ToList();

            var options = CreateViewOptions(is_QQuri, CowDistinction, is_Ｎh_Excellent, is_Mother_Ｎh_Excellent);
            if (_displaySize == DisplaySizeParser.DisplaySize.Unknown)
                options = options with { NhCode = string.Empty };
            Func<int, UserControl?> createPage = pageNumber =>
            {
                var pageName = IsStandardDisplaySize(_displaySize) ? $"RunPage{pageNumber}" : $"RunPage{pageNumber}_64";
                if (_displaySize == DisplaySizeParser.DisplaySize.Size128x64 && options.NhCode == "8808990806426")
                    pageName = AuctionViewFactory.IsSokchoEvent(options) ? $"SokchoEventRunPage{pageNumber}" : $"SokchoRunPage{pageNumber}";
                return existingPages.FirstOrDefault(p => p.Name == pageName)
                    ?? ViewFactory.CreateRunning(options, pageNumber);
            };

            var registeredPages = new List<UserControl>(totalRunningPage);
            for (int i = 1; i <= totalRunningPage; i++)
            {
                var page = createPage(i);
                if (page == null) continue;

                if (string.IsNullOrWhiteSpace(page.Name))
                {
                    page.Name = $"RunPage{i}";
                }

                SetPageProperties(page, viewModel, panel);

                registeredPages.Add(page);
            }

            state.RunningPages = registeredPages;
            var previousTotalPages = _totalRunningPage;
            _totalRunningPage = Math.Max(1, registeredPages.Count);
            _rotationIndex = Math.Clamp(_rotationIndex, 0, _totalRunningPage - 1);
            if (previousTotalPages != _totalRunningPage)
                PublishPageIndicatorState();

            state.UseRunningNoteHost = AuctionViewFactory.ShouldUseNoteHost(options);

            if (!state.UseRunningNoteHost)
            {
                state.RunningNoteHost = null;
            }
        }

        /// <summary>
        /// 로그인 후 원래 설정에서 생성할 페이지 수를 읽고, 실제 순환 수는 페이지 등록 시 확정한다.
        /// </summary>
        private void UpdateRunningPageCount()
        {
            var auction = _userInfo.Auction;
            var boardPageValue = auction?.PageSetting?.BoardPage ?? auction?.BoardPage;
            int requestedPages = string.IsNullOrWhiteSpace(boardPageValue) ? 1 : Convert.ToInt32(boardPageValue);
            _requestedRunningPage = AuctionViewFactory.LimitRunningPageCount(CreateViewOptions(string.Empty, "1"), requestedPages);
            _totalRunningPage = 1;
            _rotationIndex = 0;
        }

        /// <summary>
        /// 표준 해상도 여부를 판단한다.
        /// </summary>
        private bool IsStandardDisplaySize(DisplaySizeParser.DisplaySize size)
        {
            return size == DisplaySizeParser.DisplaySize.Size128x128 ||
                   size == DisplaySizeParser.DisplaySize.Size160x64 ||
                   size == DisplaySizeParser.DisplaySize.Size320x64;
        }



        /// <summary>
        /// 화면 사이즈와 페이지 번호에 맞는 페이지를 생성한다.
        /// </summary>
        private UserControl? CreatePageBySize(int pageNumber, DisplaySizeParser.DisplaySize size, string is_QQuri , string CowDistinction, string is_Ｎh_Excellent, string is_Mother_Ｎh_Excellent)
        {
            var options = CreateViewOptions(is_QQuri, CowDistinction, is_Ｎh_Excellent, is_Mother_Ｎh_Excellent) with { Size = size };
            return ViewFactory.CreateRunning(options, pageNumber);
        }

        private AuctionViewOptions CreateViewOptions(string isQquri, string cowDistinction, string isNhExcellent = "N", string isMotherNhExcellent = "N", string abilityNumber = "")
        {
            return new AuctionViewOptions
            {
                // 운영의 기존 알 수 없는 해상도 fallback은 유지하고 점검 도구는 명시적으로 검증한다.
                Size = _displaySize == DisplaySizeParser.DisplaySize.Unknown ? DisplaySizeParser.DisplaySize.Size128x64 : _displaySize,
                NhCode = _nhCode,
                IsQquri = isQquri,
                CowDistinction = cowDistinction,
                IsNhExcellent = isNhExcellent,
                IsMotherNhExcellent = isMotherNhExcellent,
                IsShowQquri = _userInfo.Auction?.IsShowQQuri ?? string.Empty,
                LowestPriceTitle = _userInfo.Auction?.LowestPriceTitle ?? string.Empty,
                AbilityNumber = abilityNumber
            };
        }


        // 페이지 속성을 설정하는 메서드
        /// <summary>
        /// 페이지 공통 속성과 데이터 컨텍스트를 설정한다.
        /// </summary>
        private void SetPageProperties(UserControl page, AuctionContPanelViewModel viewModel, VirtualizingStackPanel panel)
        {
            page.DataContext = viewModel;
            page.Visibility = Visibility.Collapsed;
            page.Width = panel.Width;
            page.Height = panel.Height;
        }

        // 경매 진행 페이지 번호를 표시하는 메서드
        /// <summary>
        /// 진행 화면에서 지정된 페이지를 표시한다.
        /// </summary>
        public void DisplayRunningPageNum(VirtualizingStackPanel panel, gValues cowinfo, int pageNum)
        {
            var state = _panelStates.Get(panel);
            if (state.RunningPages == null || state.RunningPages.Count == 0) return;

            VirtualizingPanel.SetIsVirtualizing(panel, true);
            VirtualizingPanel.SetVirtualizationMode(panel, VirtualizationMode.Recycling);

            // 디스플레이 Enum 기준 인덱스 조정 불필요 — 모든 Enum은 표준화된 비교 가능
            int index = Math.Clamp(pageNum, 1, state.RunningPages.Count) - 1;
            var page = state.RunningPages[index];

            var useHostForPage = state.UseRunningNoteHost &&
                AuctionViewFactory.ShouldUseNoteHost(CreateViewOptions(cowinfo.Is_Nh_QQuri, cowinfo.CowDistinction), page);

            if (useHostForPage)
            {
                var host = state.RunningNoteHost ??= new RunningNoteHost128();
                if (!ReferenceEquals(host.ScrollOwner, state.RunningViewModel))
                {
                    BindingOperations.ClearBinding(host, RunningNoteHost128.ItemDisplayProperty);
                    host.ScrollOwner = state.RunningViewModel;
                    host.SetBinding(RunningNoteHost128.ItemDisplayProperty, new Binding(nameof(AuctionContPanelViewModel.ItemDisplay))
                    {
                        Source = state.RunningViewModel,
                        Mode = BindingMode.OneWay
                    });
                }
                host.Width = panel.Width;
                host.Height = panel.Height;

                if (panel.Children.Count != 1 || panel.Children[0] != host)
                {
                    panel.Children.Clear();
                    panel.Children.Add(host);
                }

                if (!ReferenceEquals(host.PageContent, page))
                {
                    host.PageContent = page;
                }

                page.Visibility = Visibility.Visible;
                return;
            }

            // 이미 해당 페이지 보이는 중이면 스킵
            if (panel.Children.Count == 1 && panel.Children[0] == page)
            {
                return;
            }

            panel.Children.Clear();
            page.Visibility = Visibility.Visible;
            panel.Children.Add(page);
        }


        /// <summary>
        /// 낙찰된 화면 넣기 (128x128 해상도 및 일반 우군일 경우 RunningNoteHost128 공통 호스트 적용)
        /// </summary>
        public void DisplaySold(VirtualizingStackPanel panel, gValues cowInfo)
        {
            var state = _panelStates.Get(panel);
            var soldKey = BuildSoldKey(cowInfo);

            // 💡 128x128 해상도이면서 염소(5) / 말(6)이 아닌 경우 RunningNoteHost128 사용
            var useNoteHost = AuctionViewFactory.ShouldUseNoteHost(CreateViewOptions(cowInfo.Is_Nh_QQuri, cowInfo.CowDistinction));

            // 이미 동일한 낙찰 화면이 표시 중인 경우
            if (state.Mode == PanelDisplayMode.Sold && state.SoldView != null && state.SoldKey == soldKey)
            {
                if (state.LastData == null || !state.LastData.Equals(cowInfo))
                {
                    var itemDisplay = new AuctionItemDisplayViewModel(cowInfo);
                    cowInfo = itemDisplay.Snapshot;
                    state.SoldView.DataContext = itemDisplay;
                    if (useNoteHost && state.RunningNoteHost != null)
                    {
                        SetResultNoteItem(state.RunningNoteHost, itemDisplay);
                    }
                    state.LastData = cowInfo;
                }
                return;
            }

            _panelStates.ClearRunning(state, panel);
            panel.Children.Clear();
            panel.DataContext = null;

            // 뷰 객체 생성 또는 재사용
            if (state.SoldView == null || state.SoldKey != soldKey)
            {
                state.SoldView = ViewFactory.CreateSold(CreateViewOptions(cowInfo.Is_Nh_QQuri, cowInfo.CowDistinction));
                state.SoldKey = soldKey;
            }

            var soldDisplay = new AuctionItemDisplayViewModel(cowInfo);
            cowInfo = soldDisplay.Snapshot;
            state.SoldView.DataContext = soldDisplay;
            state.SoldView.Width = panel.Width;
            state.SoldView.Height = panel.Height;
            state.SoldView.Visibility = Visibility.Visible;

            // 💡 RunningNoteHost128 호스트 래핑 처리
            if (useNoteHost)
            {
                var host = state.RunningNoteHost ??= new RunningNoteHost128();
                host.Width = panel.Width;
                host.Height = panel.Height;
                SetResultNoteItem(host, soldDisplay);

                if (!ReferenceEquals(host.PageContent, state.SoldView))
                {
                    host.PageContent = state.SoldView;
                }

                panel.Children.Add(host);
            }
            else
            {
                panel.Children.Add(state.SoldView);
            }

            state.Mode = PanelDisplayMode.Sold;
            state.LastData = cowInfo;
        }


        /// <summary>
        /// 유찰된 화면 넣기 (128x128 해상도 및 일반 우군일 경우 RunningNoteHost128 공통 호스트 적용)
        /// </summary>
        public void DisplayUnSold(VirtualizingStackPanel panel, gValues cowInfo)
        {
            var state = _panelStates.Get(panel);
            var unSoldKey = BuildUnSoldKey(cowInfo);

            // 💡 128x128 해상도이면서 염소(5) / 말(6)이 아닌 경우 RunningNoteHost128 사용
            var useNoteHost = AuctionViewFactory.ShouldUseNoteHost(CreateViewOptions(cowInfo.Is_Nh_QQuri, cowInfo.CowDistinction));

            // 이미 동일한 유찰 화면이 표시 중인 경우
            if (state.Mode == PanelDisplayMode.UnSold && state.UnSoldView != null && state.UnSoldKey == unSoldKey)
            {
                if (state.LastData == null || !state.LastData.Equals(cowInfo))
                {
                    var itemDisplay = new AuctionItemDisplayViewModel(cowInfo);
                    cowInfo = itemDisplay.Snapshot;
                    state.UnSoldView.DataContext = itemDisplay;
                    if (useNoteHost && state.RunningNoteHost != null)
                    {
                        SetResultNoteItem(state.RunningNoteHost, itemDisplay);
                    }
                    state.LastData = cowInfo;
                }
                return;
            }

            _panelStates.ClearRunning(state, panel);
            panel.Children.Clear();
            panel.DataContext = null;

            // 뷰 객체 생성 또는 재사용
            if (state.UnSoldView == null || state.UnSoldKey != unSoldKey)
            {
                state.UnSoldView = ViewFactory.CreateUnSold(CreateViewOptions(cowInfo.Is_Nh_QQuri, cowInfo.CowDistinction, abilityNumber: cowInfo.Nh_ability_1_num));
                state.UnSoldKey = unSoldKey;
            }

            var unSoldDisplay = new AuctionItemDisplayViewModel(cowInfo);
            cowInfo = unSoldDisplay.Snapshot;
            state.UnSoldView.DataContext = unSoldDisplay;
            state.UnSoldView.Width = panel.Width;
            state.UnSoldView.Height = panel.Height;
            state.UnSoldView.Visibility = Visibility.Visible;

            // 💡 RunningNoteHost128 호스트 래핑 처리
            if (useNoteHost)
            {
                var host = state.RunningNoteHost ??= new RunningNoteHost128();
                host.Width = panel.Width;
                host.Height = panel.Height;
                SetResultNoteItem(host, unSoldDisplay);

                if (!ReferenceEquals(host.PageContent, state.UnSoldView))
                {
                    host.PageContent = state.UnSoldView;
                }

                panel.Children.Add(host);
            }
            else
            {
                panel.Children.Add(state.UnSoldView);
            }

            state.Mode = PanelDisplayMode.UnSold;
            state.LastData = cowInfo;
        }

        private static void SetResultNoteItem(RunningNoteHost128 host, AuctionItemDisplayViewModel itemDisplay)
        {
            BindingOperations.ClearBinding(host, RunningNoteHost128.ItemDisplayProperty);
            host.ScrollOwner = null;
            host.ItemDisplay = itemDisplay;
        }

        /// <summary>
        /// 패널의 바인딩과 자식 컨트롤을 정리한다.
        /// </summary>
        private void ClearPanelBindings(VirtualizingStackPanel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is UserControl control)
                {
                    // UserControl 해제
                    if (control.DataContext is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    control.DataContext = null;
                }
            }
            panel.Children.Clear(); // 자식 요소 제거
            panel.DataContext = null; // 데이터 컨텍스트 해제
        }


        /// <summary>
        /// 현재 화면의 이력 표시만 갱신하며 페이지 선택과 경매 진행 제어는 실행하지 않는다.
        /// </summary>
        public void UpdateHistoryDisplay(VirtualizingStackPanel panel, gValues cowInfo)
        {
            var state = _panelStates.Get(panel);
            if (state.LastData == null || state.LastData.Equals(cowInfo)) return;
            if (state.LastData.TraceNumber != cowInfo.TraceNumber ||
                state.LastData.SipNumber != cowInfo.SipNumber || state.LastData.SpaceIndex != cowInfo.SpaceIndex) return;

            if (state.Mode == PanelDisplayMode.Running && state.RunningViewModel != null)
            {
                state.RunningViewModel.UpdateCowInfo(cowInfo);
                state.LastData = state.RunningViewModel.CowInfo.Clone();
                return;
            }

            var view = state.Mode switch
            {
                PanelDisplayMode.Sold => state.SoldView,
                PanelDisplayMode.UnSold => state.UnSoldView,
                _ => null
            };
            if (view == null) return;
            var itemDisplay = new AuctionItemDisplayViewModel(cowInfo);
            view.DataContext = itemDisplay;
            if (state.RunningNoteHost != null) state.RunningNoteHost.ItemDisplay = itemDisplay;
            state.LastData = itemDisplay.Snapshot;
        }

        // 패널을 이름으로 찾는 메서드
        public void FindPanel(VirtualizingStackPanel? panel, gValues gv, int auctionmethod)
        {
            if (panel == null)
            {
                if (gv.IsRunning)
                {
                    if (AsyncDebugOutput.Enabled)
                        AsyncDebugOutput.WriteLine("해당 패널이 없습니다." + gv.SpaceIndex + " 진행 여부 : " + gv.IsRunning);
                    if (singleAuctionmethodFlag)
                    {
                        AsyncDebugOutput.WriteLine("단일경매 시작");
                        singleAuctionmethodFlag = false;
                    }
                    TimeerStop_FirstPage();
                }
                return;
            }

            switch (AuctionStateResolver.Resolve(gv))
            {
                case DisplayAuctionState.Ready:
                case DisplayAuctionState.Running:
                    DisplayRunning(panel, gv, auctionmethod);
                    break;
                case DisplayAuctionState.Sold:
                    DisplaySold(panel, gv);
                    //DisplayRunning(panel, gv, auctionmethod);
                    break;
                case DisplayAuctionState.Unsold:
                    DisplayUnSold(panel, gv);
                    break;
                default:
                    DisplayLogo(panel);
                    break;
            }
        }

        /// <summary>
        /// 진행 화면 구성에 영향을 주는 키를 만든다.
        /// </summary>
        private string BuildRunningKey(gValues cowInfo)
        {
            return $"{cowInfo.Is_Nh_QQuri}|{cowInfo.Nh_ability_1_num}|{cowInfo.CowDistinction}";
        }

        /// <summary>
        /// 낙찰 화면 구성에 영향을 주는 키를 만든다.
        /// </summary>
        private string BuildSoldKey(gValues cowInfo)
        {
            return $"{_nhCode}|{_userInfo.Auction?.BidderName ?? string.Empty}|{_userInfo.Auction?.IsShowQQuri ?? string.Empty}|{cowInfo.CowDistinction}|{_userInfo.Auction?.LowestPriceTitle ?? string.Empty}";
        }

        /// <summary>
        /// 유찰 화면 구성에 영향을 주는 키를 만든다.
        /// </summary>
        private string BuildUnSoldKey(gValues cowInfo)
        {
            return $"{_nhCode}|{cowInfo.Is_Nh_QQuri}|{cowInfo.CowDistinction}|{cowInfo.Nh_ability_1_num}";
        }

        /// <summary>
        /// 새로고침 메시지에 따라 타이머 동작을 제어한다.
        /// </summary>
        private void OnRefreshMsg(object recipient, DisplaySelectRefresh message)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (_isDisposed || dispatcher?.HasShutdownStarted == true) return;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send,
                    new Action(() => OnRefreshMsg(recipient, message)));
                return;
            }

            var todayItemsSnapshot = TodayAuctionItems.ToList();
            if (message.Data == "StopTimer") _stoppedBySignal = true;
            else if (message.Data == "Refresh") _stoppedBySignal = false;
            bool hasNonGoatAuction = todayItemsSnapshot.Any(cow => cow.CowInfo.CowDistinction != "5"); // 소 경매날인 경우
            bool isAnyRunning = todayItemsSnapshot.Any(cow => cow.CowInfo.IsRunning == true);

            if (message.Data == "Refresh")
            {
                _lockFirstPageUntilRefresh = false;
                LogRotationState("refresh:unlock-first-page");
            }

            if ((hasNonGoatAuction || isAnyRunning) && message.Data == "Refresh")
            {
                var animalType = todayItemsSnapshot.FirstOrDefault(cow => cow.CowInfo.CowDistinction != "5");
                if (animalType != null && ServerGetData._runRunSipNumber < 0)
                {
                    AsyncDebugOutput.WriteLine($"[Refresh] _runRunSipNumber < 0 : {ServerGetData._runRunSipNumber}");

                    StartInitTimer();
                    StartPageRotation();

                    if (_timer != null && !_timer.Enabled)
                    {
                        _timer.Start();
                        WeakReferenceMessenger.Default.Send(new DataStringMessage("[Refresh] 타이머 재시작"));
                        PublishPageIndicatorState();
                    }

                    singleAuctionmethodFlag = true;
                }
            }
            else
            {
                StopPageRotation(forceFirstPage: true);
                StopInitTimer();
                AsyncDebugOutput.WriteLine("[Refresh] 염소 전용 또는 비진행 상태 → 타이머 정지 + 1페이지 고정");
                LogRotationState("refresh:stop-and-force-first");
            }
        }




        /// <summary>
        /// 페이지 회전 타이머 이벤트를 처리한다.
        /// </summary>
        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_timer == null) return;  // 타이머가 정지된 경우 실행 방지

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (_timer == null) return; // 다시 한 번 체크
                if (ShouldFreezeRunningRotation())
                {
                    _rotationIndex = 0;
                    ApplyRunningPageToAll(1);
                    _timer.Stop();
                    LogRotationState("timer:freeze-force-first");

                    if (_pageTimerSync != null && _pageTimerSync.IsMaster)
                    {
                        _pageTimerSync.UpdateState(new PageSyncState(1, _totalRunningPage, pageTime[0], DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
                    }
                    return;
                }
                if (_pageTimerSync != null && !_pageTimerSync.IsMaster && !_isSubFallbackActive)
                {
                    PublishPageIndicatorState();
                    return;
                }

                var (index, secondsLeft) = RunningPageSchedule.Calculate(DateTime.UtcNow, _totalRunningPage, pageTime);
                _rotationIndex = index;

                ApplyRunningPageToAll(index + 1);
                if (_pageTimerSync != null && _pageTimerSync.IsMaster)
                {
                    _pageTimerSync.UpdateState(new PageSyncState(index + 1, _totalRunningPage, secondsLeft, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
                }

                if (AsyncDebugOutput.DiagnosticsEnabled)
                    AsyncDebugOutput.WriteDiagnostic($"[UTC 동기화] 현재 페이지: {_rotationIndex + 1} / 총 페이지: {_totalRunningPage}, 남은 시간: {pageTime[_rotationIndex]}초");

                _timer.Interval = (pageTime[_rotationIndex] * 1000);
                _timer.Start();
                PublishPageIndicatorState();
            });
        }



        // _initTimer 중지
        /// <summary>
        /// 정시 동기화 타이머를 중지한다.
        /// </summary>
        public void StopInitTimer()
        {
            if (_initTimer != null)
            {
                _initTimer.Dispose();
                _initTimer = null;
            }
        }

        // _initTimer 시작
        /// <summary>
        /// 정시 동기화 타이머를 시작한다.
        /// </summary>
        public void StartInitTimer()
        {
            if (_initTimer == null)
            {
                //_initTimer = new Timer(InitTimer_Tick, null, 0, 1000);
                _initTimer = new Timer(InitTimer_Tick, null, 0, 1000);
            }
        }

        /// <summary>
        /// 페이지 회전을 멈추고 첫 페이지를 고정 표시한다.
        /// </summary>
        public void TimeerStop_FirstPage()
        {
            var timer = _timer;
            if (timer != null)
            {
                try
                {
                    timer.Stop();
                }
                catch (ObjectDisposedException)
                {
                    // 이미 Dispose 된 타이머일 수 있으므로 무시한다.
                }
            }
            else
            {
                AsyncDebugOutput.WriteLine("[TimeerStop_FirstPage] 타이머 객체가 null입니다.");
            }

            foreach (AuctionContPanelViewModel vm in TodayAuctionItems.ToList())
            {
                if (vm?._panel == null || vm.CowInfo == null)
                {
                    continue;
                }
                DisplayRunningPageNum(vm._panel, vm.CowInfo, 1);
            }
            PublishPageIndicatorState();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            _msgRefreshString.Unregister<DisplaySelectRefresh>(this);

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= Timer_Elapsed;
                _timer.Dispose();
                _timer = null;
            }

            if (_initTimer != null)
            {
                _initTimer.Dispose();
                _initTimer = null;
            }

            _pageTimerSync.Dispose();
            _panelStates.Dispose();
            GC.SuppressFinalize(this);
        }


        
    }
}
