using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views;
using CowAuctionSmall.Views.Size128_128;
//using CowAuctionSmall.Views.Size128_128.Running;
using CowAuctionSmall.Views.Size128_64;
using CowAuctionSmall.Views.Size128_64.Running;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UserInfo = CowAuctionSmall.Models.Structures.UserInfo;
using Stretch = System.Windows.Media.Stretch;
using CowAuctionSmall.Views.Size128_128.Running.CustomAuctionRunning1;

namespace CowAuctionSmall.Models
{
    public class DisplaySelect
    {
        // 진행 중인 뷰모델을 관리하는 ObservableCollection
        public ObservableCollection<AuctionContPanelViewModel> RunningViewModel { get; private set; }

        private UserInfo _userInfo;
        private BoardList _boardinfo;
        private string _nhCode = string.Empty; // 축협 사업장 코드

        // 페이지 시간 목록
        private List<int> pageTime = new List<int>();
        private int _totalRunningPage = 0;

        // 화면 전환을 위한 타이머
        private DispatcherTimer _timer;
        private int _rotationIndex = 0;

        private Timer? _initTimer; // 매 정각마다 초기화 타이머

        // 단일 경매 진행 시 플래그
        private bool singleAuctionmethodFlag = true;

        private readonly WeakReferenceMessenger _msgRefreshString;


        private NLogger logger; // 로그용

