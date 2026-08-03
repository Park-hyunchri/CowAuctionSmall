using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.Models.XMLParser;
using CowAuctionSmall.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        private string _pageIndicatorRoleText = "모드: -";

        [ObservableProperty]
        private string _pageIndicatorSyncText = "연결상태: -";

        [ObservableProperty]
        private Visibility _pageIndicatorVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private double _mainPositionX;
        [ObservableProperty]
        private double _mainPositionY;

        // 각각의 패널 크기
        private int _eachPanelWidth;
        private int _eachPanelHeight;

        private bool _first = true;

        private DisplaySelect? _displaySelect;
        private ServerGetData _serverGetData;

        private int _auctionmethod = 0; // 단일 일괄경매방식
        public ObservableCollection<Brush> PageIndicatorDots { get; } = new ObservableCollection<Brush>();
        private static readonly Brush DotInactiveBrush = Brushes.DimGray;

        /// <summary>
        /// 메인 화면의 패널과 메시지 구독을 초기화한다.
        /// </summary>
        public MainWindowViewModel(XmlParserCont xmlParserCont, ServerGetData serverGetData)
        {
            var logger = NLogger.Instance;

            VersionShow();

            _dispatcher = Dispatcher.CurrentDispatcher;

            _messenger = WeakReferenceMessenger.Default;
            _messenger.Register<DataChangedMessage>(this, OnDataChanged);

            _messengerStringMsg = WeakReferenceMessenger.Default;
            _messengerStringMsg.Register<DataStringMessage>(this, OnDataStringMsg);
            _messengerStringMsg.Register<PageIndicatorStateMessage>(this, OnPageIndicatorStateMsg);

            this._serverGetData = serverGetData;

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

            _displaySelect = new DisplaySelect(r.userInfo, r.board);
            InitCreateStackPanel(r.board);

            MainWindowTextBox += "뿌리농가 적용 버전" + "\n";
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
                    _mainPositionX = double.Parse(startPositionParts[0]);
                    _mainPositionY = double.Parse(startPositionParts[1]);
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

            if (board.MultiBoards?.FirstOrDefault()?.Rows != null)
            {
                _mainWindowWidth = board.MultiBoards[0].Rows[0].Length * _eachPanelWidth;
                _mainWindowHeight = board.MultiBoards[0].Rows.Count * _eachPanelHeight + 98;
            }
        }

        /// <summary>
        /// 데이터 변경 메시지를 받아 화면 갱신을 요청한다.
        /// </summary>
        private void OnDataChanged(object recipient, DataChangedMessage message)
        {

            if (message.Data.Count > 0)
            {
                _dispatcher.Invoke(() =>
                {
                    _currentCowList.Clear();
                    foreach (var gValue in message.Data)
                    {
                        _currentCowList.Add(gValue);
                    }
                    UpdatePanels(_currentCowList);
                });
            }
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
            Debug.WriteLine("------------ {0}", currentCowList.Count);

            // 먼저 모든 패널을 딕셔너리로 변환 (빠른 검색을 위해)
            var panelDict = Panels.ToDictionary(panel => panel.Name);

            // UI 업데이트를 한 번에 실행하기 위해 리스트에 저장
            List<Action> updates = new List<Action>();

            var groupedBySpaceIndex = currentCowList
                .Where(item => item != null)
                .GroupBy(item => item.SpaceIndex);

            foreach (var group in groupedBySpaceIndex)
            {
                var winner = CowAuctionSmall.Utils.CowDisplaySelector.SelectForSpaceIndex(group);
                if (winner == null)
                {
                    continue;
                }

                var panelName = "Cow_" + winner.SpaceIndex;
                if (!panelDict.TryGetValue(panelName, out var panel) || panel == null)
                {
                    //Debug.WriteLine($"패널을 찾지 못해 업데이트를 건너뜁니다. name={panelName}, sip={winner.SipNumber}, running={winner.IsRunning}");
                    continue;
                }
                updates.Add(() => _displaySelect.FindPanel(panel, winner, _auctionmethod));
            }

            // UI 스레드에서 한 번만 실행 (UI 렌더링 최적화)
            _dispatcher.Invoke(() =>
            {
                foreach (var update in updates)
                {
                    update();
                }
            });
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
            if (boardInfo.MultiBoards?.FirstOrDefault()?.Rows == null)
            {
                return;
            }

            var rowIdx = boardInfo.MultiBoards[0].Rows;
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
                    rowStackPanel.Children.Add(panel);
                }

                mainContainer.Children.Add(rowStackPanel);
            }

            MainContainer = mainContainer;
        }


        /// <summary>
        /// 상태 메시지를 로그 영역에 표시한다.
        /// </summary>
        private void OnDataStringMsg(object recipient, DataStringMessage message)
        {
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

        private void OnPageIndicatorStateMsg(object recipient, PageIndicatorStateMessage message)
        {
            _dispatcher.Invoke(() =>
            {
                var totalPages = Math.Clamp(message.TotalPages, 1, 4);
                var currentPage = Math.Clamp(message.CurrentPage, 1, totalPages);
                var syncText = message.IsFrozen
                    ? "연결상태: 1페이지 고정"
                    : (message.IsSubFallbackActive ? "연결상태: 로컬 복구" : "연결상태: 연결 중");
                var activeBrush = message.IsFrozen
                    ? Brushes.Gold
                    : (message.IsSubFallbackActive ? Brushes.DeepSkyBlue : Brushes.LimeGreen);

                PageIndicatorRoleText = message.IsMaster ? "모드: 마스터 모드" : "모드: 서브 모드";
                PageIndicatorSyncText = syncText;
                PageIndicatorVisibility = totalPages > 1 ? Visibility.Visible : Visibility.Collapsed;

                PageIndicatorDots.Clear();
                for (int i = 1; i <= totalPages; i++)
                {
                    PageIndicatorDots.Add(i == currentPage ? activeBrush : DotInactiveBrush);
                }
            });
        }

        /// <summary>
        /// 빌드 버전을 로그에 출력한다.
        /// </summary>
        private string VersionShow()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;

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
            _messenger.Unregister<DataChangedMessage>(this);
            _messengerStringMsg.Unregister<DataStringMessage>(this);
            _messengerStringMsg.Unregister<PageIndicatorStateMessage>(this);
            _displaySelect?.Dispose();
            _displaySelect = null;
            GC.SuppressFinalize(this);
        }
    }
}
