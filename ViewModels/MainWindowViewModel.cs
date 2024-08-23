using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.Models.XMLParser;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using static CowAuctionSmall.Models.NLogger;

namespace CowAuctionSmall.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly WeakReferenceMessenger _messenger;
        private readonly WeakReferenceMessenger _messengerStringMsg;

        private XmlParserCont _xmlParserCont;


        public ObservableCollection<VirtualizingStackPanel> Panels { get; private set; } // 전체적인 패널 관리
        

        private readonly ObservableCollection<gValues> _currentCowList = new ObservableCollection<gValues>();
        private readonly Dispatcher _dispatcher;
        public VirtualizingStackPanel MainContainer { get; private set; }

        [ObservableProperty]
        private int _mainWindowWidth;
        [ObservableProperty]
        private int _mainWindowHeight;

        [ObservableProperty]
        private string _mainWindowTextBox;

        [ObservableProperty]
        private double _mainPositionX;
        [ObservableProperty]
        private double _mainPositionY;

        //각각의 패널 크기
        private int _eachPanelWidth;
        private int _eachPanelHeight;


        //private int test = 1;
        private bool first = true;

        //패널 인덱스(생성될 패널의 이름용)
        //private int k = 1;

        private DisplaySelect displaySelect;
        private ServerGetData _serverGetData;

        private int _auctionmethod = 0; // 단일 일괄경매방식


        public MainWindowViewModel(XmlParserCont xmlParserCont, ServerGetData serverGetData)
        {

            // NLogger 초기화
            var logger = NLogger.Instance;

            // 초기화 후 로그 기록
            //logger.LogInfo("Application started.22");

            _dispatcher = Dispatcher.CurrentDispatcher;

            _messenger = WeakReferenceMessenger.Default; 
            _messenger.Register<DataChangedMessage>(this, OnDataChanged);

            _messengerStringMsg = WeakReferenceMessenger.Default;
            _messengerStringMsg.Register<DataStringMessage>(this, OnDataStringMsg);

            this._serverGetData = serverGetData;

            //관찰 나중에 값을 변경하려고 패널을 관찰
            Panels = new ObservableCollection<VirtualizingStackPanel>();

            //xml (user, board) 파싱한 값
            _xmlParserCont = xmlParserCont;
            var r = _xmlParserCont.XmlPaserResult();

            //각각의 패널의 사이즈
            _eachPanelWidth = int.Parse(r.board.Size.Split(',')[0]);
            _eachPanelHeight = int.Parse(r.board.Size.Split(',')[1]);

            //전체 보여주는 화면의 사이즈
            _mainWindowWidth = r.board.MultiBoards[0].Rows[0].Length * _eachPanelWidth;
            _mainWindowHeight = r.board.MultiBoards[0].Rows.Count * _eachPanelHeight +75;

            _mainPositionX = double.Parse(r.userInfo.Auction.StartPosition.Split(",")[0]);
            _mainPositionY = double.Parse(r.userInfo.Auction.StartPosition.Split(",")[1]);

            displaySelect = new DisplaySelect(r.userInfo, r.board);

            initCreateStackPanel(r.board);

        }



        /// <summary>
        /// 고정적으로 메시지를 받아와서 넣어주고
        /// </summary>
        private void OnDataChanged(object recipient, DataChangedMessage message)
        {
            ObservableCollection<gValues> currentCowList = new ObservableCollection<gValues>();
            currentCowList.Clear();

            // 데이터 처리
            foreach (var gValue in message.Data)
            {
                currentCowList.Add(gValue);
            }
            // UI 스레드에서 UpdatePanels 메서드 호출
            _dispatcher.Invoke(() => UpdatePanels(currentCowList));
        }

        public void UpdatePanels(ObservableCollection<gValues> currentCowList)
        {
            Debug.WriteLine("------------ {0}", currentCowList.Count);
            // Panels의 내용을 업데이트
            foreach (gValues gValues in currentCowList)
            {
                displaySelect.FindPanel("Cow_"+ gValues.SpaceIndex, Panels, gValues, _auctionmethod);
            }
        }


        private void initCreateStackPanel(BoardList boardInfo)
        {

            // RowIdx에 접근
            List<int[]> rowIdx = boardInfo.MultiBoards[0].Rows;
            if (rowIdx != null)
            {
                // 최상위 컨테이너
                var mainContainer = new VirtualizingStackPanel();
                mainContainer.Background = Brushes.DarkSlateBlue;
                mainContainer.Orientation = Orientation.Vertical;

                int rowIdxNumber = 0;

                int k = 0;// 판넬 인덱스용?
                for (int i=0; i< rowIdx.Count; i++)
                {

                    // 각 행의 VirtualizingStackPanel
                    var rowStackPanel = new VirtualizingStackPanel();
                    rowStackPanel.Orientation = Orientation.Horizontal;


                    for (int j = 0; j < rowIdx[i].Length ; j++)
                    {
                        k++;
                        // 패널 생성 및 레이아웃 설정
                        var panel = new VirtualizingStackPanel(); // CowInfoPanel 사용
                        panel.Name = "Cow_"+ rowIdx[i][j].ToString(); // 나중에 해당 패널만 조작
                        panel.Width = _eachPanelWidth;
                        panel.Height = _eachPanelHeight; // 패널의 세로 크기
                        panel.Background = Brushes.Black;


                        if (_currentCowList.Count <0 || first) //데이터가 없을경우 즉, 오늘 경매날이 아닌경우
                        {
                            displaySelect.DisplayLogo(panel);
                        }
                        else // 아직... 생각중..
                        {
                            first = false;
                        }
                        Panels.Add(panel);
                        // 생성된 패널을 현재 행의 VirtualizingStackPanel에 추가
                        rowStackPanel.Children.Add(panel);
                        
                    }

                    rowIdxNumber++;
                    
                    // 현재 행의 VirtualizingStackPanel을 최상위 컨테이너에 추가
                    mainContainer.Children.Add(rowStackPanel);
                    //_subWindowWidth = widthPanelSize.Length;


                    k++; //test
                }

                // 최종적으로 생성된 mainContainer를 MainContainer 속성에 설정
                MainContainer = mainContainer;
            }
        }

        private void OnDataStringMsg(object recipient, DataStringMessage message)
        {
            string msg = String.Empty;
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
    }

}
