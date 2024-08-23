using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views;
using CowAuctionSmall.Views.Size128_128;
//using CowAuctionSmall.Views.Size128_128.Running;
using CowAuctionSmall.Views.Size128_64;
using CowAuctionSmall.Views.Size128_64.Running;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UserInfo = CowAuctionSmall.Models.Structures.UserInfo;

namespace CowAuctionSmall.Models
{
    public class DisplaySelect
    {
        // 진행 중인 뷰모델을 관리하는 ObservableCollection
        public ObservableCollection<AuctionContPanelViewModel> RunningViewModel { get; private set; }

        private UserInfo _userInfo;
        private BoardList _boardinfo;

        // 페이지 시간 목록
        private List<int> pageTime = new List<int>();
        private int _totalRunningPage = 0;

        // 화면 전환을 위한 타이머
        private DispatcherTimer _timer;
        private int _rotationIndex = 0;

        // 단일 경매 진행 시 플래그
        private bool singleAuctionmethodFlag = true;

        public DisplaySelect(UserInfo userInfo, BoardList boardinfo)
        {
            _userInfo = userInfo;
            _boardinfo = boardinfo;

            // 경매 페이지 수 설정
            if (_userInfo.Auction.BoardPage.Length <= 0)
            {
                _totalRunningPage = 1;
            }
            else
            {
                _totalRunningPage = Convert.ToInt32(_userInfo.Auction.BoardPage);
            }

            // 페이지 시간 설정
            pageTime.Add(Convert.ToInt32(_userInfo.Auction.BoardPageTime));
            pageTime.Add(Convert.ToInt32(_userInfo.Auction.BoardPageTime2));
            pageTime.Add(Convert.ToInt32(_userInfo.Auction.BoardPageTime3));

            RunningViewModel = new ObservableCollection<AuctionContPanelViewModel>();

            // 타이머 초기화 및 시작
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Start();
        }

        // 로고를 여러 개 보여줘야 할 경우 한 줄마다 다르게 보여주기 위한 메서드
        private string IsMultipleLogo(string panelName)
        {
            string logoImgName = string.Empty;

            panelName = panelName.Split('_')[1];

            List<LogoRowIdx> logoRows = _boardinfo.LogoBoard[0].Rows;

            foreach (var logoRow in logoRows)
            {
                if (logoRow.Rows != null && logoRow.Rows.Contains(Convert.ToInt32(panelName)))
                {
                    logoImgName = logoRow.ID;
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
                image.Source = new BitmapImage(new Uri(logoPath, UriKind.Absolute));
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
            UserControl page1, page2;

            if (_boardinfo.Size.Equals("128,128") || _boardinfo.Size.Equals("128*128"))
            {
                page1 = existingPages.FirstOrDefault(p => p.Name == "RunPage1") ?? new AuctionRunning1 { Name = "RunPage1" };
                page2 = existingPages.FirstOrDefault(p => p.Name == "RunPage2") ?? new AuctionRunning2 { Name = "RunPage2" };
            }
            else
            {
                page1 = existingPages.FirstOrDefault(p => p.Name == "RunPage1_64") ?? new AuctionRunning1_64 { Name = "RunPage1_64" };
                page2 = existingPages.FirstOrDefault(p => p.Name == "RunPage2_64") ?? new AuctionRunning2_64 { Name = "RunPage2_64" };
            }

            SetPageProperties(page1, viewModel, panel);
            SetPageProperties(page2, viewModel, panel);

            if (!existingPages.Contains(page1)) panel.Children.Add(page1);
            if (!existingPages.Contains(page2)) panel.Children.Add(page2);
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
            if (gv.Code.Equals("SV"))
            {
                if (singleAuctionmethodFlag == true && !gv.CowDistinction.Equals("5"))
                {
                    _timer.Start();
                }

                singleAuctionmethodFlag = true;
            }

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

        // 타이머를 중지하는 메서드
        public void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
        }
    }
}
