using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.Models.XMLParser;
using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
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

        private bool _first = true;

        private DisplaySelect _displaySelect;
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

            // null 가능성 검사 추가
            var r = _xmlParserCont.XmlPaserResult();
            if (r.board == null || r.userInfo == null)
            {
                logger.LogError("MainWindowViewModel: board 또는 userInfo가 null입니다.");
                return;
            }

            InitializeBoardSize(r.board);
            InitializeUserInfo(r.userInfo);

            _displaySelect = new DisplaySelect(r.userInfo, r.board);
            InitCreateStackPanel(r.board);
        }
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
                _mainWindowHeight = board.MultiBoards[0].Rows.Count * _eachPanelHeight + 75;
            }
        }

        private void OnDataChanged(object recipient, DataChangedMessage message)
        {

            if (message.Data.Count >0)
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



        public void UpdatePanels(ObservableCollection<gValues> currentCowList)
        {
            Debug.WriteLine("------------ {0}", currentCowList.Count);

            _dispatcher.Invoke(() =>
            {
                foreach (gValues gValues in currentCowList)
                {
                    if (gValues == null)
                    {
                        return;
                    }
                    // 각 소의 정보를 기반으로 패널을 업데이트
                    _displaySelect.FindPanel("Cow_" + gValues.SpaceIndex, Panels, gValues, _auctionmethod);
                }
            });
        }


        private void InitCreateStackPanel(BoardList boardInfo)
        {
            if (boardInfo.MultiBoards?.FirstOrDefault()?.Rows == null)
            {
                return;
            }

            var rowIdx = boardInfo.MultiBoards[0].Rows;
            var mainContainer = new VirtualizingStackPanel
            {
                Background = Brushes.DarkSlateBlue,
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
                        Background = Brushes.Black
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


        private void OnDataStringMsg(object recipient, DataStringMessage message)
        {
            string msg = message.Data switch
            {
                "10" => "일괄 경매 방식",
                "20" => "단일 경매 방식",
                _ => message.Data
            };

            MainWindowTextBox +=  msg+"\n";
            if (int.TryParse(message.Data, out int method))
            {
                _auctionmethod = method;
            }

        }


        public void Dispose()
        {
            _messenger.Unregister<DataChangedMessage>(this);
            _messengerStringMsg.Unregister<DataStringMessage>(this);
        }
    }
}