        public DisplaySelect(UserInfo userInfo, BoardList boardinfo)
        {
            logger = NLogger.Instance;

            _msgRefreshString = WeakReferenceMessenger.Default;
            _msgRefreshString.Register<DisplaySelectRefresh>(this, OnRefreshMsg);

            _userInfo = userInfo;
            _boardinfo = boardinfo;

            _nhCode = _userInfo.Auction.AuctionHouseCode;

            // 경매 페이지 수 설정
            if (_userInfo.Auction.BoardPage.Length <= 0)
            {
                _totalRunningPage = 1;
            }
            else
            {
                _totalRunningPage = Convert.ToInt32(_userInfo.Auction.BoardPage);
            }

           int page1 = Convert.ToInt32(_userInfo.Auction.BoardPageTime);
           int page2 =  Convert.ToInt32(_userInfo.Auction.BoardPageTime2);
           int page3 = Convert.ToInt32(_userInfo.Auction.BoardPageTime3);

            // 페이지 시간 설정
            pageTime.Add(page1 == 0 ? 25 : page1);
            pageTime.Add(page2 == 0 ? 7 :  page2);
            pageTime.Add(page3 == 0 ? 0 : page3);

            RunningViewModel = new ObservableCollection<AuctionContPanelViewModel>();

            // 타이머 초기화 및 시작
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Start();

            _initTimer = new Timer(InitTimer_Tick, null, 0, 1000);
        }


        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // SemaphoreSlim 초기화
        /// <summary>
        /// 페이지 싱크를 위한 타이머 PC1,2가 최대한 동일한 페이지를 보여주기 위함
        /// </summary>
        /// <param name="state"></param>
        private async void InitTimer_Tick(object? state)
        {
            if (_totalRunningPage == 1 || singleAuctionmethodFlag == false) //총 보여줄 페이지가 1개이거나 단일경매 진행했다면
            {
                StopInitTimer();
                return;
            }

            if (DateTime.Now.Minute % 25 == 0 && DateTime.Now.Second <= 3) // 매 25분,50분 0초부터 3초까지 초기화
            {
                await _semaphore.WaitAsync(); // 세마포어 락
                try
                {
                    // UI 업데이트는 Dispatcher를 사용하여 UI 스레드에서 수행해야 합니다.
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // _timer 리셋
                        _timer.Stop();
                        _rotationIndex = 0; // 페이지 인덱스 초기화

                        foreach (AuctionContPanelViewModel viewModel in RunningViewModel)
                        {
                            DisplayRunningPageNum(viewModel._panel, viewModel.CowInfo, _rotationIndex + 1);
                        }

                        _timer.Start(); // 타이머 재시작
                        logger.LogInfo("매 25분,50분 마다 초기화 완료");
                        Debug.WriteLine("매 25분,50분 마다 초기화 완료");
                    });
                }
                finally
                {
                    _semaphore.Release(); // 세마포어 해제
                }
            }
        }

        // 로고를 여러 개 보여줘야 할 경우 한 줄마다 다르게 보여주기 위한 메서드
        private string IsMultipleLogo(string panelName)
        {
            string logoImgName = string.Empty;

            panelName = panelName.Split('_')[1];

            List<LogoRowIdx> logoRows = _boardinfo?.LogoBoard?[0]?.Rows ?? new List<LogoRowIdx>();

            foreach (var logoRow in logoRows)
            {
                if (logoRow.Rows != null && logoRow.Rows.Contains(Convert.ToInt32(panelName)))
                {
                    logoImgName = logoRow.ID ?? "logo.bmp";
                }
            }

            if (logoImgName == string.Empty)
            {
                logoImgName = "logo.bmp";
            }

            if (logoImgName != null)
            {
                // 폴더 경로 설정
                string folderPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Config", logoImgName);
                return folderPath;
            }
            else
            {
                return string.Empty;
            }
        }

        // 로고를 패널에 표시하는 메서드
        public void DisplayLogo(VirtualizingStackPanel panel)
        {
            string logoPath = IsMultipleLogo(panel.Name);

            panel.Children.Clear();
            RemoveViewModelFromPanel(panel);

            if (File.Exists(logoPath))
            {
                var image = new Image();

                // 기존 이미지가 있을 경우 해제
                if (image.Source != null)
                {
                    image.Source = null; // 이전 이미지 리소스 해제
                }

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(logoPath, UriKind.Absolute);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 캐시 옵션을 설정하여 파일을 메모리에 로드
                bitmapImage.EndInit();

                image.Source = bitmapImage;
                image.Stretch = Stretch.Fill;
                panel.Children.Add(image);
            }
        }


        // 패널에서 뷰모델을 제거하는 메서드
        private void RemoveViewModelFromPanel(VirtualizingStackPanel panel)
        {
            var viewModel = RunningViewModel.FirstOrDefault(vm => vm._panel == panel);
            if (viewModel != null)
            {
                RunningViewModel.Remove(viewModel);
                viewModel.Dispose();
                panel.DataContext = null;
            }
        }

        /// <summary>
        /// 진행할 화면 넣기 추후 여러 페이지 보여주기
        /// </summary>
        public void DisplayRunning(VirtualizingStackPanel panel, gValues cowInfo, int auctionmethod)
        {
            var viewModel = new AuctionContPanelViewModel(cowInfo, this, panel, pageTime, _totalRunningPage);
            RunningViewModel.Add(viewModel);
            InitializePages(panel, viewModel);

            if (panel.Children.Count != 0)
            {
                DisplayRunningPageNum(viewModel._panel, cowInfo, 1);
            }

            // 염소 경매인 경우
            if (cowInfo.CowDistinction.Equals("5"))
            {
                DisplayRunningPageNum(viewModel._panel, cowInfo, 1);
                _timer.Stop();
                StopInitTimer();
            }

            switch (auctionmethod)
            {
                case 10: // 일괄 경매
                    break;
                case 20: // 단일 경매
                    if (cowInfo.IsRunning == true && singleAuctionmethodFlag)
                    {
                        Debug.WriteLine("단일경매 시작");

                        singleAuctionmethodFlag = false;

                        foreach (AuctionContPanelViewModel vm in RunningViewModel)
                        {
                            DisplayRunningPageNum(vm._panel, vm.CowInfo, 1);
                        }
                        _timer.Stop();
                    }
                    break;
                default:
                    break;
            }
        }

        // 페이지를 초기화하는 메서드
        public void InitializePages(VirtualizingStackPanel panel, AuctionContPanelViewModel viewModel)
        {
            var existingPages = panel.Children.OfType<UserControl>().ToList();
            UserControl page1, page2;   //, page3;

            if (_boardinfo.Size.Equals("128,128") || _boardinfo.Size.Equals("128*128"))
            {
                //page1 = existingPages.FirstOrDefault(p => p.Name == "RunPage1") ?? new AuctionRunning1 { Name = "RunPage1" };

                page1 = existingPages.FirstOrDefault(p => p.Name == "RunPage1") ?? CustomAuctionRunning1_128();
                page1.Name = "RunPage1"; // 함수 반환 후에도 이름 설정 필요

                page2 = existingPages.FirstOrDefault(p => p.Name == "RunPage2") ?? new AuctionRunning2 { Name = "RunPage2" };
                //page3 = existingPages.FirstOrDefault(p => p.Name == "RunPage3") ?? new AuctionRunning3 { Name = "RunPage3" };
            }
            else
            {
                page1 = existingPages.FirstOrDefault(p => p.Name == "RunPage1_64") ?? new AuctionRunning1_64 { Name = "RunPage1_64" };
                page2 = existingPages.FirstOrDefault(p => p.Name == "RunPage2_64") ?? new AuctionRunning2_64 { Name = "RunPage2_64" };
                //page3 = existingPages.FirstOrDefault(p => p.Name == "RunPage3_64") ?? new AuctionRunning3_64 { Name = "RunPage3_64" };
            }

            SetPageProperties(page1, viewModel, panel);
            SetPageProperties(page2, viewModel, panel);
            //SetPageProperties(page3, viewModel, panel);

            if (!existingPages.Contains(page1)) panel.Children.Add(page1);
            if (!existingPages.Contains(page2)) panel.Children.Add(page2);
            //if (!existingPages.Contains(page2)) panel.Children.Add(page3);
        }



        // 페이지 속성을 설정하는 메서드
        private void SetPageProperties(UserControl page, AuctionContPanelViewModel viewModel, VirtualizingStackPanel panel)
        {
            page.DataContext = viewModel;
            page.Visibility = Visibility.Collapsed;
            page.Width = panel.Width;
            page.Height = panel.Height;
        }

        // 경매 진행 페이지 번호를 표시하는 메서드
        public void DisplayRunningPageNum(VirtualizingStackPanel panel, gValues cowinfo, int pageNum)
        {
            foreach (UIElement child in panel.Children)
            {
                child.Visibility = Visibility.Collapsed;
            }

            if (cowinfo.IsRunning)
            {
                var existingViewModel = panel.DataContext as AuctionContPanelViewModel;

                if (existingViewModel == null || !existingViewModel.CowInfo.Equals(cowinfo))
                {
                    // 기존 ViewModel이 있을 경우 메모리에서 해제
                    panel.DataContext = null;

                    existingViewModel = new AuctionContPanelViewModel(cowinfo, this, panel, pageTime, _totalRunningPage);
                    panel.DataContext = existingViewModel;
                    InitializePages(panel, existingViewModel);
                }
            }

            if (pageNum >= 1 && pageNum <= 3)
            {
                int index = 0;
                if (_boardinfo.Size.Equals("128,64") || _boardinfo.Size.Equals("128*64"))
                {
                    index = (pageNum - 1) + (_boardinfo.Size.Equals("128,64") || _boardinfo.Size.Equals("128*64") ? 0 : 3);
                }
                else
                {
                    index = (pageNum - 1) + (_boardinfo.Size.Equals("128,128") || _boardinfo.Size.Equals("128*128") ? 0 : 3);
                }

                if (index < panel.Children.Count)
                {
                    panel.Children[index].Visibility = Visibility.Visible;
                }
            }
        }


        /// <summary>
        /// 낙찰된 화면 넣기
        /// </summary>
        public void DisplaySold(VirtualizingStackPanel panel, gValues cowInfo)
        {
            UserControl cowPanel = null;
            if (_boardinfo.Size.Equals("128,128") || _boardinfo.Size.Equals("128*128"))
            {
                cowPanel = new AuctionSold();
            }
            else
            {
                cowPanel = new AuctionSold_64();
            }
            cowPanel.DataContext = cowInfo;
            cowPanel.Width = panel.Width;
            cowPanel.Height = panel.Height;

            panel.Children.Add(cowPanel);
        }

        /// <summary>
        /// 유찰된 화면 넣기
        /// </summary>
        public void DisplayUnSold(VirtualizingStackPanel panel, gValues cowInfo)
        {
            UserControl cowPanel = null;
            if (_boardinfo.Size.Equals("128,128") || _boardinfo.Size.Equals("128*128"))
            {
                cowPanel = new AuctionUnSold();
            }
            else
            {
                cowPanel = new AuctionUnSold_64();
            }
            cowPanel.DataContext = cowInfo;
            cowPanel.Width = panel.Width;
            cowPanel.Height = panel.Height;

            panel.Children.Add(cowPanel);
        }

        // 패널의 바인딩을 해제하는 메서드
        private void ClearPanelBindings(VirtualizingStackPanel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is UserControl control)
                {
                    control.DataContext = null;
                }
            }
            panel.DataContext = null;
            panel.Children.Clear();
        }

        // 패널을 이름으로 찾는 메서드
        public void FindPanel(string pName, ObservableCollection<VirtualizingStackPanel> panels, gValues gv, int auctionmethod)
        {

            VirtualizingStackPanel panel1 = FindPanel(p => p.Name == pName, panels);

            int auctionStatus = int.Parse(gv.AuctionResultStatus);
            if (panel1 != null)
            {
                RemoveViewModelFromPanel(panel1);
                ClearPanelBindings(panel1);

                switch (auctionStatus)
                {
                    case 11:
                        DisplayRunning(panel1, gv, auctionmethod);
                        break;
                    case 22:
                        DisplaySold(panel1, gv);
                        break;
                    case 23:
                        DisplayUnSold(panel1, gv);
                        break;
                    default:
                        DisplayLogo(panel1);
                        break;
                }
            }
            else
            {
                if (gv.IsRunning == true && singleAuctionmethodFlag == true)
                {
                    Debug.WriteLine("단일경매 시작");
                    singleAuctionmethodFlag = false;

                    foreach (AuctionContPanelViewModel vm in RunningViewModel)
                    {
                        DisplayRunningPageNum(vm._panel, vm.CowInfo, 1);
                    }
                    _timer.Stop();
                }
            }
        }

        // 패널을 조건에 맞게 찾는 메서드
        private VirtualizingStackPanel? FindPanel(Func<VirtualizingStackPanel, bool> predicate, ObservableCollection<VirtualizingStackPanel> panels)
        {
            foreach (var panel in panels)
            {
                if (predicate(panel))
                {
                    return panel;
                }
            }
            return null;
        }

        //ServerGetData에서 Refresh라는 메시지를 받았을 때 실행되는 메서드
        private void OnRefreshMsg(object recipient, DisplaySelectRefresh message)
        {
            if (RunningViewModel.Any(cow => cow.CowInfo.CowDistinction != "5"))
            {
                var AnimalType = RunningViewModel.First(cow => cow.CowInfo.CowDistinction != "5");
                if (message.Data.Equals("Refresh") && AnimalType != null)
                {
                    Debug.WriteLine("Refresh \t\t\t\t\tRefresh \t\t\t\t\tRefresh \t\t\t\t\tRefresh \t\t\t\t\tRefresh \t\t\t\t\t");
                    _timer.Start();
                    StartInitTimer();
                    singleAuctionmethodFlag = true;
                }
            }
            else // 염소 경매인 경우, 진행 페이지가 1개라 따로 타이머 돌 필요 없음 하지만 혹시 모르니 타이머는 정지  
            {
                _timer.Stop();
                StopInitTimer();
            }
        }

        // 타이머 틱 이벤트 핸들러
        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (AuctionContPanelViewModel viewModel in RunningViewModel)
            {
                DisplayRunningPageNum(viewModel._panel, viewModel.CowInfo, _rotationIndex + 1);
            }

            _timer.Interval = TimeSpan.FromSeconds(pageTime[_rotationIndex]);

            _rotationIndex = (_rotationIndex + 1) % _totalRunningPage;
        }


        // _initTimer 중지
        public void StopInitTimer()
        {
            if (_initTimer != null)
            {
                _initTimer.Dispose();
                _initTimer = null;
            }
        }

        // _initTimer 시작
        public void StartInitTimer()
        {
            if (_initTimer == null)
            {
                _initTimer = new Timer(InitTimer_Tick, null, 0, 1000);
            }
        }

        private UserControl CustomAuctionRunning1_128()
        {
            switch (_nhCode)
            {

                case "8808990656953": // 정읍 8808990656953 중량란 대신에 유전능력 알파벳으로 표시
                    return new Jeongeup();
                default:
                    return new AuctionRunning1();
            }
        }
    
    }
}
