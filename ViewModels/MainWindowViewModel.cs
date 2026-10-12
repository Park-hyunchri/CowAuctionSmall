using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.Infra.Logging;
using CowAuctionSmall.Infra.XmlSettings;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Application.AnimalHistory;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.AnimalHistory;
using CowAuctionSmall.Domain.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;


namespace CowAuctionSmall.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject, IDisposable
    {
        private readonly WeakReferenceMessenger _messenger;
        private readonly WeakReferenceMessenger _messengerStringMsg;

        private XmlParserCont _xmlParserCont;

        public ObservableCollection<VirtualizingStackPanel> Panels { get; private set; } // 전체적인 패널 관리

        private readonly ObservableCollection<gValues> _currentCowList = new ObservableCollection<gValues>();
        private readonly Dispatcher _dispatcher;
        public VirtualizingStackPanel? MainContainer { get; private set; } // null 허용으로 변경

        [ObservableProperty]
        private int _mainWindowWidth;
        [ObservableProperty]
        private int _mainWindowHeight;

        [ObservableProperty]
        private string? _mainWindowTextBox; // null 허용으로 변경

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AuctionSignalDisplayText))]
        private string _auctionSignalText = "경매 신호 대기";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AuctionSignalDisplayText))]
        private string _auctionSectionText = string.Empty;

        public string AuctionSignalDisplayText => string.IsNullOrEmpty(AuctionSectionText)
            ? AuctionSignalText : $"{AuctionSignalText} | {AuctionSectionText}";

        [ObservableProperty]
        private string _historyProgressText = "이력 조회 설정 대기";

        public MainStatusViewModel StatusBar { get; } = new();
        private int _boardContentHeight;
        private int _footerHeight = 130;

        public void UpdateFooterHeight(double height)
        {
            if (_disposed || !double.IsFinite(height) || height <= 0) return;
            _footerHeight = (int)Math.Ceiling(height);
            MainWindowHeight = _boardContentHeight + _footerHeight + 4;
        }

        [ObservableProperty]
        private double _mainPositionX;
        [ObservableProperty]
        private double _mainPositionY;

        // 각각의 패널 크기
        private int _eachPanelWidth;
        private int _eachPanelHeight;

        private bool _first = true;

        private readonly AnimalHistoryService? _history;
        private bool _historyReady;
        private bool _showOwnerChangeNote = true;
        private readonly AnimalHistoryCoordinator? _historyCoordinator;
        private readonly Dictionary<string, gValues> _visibleCows = new Dictionary<string, gValues>();
        private bool _disposed;
        private DisplaySelect? _displaySelect;
        private BoardDisplayTracker? _boardTracker;
        private BoardDisplayCoverage? _lastCoverage;
        private readonly DisplayUpdateTracker _displayUpdates = new();
        private readonly Dictionary<string, List<VirtualizingStackPanel>> _panelsByName = new(StringComparer.Ordinal);
        private ServerGetData _serverGetData;

        private int _auctionmethod = 0; // 단일 일괄경매방식

        /// <summary>
        /// 메인 화면의 패널과 메시지 구독을 초기화한다.
        /// </summary>
        public MainWindowViewModel(XmlParserCont xmlParserCont, ServerGetData serverGetData, AnimalHistoryService? history = null)
        {
            var logger = NLogger.Instance;

            VersionShow();

            _dispatcher = Dispatcher.CurrentDispatcher;

            _messenger = WeakReferenceMessenger.Default;
            _messenger.Register<DataChangedMessage>(this, OnDataChanged);
            _messenger.Register<DisplaySelectRefresh>(this, OnDisplayRefresh);

            _messengerStringMsg = WeakReferenceMessenger.Default;
            _messengerStringMsg.Register<DataStringMessage>(this, OnDataStringMsg);
            _messengerStringMsg.Register<AuctionSignalMessage>(this, OnAuctionSignal);
            _messengerStringMsg.Register<AuctionSectionMessage>(this, OnAuctionSection);
            _messengerStringMsg.Register<PageIndicatorStateMessage>(this, OnPageIndicatorStateMsg);
            _messengerStringMsg.Register<NettyConnectionResultMessage>(this, OnNettyConnectionResultMsg);
            _messengerStringMsg.Register<AuctionDisplayStateMessage>(this, OnAuctionDisplayStateMsg);

            this._serverGetData = serverGetData;
            _history = history;
            _historyCoordinator = history == null ? null : new AnimalHistoryCoordinator(
                history.LookupAsync, history.GetCachedNoteAsync, () => history.Epoch,
                action =>
                {
                    if (_dispatcher.HasShutdownStarted) throw new InvalidOperationException("History display dispatcher is shutting down.");
                    _dispatcher.BeginInvoke(action);
                }, ApplyHistoryResult);
            if (_history != null)
            {
                _historyCoordinator!.ProgressChanged += OnHistoryProgress;
                _history.DayChanged += OnHistoryDayChanged;
                _history.Status += OnHistoryStatus;
            }

            Panels = new ObservableCollection<VirtualizingStackPanel>();
            _xmlParserCont = xmlParserCont;

            // null 가능성 검사 추가
            var r = _xmlParserCont.XmlPaserResult();
            if (r.board == null || r.userInfo == null)
            {
                logger.LogError("MainWindowViewModel: board 또는 userInfo가 null입니다.");
                return;
            }

            WarnDuplicateBoardIndices(r.board);

            InitializeBoardSize(r.board);
            InitializeUserInfo(r.userInfo);
            StatusBar.SetRootFarm(r.userInfo.Auction?.IsShowQQuri);

            // 로그인 전에는 기존 패널과 로고만 표시한다.
            (r.userInfo.Auction ??= new Auction()).AuctionHouseCode = string.Empty;
            _displaySelect = new DisplaySelect(r.userInfo, r.board);
            InitCreateStackPanel(r.board);
            _boardTracker = new BoardDisplayTracker(Panels.Select(panel => panel.Name.Substring(4)));
            _ = Task.Run(() => _serverGetData.InitializeAsync(runtimeUser =>
                _dispatcher.Invoke(() =>
                {
                    if (_disposed)
                        throw new ObjectDisposedException(nameof(MainWindowViewModel));
                    _displaySelect.ApplyAuthenticatedUserInfo(runtimeUser);
                    StatusBar.SetRootFarm(runtimeUser.Auction?.IsShowQQuri);
                    _history?.Configure(runtimeUser.AnimalTrace);
                    _historyReady = runtimeUser.AnimalTrace.Enabled;
                    if (!_historyReady) _historyCoordinator?.Reset();
                    _showOwnerChangeNote = runtimeUser.AnimalTrace.ShowOwnerChangeNote;
                    HistoryProgressText = _historyReady ? "이력 조회 대상 대기" : "이력 조회 비활성 · 사용 설정과 서비스 키를 확인해 주세요";
                })));

            MainWindowTextBox += "뿌리농가 적용 버전" + "\n";
        }

        private void OnHistoryStatus(string text)
        {
            OnDataStringMsg(this, new DataStringMessage(text));
        }

        private void OnHistoryProgress(HistoryBatchProgress progress)
        {
            if (!_disposed && _historyReady) HistoryProgressText = progress.Text;
        }

        private void OnHistoryDayChanged()
        {
            _historyCoordinator?.Reset();
            _dispatcher.BeginInvoke(new Action(() =>
            {
                if (_disposed) return;
                foreach (var pair in _visibleCows.ToList())
                {
                    var copy = pair.Value.Clone();
                    copy.NoteChangeOwner = "";
                    copy.HistoryOwner = null;
                    _visibleCows[pair.Key] = copy;
                }
                UpdatePanels(new ObservableCollection<gValues>(_visibleCows.Values));
            }));
        }

        private void ApplyHistoryResult(HistoryItemUpdate update)
        {
            if (_disposed || !_historyReady || _displaySelect == null ||
                _historyCoordinator?.IsCurrent(update) != true || !update.Result.HasValue) return;

            // Recheck current occupants and copy only matching items; auction data can change during lookup.
            try
            {
                string note = _showOwnerChangeNote ? update.Result.Note : string.Empty;
                var slots = _visibleCows.Where(pair => IsHistoryAnimal(pair.Value) &&
                    string.Equals(pair.Value.TraceNumber, update.Number, StringComparison.Ordinal))
                    .Select(pair => pair.Key).ToArray();
                foreach (var slot in slots)
                {
                    var current = _visibleCows[slot];
                    if (current.NoteChangeOwner == note && current.HistoryOwner == update.Result.LatestOwner) continue;
                    var panelName = "Cow_" + slot;
                    if (!_panelsByName.TryGetValue(panelName, out var panels)) continue;
                    var copy = current.Clone();
                    copy.NoteChangeOwner = note;
                    copy.HistoryOwner = update.Result.LatestOwner;
                    UpdateItemPanels(panelName, panels, copy, historyOnly: true);
                    _visibleCows[slot] = copy;
                }
            }
            catch (Exception)
            {
                OnHistoryStatus("소유주 이력을 표시하지 못했습니다. 잠시 후 다시 확인해 주세요.");
                throw; // The coordinator reports failed UI publication instead of a false completion.
            }
        }

        private IEnumerable<string> DisplayedHistoryNumbers()
        {
            return _visibleCows.Values.Where(IsHistoryAnimal)
                .OrderBy(item => int.TryParse(item.SpaceIndex, out int slot) ? slot : int.MaxValue)
                .ThenBy(item => item.SpaceIndex, StringComparer.Ordinal).Select(item => item.TraceNumber);
        }

        public System.Windows.Input.ICommand RefreshOwnerHistoryCommand => new RelayCommand(RefreshDisplayedOwnerHistory);

        private static bool IsHistoryAnimal(gValues item) => item.CowDistinction == "1" || item.CowDistinction == "2" || item.CowDistinction == "3";

        public void RefreshDisplayedOwnerHistory()
        {
            if (!_historyReady) return;
            _historyCoordinator?.UpdateTargets(DisplayedHistoryNumbers(), refresh: true);
        }

        private void WarnDuplicateBoardIndices(BoardList boardInfo)
        {
            var boardDetails = BuildBoardDuplicateDetails(boardInfo);
            var logoDetails = BuildLogoDuplicateDetails(boardInfo);

            if (boardDetails.Count == 0 && logoDetails.Count == 0)
            {
                return;
            }

            var message = new StringBuilder();
            if (boardDetails.Count > 0)
            {
                message.AppendLine("Board.XML RowIdx 중복 발견");
                foreach (var detail in boardDetails)
                {
                    message.AppendLine(detail);
                }
            }

            if (logoDetails.Count > 0)
            {
                if (message.Length > 0)
                {
                    message.AppendLine();
                }

                message.AppendLine("Board.XML LogoRowIdx 중복 발견");
                foreach (var detail in logoDetails)
                {
                    message.AppendLine(detail);
                }
            }

            MessageBox.Show(message.ToString(), "Board.XML 중복 경고", MessageBoxButton.OK, MessageBoxImage.Warning);
        }


        private static List<string> BuildBoardDuplicateDetails(BoardList boardInfo)
        {
            var occurrences = new Dictionary<int, List<(string BoardName, int RowIndex)>>();
            var boards = boardInfo.MultiBoards;
            if (boards == null)
            {
                return new List<string>();
            }

            foreach (var board in boards)
            {
                if (board?.Rows == null)
                {
                    continue;
                }

                var boardName = string.IsNullOrWhiteSpace(board.Name) ? "(no name)" : board.Name;
                for (int rowIndex = 0; rowIndex < board.Rows.Count; rowIndex++)
                {
                    var row = board.Rows[rowIndex];
                    if (row == null)
                    {
                        continue;
                    }

                    foreach (var idx in row)
                    {
                        if (!occurrences.TryGetValue(idx, out var list))
                        {
                            list = new List<(string BoardName, int RowIndex)>();
                            occurrences[idx] = list;
                        }

                        list.Add((boardName, rowIndex + 1));
                    }
                }
            }

            var details = new List<string>();
            foreach (var entry in occurrences.Where(kvp => kvp.Value.Count > 1).OrderBy(kvp => kvp.Key))
            {
                var count = entry.Value.Count;
                foreach (var occurrence in entry.Value.OrderBy(value => value.BoardName).ThenBy(value => value.RowIndex))
                {
                    details.Add($"- Board: {occurrence.BoardName} / Row: {occurrence.RowIndex} / Value: {entry.Key} (중복 {count}회)");
                }
            }

            return details;
        }

        private static List<string> BuildLogoDuplicateDetails(BoardList boardInfo)
        {
            var occurrences = new Dictionary<int, List<(string LogoBoardName, string RowLabel)>>();
            var logoBoards = boardInfo.LogoBoard;
            if (logoBoards == null)
            {
                return new List<string>();
            }

            foreach (var logoBoard in logoBoards)
            {
                if (logoBoard?.Rows == null)
                {
                    continue;
                }

                var logoBoardName = string.IsNullOrWhiteSpace(logoBoard.Name) ? "(no name)" : logoBoard.Name;
                for (int rowIndex = 0; rowIndex < logoBoard.Rows.Count; rowIndex++)
                {
                    var row = logoBoard.Rows[rowIndex];
                    if (row?.Rows == null)
                    {
                        continue;
                    }

                    var rowLabel = string.IsNullOrWhiteSpace(row.ID)
                        ? $"Row {rowIndex + 1}"
                        : $"ID {row.ID} (Row {rowIndex + 1})";

                    foreach (var idx in row.Rows)
                    {
                        if (!occurrences.TryGetValue(idx, out var list))
                        {
                            list = new List<(string LogoBoardName, string RowLabel)>();
                            occurrences[idx] = list;
                        }

                        list.Add((logoBoardName, rowLabel));
                    }
                }
            }

            var details = new List<string>();
            foreach (var entry in occurrences.Where(kvp => kvp.Value.Count > 1).OrderBy(kvp => kvp.Key))
            {
                var count = entry.Value.Count;
                foreach (var occurrence in entry.Value.OrderBy(value => value.LogoBoardName).ThenBy(value => value.RowLabel))
                {
                    details.Add($"- LogoBoard: {occurrence.LogoBoardName} / LogoRowIdx: {occurrence.RowLabel} / Value: {entry.Key} (중복 {count}회)");
                }
            }

            return details;
        }


        /// <summary>
        /// 사용자 설정을 읽어 화면 시작 위치를 설정한다.
        /// </summary>
        private void InitializeUserInfo(UserInfo userInfo)
        {
            if (userInfo.Auction?.StartPosition != null)
            {
                var startPositionParts = userInfo.Auction.StartPosition.Split(",");
                if (startPositionParts.Length == 2)
                {
                    MainPositionX = double.Parse(startPositionParts[0]);
                    MainPositionY = double.Parse(startPositionParts[1]);
                }
            }
        }

        /// <summary>
        /// 보드 크기를 기준으로 메인 윈도우 크기를 계산한다.
        /// </summary>
        private void InitializeBoardSize(BoardList board)
        {
            if (board.Size != null)
            {
                var sizeParts = board.Size.Split(',');
                if (sizeParts.Length == 2)
                {
                    _eachPanelWidth = int.Parse(sizeParts[0]);
                    _eachPanelHeight = int.Parse(sizeParts[1]);
                }
            }

            var rows = board.MultiBoards?.FirstOrDefault()?.Rows;
            if (rows != null && rows.Count > 0)
            {
                MainWindowWidth = rows[0].Length * _eachPanelWidth;
                _boardContentHeight = rows.Count * _eachPanelHeight;
                MainWindowHeight = _boardContentHeight + (_footerHeight > 0 ? _footerHeight : 130) + 4;
            }
        }

        /// <summary>
        /// 데이터 변경 메시지를 받아 화면 갱신을 요청한다.
        /// </summary>
        private void OnDataChanged(object recipient, DataChangedMessage message)
        {
            if (_disposed || _dispatcher.HasShutdownStarted) return;
            if (message.Data.Count > 0 || message.IsFullSnapshot || message.SourceStatistics != null)
            {
                _dispatcher.Invoke(() =>
                {
                    if (_disposed) return;
                    var selected = _boardTracker?.Apply(message.Data, message.IsFullSnapshot, message.SourceStatistics)
                        ?? message.Data;
                    if (message.IsFullSnapshot)
                    {
                        var present = selected.Select(item => item.SpaceIndex).ToHashSet(StringComparer.Ordinal);
                        foreach (var slot in _visibleCows.Keys.Where(slot => !present.Contains(slot)).ToArray())
                        {
                            _visibleCows.Remove(slot);
                            _displayUpdates.Remove("Cow_" + slot);
                            if (_panelsByName.TryGetValue("Cow_" + slot, out var missingPanels))
                                foreach (var panel in missingPanels) _displaySelect?.DisplayLogo(panel);
                        }
                    }
                    foreach (var removed in selected.Where(item => item.AuctionResultStatus == "00" && item.SipNumber == "-1"))
                    {
                        _visibleCows.Remove(removed.SpaceIndex);
                        _displayUpdates.Remove("Cow_" + removed.SpaceIndex);
                        if (_panelsByName.TryGetValue("Cow_" + removed.SpaceIndex, out var emptyPanels))
                            foreach (var panel in emptyPanels) _displaySelect?.DisplayLogo(panel);
                    }
                    var changed = selected.Where(item => item.AuctionResultStatus != "00" || item.SipNumber != "-1").ToList();
                    if (changed.Count > 0) UpdatePanels(new ObservableCollection<gValues>(changed));
                    else if (message.Data.Count > 0 || message.IsFullSnapshot)
                        _historyCoordinator?.UpdateTargets(_historyReady ? DisplayedHistoryNumbers() : Array.Empty<string>());
                    var coverage = _boardTracker?.Coverage;
                    if (coverage != null && coverage != _lastCoverage)
                    {
                        _lastCoverage = coverage;
                        string summary = $"[보드] 최근 전체 행={coverage.RawRowCount}, 무효={coverage.InvalidRowCount}, 중복={coverage.DuplicateRowCount}, " +
                            $"현재 계류대={coverage.ValidSpaceCount}, 일치={coverage.MatchedSpaceCount}, 제외={coverage.ExcludedSpaceCount}, " +
                            $"슬롯={coverage.BoardSlotCount}, 표시={coverage.DisplayedSlotCount}, 로고={coverage.LogoSlotCount}";
                        NLogger.Instance.LogInfo(summary);
                        OnDataStringMsg(this, new DataStringMessage(summary));
                    }
                });
            }
        }

        private void OnDisplayRefresh(object recipient, DisplaySelectRefresh message)
        {
            if (_disposed || _dispatcher.HasShutdownStarted || message.Data != "Refresh") return;
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => OnDisplayRefresh(recipient, message)));
                return;
            }
            _displayUpdates.Clear();
        }


        /// <summary>
        /// 현재 데이터 목록을 기준으로 각 패널을 갱신한다.
        /// </summary>
        public void UpdatePanels(ObservableCollection<gValues> currentCowList)
        {
            if (_displaySelect == null)
            {
                return;
            }

            // 초기 배치에서 만든 색인을 재사용한다. 같은 이름의 물리 슬롯도 함께 갱신한다.
            var panelDict = _panelsByName;

            // UI 업데이트를 한 번에 실행하기 위해 리스트에 저장
            List<Action> updates = new List<Action>();

            var groupedBySpaceIndex = currentCowList
                .Where(item => item != null)
                .GroupBy(item => item.SpaceIndex);

            foreach (var group in groupedBySpaceIndex)
            {
                var winner = CowAuctionSmall.Domain.Auction.CowDisplaySelector.SelectForSpaceIndex(group);
                if (winner == null)
                {
                    continue;
                }

                var panelName = "Cow_" + winner.SpaceIndex;
                if (!panelDict.TryGetValue(panelName, out var panels))
                {
                    //Debug.WriteLine($"패널을 찾지 못해 업데이트를 건너뜁니다. name={panelName}, sip={winner.SipNumber}, running={winner.IsRunning}");
                    continue;
                }
                _visibleCows[winner.SpaceIndex] = winner;
                var history = _historyReady && IsHistoryAnimal(winner) && _historyCoordinator != null &&
                    _historyCoordinator.TryGetHistory(winner.TraceNumber, out var loadedHistory) ? loadedHistory : null;
                winner.NoteChangeOwner = _showOwnerChangeNote ? history?.Note ?? string.Empty : string.Empty;
                winner.HistoryOwner = history?.LatestOwner;
                updates.Add(() => UpdateItemPanels(panelName, panels, winner));
            }

            // UI 스레드에서 한 번만 실행 (UI 렌더링 최적화)
            _dispatcher.Invoke(() =>
            {
                foreach (var update in updates)
                {
                    update();
                }
                _historyCoordinator?.UpdateTargets(_historyReady ? DisplayedHistoryNumbers() : Array.Empty<string>());
            });
        }





        private void UpdateItemPanels(string panelName, List<VirtualizingStackPanel> panels, gValues item, bool historyOnly = false)
        {
            if (_displaySelect == null || !_displayUpdates.ShouldUpdate(panelName, item, _auctionmethod)) return;
            try
            {
                foreach (var panel in panels)
                {
                    if (historyOnly) _displaySelect.UpdateHistoryDisplay(panel, item);
                    else _displaySelect.FindPanel(panel, item, _auctionmethod);
                }
            }
            catch
            {
                _displayUpdates.Remove(panelName);
                throw;
            }
        }

        /// <summary>
        /// 보드 구성 정보를 기반으로 패널을 생성한다.
        /// </summary>
        private void InitCreateStackPanel(BoardList boardInfo)
        {
            if (_displaySelect == null)
            {
                return;
            }
            var rowIdx = boardInfo.MultiBoards?.FirstOrDefault()?.Rows;
            if (rowIdx == null)
            {
                return;
            }

            var mainContainer = new VirtualizingStackPanel
            {
                Background = Brushes.Transparent,
                Orientation = Orientation.Vertical
            };

            foreach (var row in rowIdx)
            {
                var rowStackPanel = new VirtualizingStackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                foreach (var cell in row)
                {
                    var panel = new VirtualizingStackPanel
                    {
                        Name = "Cow_" + cell,
                        Width = _eachPanelWidth,
                        Height = _eachPanelHeight,
                        Background = Brushes.Transparent
                    };

                    if (_currentCowList.Count == 0 || _first)
                    {
                        _displaySelect.DisplayLogo(panel);
                    }
                    else
                    {
                        _first = false;
                    }
                    Panels.Add(panel);
                    if (!_panelsByName.TryGetValue(panel.Name, out var namedPanels))
                        _panelsByName.Add(panel.Name, namedPanels = new List<VirtualizingStackPanel>());
                    namedPanels.Add(panel);
                    rowStackPanel.Children.Add(panel);
                }

                mainContainer.Children.Add(rowStackPanel);
            }

            MainContainer = mainContainer;
        }


        /// <summary>
        /// 수신 구간 정보를 경매 신호 상태줄에 표시한다.
        /// </summary>
        private void OnAuctionSection(object recipient, AuctionSectionMessage message)
        {
            if (_disposed || _dispatcher.HasShutdownStarted) return;
            _dispatcher.BeginInvoke(new Action(() =>
            {
                if (_disposed) return;
                var state = message.State;
                if (!state.IsValid)
                {
                    AuctionSectionText = string.IsNullOrEmpty(state.Source) ? string.Empty : "구간 정보 미확인";
                    return;
                }
                string name = state.CowType switch { 0 => "전체", 1 => "송아지", 2 => "비육우", 3 => "번식우", _ => "미확인" };
                string range = state.IsRangeConfirmed && state.Range != null
                    ? $"경매프로그램 선택 구간 {state.Range.StartNumber}~{state.Range.EndNumber}번" : "범위 미확인";
                AuctionSectionText = $"구분 {state.CowType}({name}) · {state.SectionNumber}구간 · {range}";
            }));
        }

        private void OnAuctionSignal(object recipient, AuctionSignalMessage message)
        {
            if (_disposed || _dispatcher.HasShutdownStarted) return;
            _dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_disposed) AuctionSignalText = $"최근 수신 {message.ReceivedAt:HH:mm:ss} · {message.Text}";
            }));
        }

        private void OnDataStringMsg(object recipient, DataStringMessage message)
        {
            if (_disposed || _dispatcher.HasShutdownStarted) return;
            if (!_dispatcher.CheckAccess())
            {
                // 경매 방식은 다음 개체 갱신보다 먼저 적용되어야 한다.
                if (message.Data is "10" or "20")
                    _dispatcher.Invoke(() => OnDataStringMsg(recipient, message));
                else
                    _dispatcher.BeginInvoke(new Action(() => OnDataStringMsg(recipient, message)));
                return;
            }
            if (_disposed)
                return;
            string msg = message.Data switch
            {
                "10" => "일괄 경매 방식 ",
                "20" => "단일 경매 방식 ",
                _ => message.Data
            };

            MainWindowTextBox += msg + "\n";
            if (int.TryParse(message.Data, out int method))
            {
                _auctionmethod = method;
            }

        }

        private void UpdateStatusBar(Action update)
        {
            if (_disposed || _dispatcher.HasShutdownStarted) return;
            if (_dispatcher.CheckAccess())
                update();
            else
                _dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!_disposed) update();
                }));
        }

        private void OnNettyConnectionResultMsg(object recipient, NettyConnectionResultMessage message)
        {
            UpdateStatusBar(() => StatusBar.SetConnection(message.ResultCode));
        }

        private void OnAuctionDisplayStateMsg(object recipient, AuctionDisplayStateMessage message)
        {
            UpdateStatusBar(() => StatusBar.SetAuction(message.State));
        }

        private void OnPageIndicatorStateMsg(object recipient, PageIndicatorStateMessage message)
        {
            UpdateStatusBar(() => StatusBar.SetPage(message));
        }

        /// <summary>
        /// 빌드 버전을 로그에 출력한다.
        /// </summary>
        private string VersionShow()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            StatusBar.VersionText = $"버전: {version}";

            // 1. 실행 파일 경로 구하기 (단일 파일 게시 호환)
            string? filePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                filePath = AppContext.BaseDirectory;
            }

            // 2. System.IO.File로 명시하여 네임스페이스 오류 해결
            DateTime buildDate = System.IO.File.Exists(filePath)
                ? System.IO.File.GetLastWriteTime(filePath)
                : DateTime.Now;

            // 포맷: Version : 1.0.0.0 / Build : 2026-08-03
            string msg = $"Version : {version} / Build : {buildDate:yyyy-MM-dd}";
            MainWindowTextBox += msg + "\n";
            return msg;
        }

        /// <summary>
        /// 메시지 구독을 해제한다.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            if (_historyCoordinator != null) _historyCoordinator.ProgressChanged -= OnHistoryProgress;
            _historyCoordinator?.Dispose();
            if (_history != null)
            {
                _history.DayChanged -= OnHistoryDayChanged;
                _history.Status -= OnHistoryStatus;
            }
            _messenger.Unregister<DataChangedMessage>(this);
            _messenger.Unregister<DisplaySelectRefresh>(this);
            _displayUpdates.Clear();
            _panelsByName.Clear();
            _messengerStringMsg.Unregister<DataStringMessage>(this);
            _messengerStringMsg.Unregister<AuctionSignalMessage>(this);
            _messengerStringMsg.Unregister<AuctionSectionMessage>(this);
            _messengerStringMsg.Unregister<PageIndicatorStateMessage>(this);
            _messengerStringMsg.Unregister<NettyConnectionResultMessage>(this);
            _messengerStringMsg.Unregister<AuctionDisplayStateMessage>(this);
            _displaySelect?.Dispose();
            _displaySelect = null;
            GC.SuppressFinalize(this);
        }
    }
}
