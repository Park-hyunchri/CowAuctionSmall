using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.Models.XMLParser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Controls;
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
        private double _mainPositionX;
        [ObservableProperty]
        private double _mainPositionY;

        // 각각의 패널 크기
        private int _eachPanelWidth;
        private int _eachPanelHeight;

        private bool first = true;

        private DisplaySelect displaySelect;
        private ServerGetData _serverGetData;

        private int _auctionmethod = 0; // 단일 일괄경매방식

        public MainWindowViewModel(XmlParserCont xmlParserCont, ServerGetData serverGetData)
        {
            var logger = NLogger.Instance;

            _dispatcher = Dispatcher.CurrentDispatcher;

            _messenger = WeakReferenceMessenger.Default;
            _messenger.Register<DataChangedMessage>(this, OnDataChanged);

            _messengerStringMsg = WeakReferenceMessenger.Default;
            _messengerStringMsg.Register<DataStringMessage>(this, OnDataStringMsg);

            this._serverGetData = serverGetData;

            Panels = new ObservableCollection<VirtualizingStackPanel>();

            _xmlParserCont = xmlParserCont;
            var r = _xmlParserCont.XmlPaserResult();

            // null 가능성 검사 추가
            if (r.board == null || r.userInfo == null)
            {
                logger.LogError("MainWindowViewModel: board 또는 userInfo가 null입니다.");
                return;
            }

            if (r.board.Size != null)
            {
                var sizeParts = r.board.Size.Split(',');
                if (sizeParts.Length == 2)
                {
                    _eachPanelWidth = int.Parse(sizeParts[0]);
                    _eachPanelHeight = int.Parse(sizeParts[1]);
                }
            }

            // null 가능성 검사 추가
            if (r.board.MultiBoards != null && r.board.MultiBoards.Count > 0 && r.board.MultiBoards[0].Rows != null)
            {
                _mainWindowWidth = r.board.MultiBoards[0].Rows[0].Length * _eachPanelWidth;
                _mainWindowHeight = r.board.MultiBoards[0].Rows.Count * _eachPanelHeight + 75;
            }

            if (r.userInfo.Auction != null && r.userInfo.Auction.StartPosition != null)
            {
                var startPositionParts = r.userInfo.Auction.StartPosition.Split(",");
                if (startPositionParts.Length == 2)
                {
                    _mainPositionX = double.Parse(startPositionParts[0]);
                    _mainPositionY = double.Parse(startPositionParts[1]);
                }
            }

            displaySelect = new DisplaySelect(r.userInfo, r.board);

            initCreateStackPanel(r.board);
        }

        private void OnDataChanged(object recipient, DataChangedMessage message)
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



        public void UpdatePanels(ObservableCollection<gValues> currentCowList)
        {
            Debug.WriteLine("------------ {0}", currentCowList.Count);

            _dispatcher.Invoke(() =>
            {
                foreach (gValues gValues in currentCowList)
                {
                    // 각 소의 정보를 기반으로 패널을 업데이트
                    displaySelect.FindPanel("Cow_" + gValues.SpaceIndex, Panels, gValues, _auctionmethod);
                }
            });
        }


        private void initCreateStackPanel(BoardList boardInfo)
        {
            // null 가능성 검사 추가
            if (boardInfo.MultiBoards == null || boardInfo.MultiBoards.Count == 0 || boardInfo.MultiBoards[0].Rows == null)
            {
                return;
            }

            List<int[]> rowIdx = boardInfo.MultiBoards[0].Rows;
            var mainContainer = new VirtualizingStackPanel
            {
                Background = Brushes.DarkSlateBlue,
                Orientation = Orientation.Vertical
            };

            int k = 0;
            foreach (var row in rowIdx)
            {
                var rowStackPanel = new VirtualizingStackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                foreach (var cell in row)
                {
                    k++;
                    var panel = new VirtualizingStackPanel
                    {
                        Name = "Cow_" + cell.ToString(),
                        Width = _eachPanelWidth,
                        Height = _eachPanelHeight,
                        Background = Brushes.Black
                    };

                    if (_currentCowList.Count < 0 || first)
                    {
                        displaySelect.DisplayLogo(panel);
                    }
                    else
                    {
                        first = false;
                    }
                    Panels.Add(panel);
                    rowStackPanel.Children.Add(panel);
                }

                mainContainer.Children.Add(rowStackPanel);
                k++;
            }

            MainContainer = mainContainer;
        }

        private void OnDataStringMsg(object recipient, DataStringMessage message)
        {
            string msg = string.Empty;
            switch (message.Data)
            {
                case "10":
                    msg = "일괄 경매 방식";
                    MainWindowTextBox += msg;
                    _auctionmethod = Convert.ToInt32(message.Data);
                    break;

                case "20":
                    msg = "단일 경매 방식";
                    MainWindowTextBox += msg;
                    _auctionmethod = Convert.ToInt32(message.Data);
                    break;

                default:
                    MainWindowTextBox += message.Data;
                    break;
            }
        }

        public void Dispose()
        {
            _messenger.Unregister<DataChangedMessage>(this);
            _messengerStringMsg.Unregister<DataStringMessage>(this);
        }
    }
}
