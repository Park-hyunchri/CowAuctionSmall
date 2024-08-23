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
        public ObservableCollection<AuctionContPanelViewModel> RunningViewModel { get; private set; } // 진행중인 뷰모델만 관리

        private UserInfo _userInfo;
        private BoardList _boardinfo;

        private List<int> pageTime = new List<int>();
        private int _totalRunningPage = 0;

        private DispatcherTimer _timer; //왔다갔다 타이머
        private int _rotationIndex = 0; //화면 왔다갔다 인덱스

        //private readonly WeakReferenceMessenger _messenger;
        //private readonly WeakReferenceMessenger _messengerStringArr;

        public DisplaySelect(UserInfo userInfo,BoardList boardinfo)
        {


            _userInfo = userInfo;
            _boardinfo = boardinfo;

            if (_userInfo.Auction.BoardPage.Length <=0)
            {
                _totalRunningPage = 1;
            }
            else
            {
                _totalRunningPage = Convert.ToInt32(_userInfo.Auction.BoardPage);
            }

            pageTime.Add(Convert.ToInt32(_userInfo.Auction.BoardPageTime)); //Convert.ToInt32 null일 경우 0 반환
            pageTime.Add(Convert.ToInt32(_userInfo.Auction.BoardPageTime2));
            pageTime.Add(Convert.ToInt32(_userInfo.Auction.BoardPageTime3));

            RunningViewModel = new ObservableCollection<AuctionContPanelViewModel>();

            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Start();


        }

        

        private string IsMultipleLogo(string panelName) //로고를 여러개를 보여줘야 한다면 한줄마다 다르게 보여주기
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
                logoImgName ="logo.bmp";
            }

            if (logoImgName != null)
            {
                // 폴더 경로 설정
                string folderPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Config", logoImgName);
                // 만약 일치하는 ID가 없는 경우에 대한 처리
                return folderPath; // 또는 다른 값을 반환하거나 예외를 throw할 수 있음
            }
            else
            {
                return string.Empty;
            }
        }

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

        private void RemoveViewModelFromPanel(VirtualizingStackPanel panel)
        {
            // 패널에 연결된 뷰모델 찾기
            var viewModel = RunningViewModel.FirstOrDefault(vm => vm._panel == panel);
            if (viewModel != null)
            {
                RunningViewModel.Remove(viewModel);
                viewModel.Dispose();
                panel.DataContext = null;
                
            }
        }




        private bool singleAuctionmethodFlag = true; //단일경매 진행시 계속 새로고침을 할수는 없으니
        /// <summary>
        /// 진행할 화면 넣기 추후 여러페이지 보여주기(고민..)
        /// </summary>
        public void DisplayRunning(VirtualizingStackPanel panel, gValues cowInfo, int auctionmethod)
        {
            
            

            // 데이터 바인딩을 통해 UI를 표시
            var viewModel = new AuctionContPanelViewModel(cowInfo,this,panel, pageTime, _totalRunningPage);
            RunningViewModel.Add(viewModel);
            InitializePages(panel,  viewModel);


            if (panel.Children.Count !=0 )  //거치대 변경시 타이머 때문에 일순간 패널의 자식이 0이됨
            {
                DisplayRunningPageNum(viewModel._panel, cowInfo, 1);
                
            }

            //염소 경매인경우
            if (cowInfo.CowDistinction.Equals("5"))
            {
                DisplayRunningPageNum(viewModel._panel, cowInfo, 1);
                _timer.Stop();
            }

            switch (auctionmethod)
            {
                case 10: //일괄 경매
                    break;
                case 20: //단일 경매
                    if (cowInfo.IsRunning == true && singleAuctionmethodFlag) //단일경매 방식이면서 경매시작을 했을경우 모든 페이지는 첫페이지로 전환되어야한다.
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

        public void InitializePages(VirtualizingStackPanel panel, AuctionContPanelViewModel viewModel)
        {
            // 패널에 이미 추가된 컨트롤이 있는지 확인합니다.
            var existingPages = panel.Children.OfType<UserControl>().ToList();
            UserControl page1, page2;    //, page3;

            // 128x128 크기의 패널인 경우
            if (_boardinfo.Size.Equals("128,128") || _boardinfo.Size.Equals("128*128"))
            {
                page1 = existingPages.FirstOrDefault(p => p.Name == "RunPage1") ?? new AuctionRunning1 { Name = "RunPage1" };
                page2 = existingPages.FirstOrDefault(p => p.Name == "RunPage2") ?? new AuctionRunning2 { Name = "RunPage2" };
                
                //페이지 3을 적용한 축협x
                //page3 = existingPages.FirstOrDefault(p => p.Name == "RunPage3") ?? new AuctionRunning3 { Name = "RunPage3" };
            }
            // 128x64 크기의 패널인 경우
            else
            {
                page1 = existingPages.FirstOrDefault(p => p.Name == "RunPage1_64") ?? new AuctionRunning1_64 { Name = "RunPage1_64" };
                page2 = existingPages.FirstOrDefault(p => p.Name == "RunPage2_64") ?? new AuctionRunning2_64 { Name = "RunPage2_64" };

                //페이지 3을 적용한 축협x
                //page3 = existingPages.FirstOrDefault(p => p.Name == "RunPage3_64") ?? new AuctionRunning3_64 { Name = "RunPage3_64" };
            }

            // 모든 페이지의 DataContext와 크기를 설정합니다.
            SetPageProperties(page1, viewModel, panel);
            SetPageProperties(page2, viewModel, panel);
            //SetPageProperties(page3, viewModel, panel);

            // 패널에 컨트롤이 이미 추가되지 않은 경우에만 추가합니다.
            if (!existingPages.Contains(page1)) panel.Children.Add(page1);
            if (!existingPages.Contains(page2)) panel.Children.Add(page2);
            //if (!existingPages.Contains(page3)) panel.Children.Add(page3);
        }

        private void SetPageProperties(UserControl page, AuctionContPanelViewModel viewModel, VirtualizingStackPanel panel)
        {
            page.DataContext = viewModel;
            page.Visibility = Visibility.Collapsed;
            page.Width = panel.Width;
            page.Height = panel.Height;
        }



        public void DisplayRunningPageNum(VirtualizingStackPanel panel, gValues cowinfo, int pageNum)
        {
            // 모든 UserControl을 숨깁니다.
            foreach (UIElement child in panel.Children)
            {
                child.Visibility = Visibility.Collapsed;
            }

            // 경매가 진행 중이면 뷰모델을 새로 생성하지 않고, 기존 뷰모델을 사용합니다.
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

            // 선택된 페이지를 표시합니다.
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
            // 데이터 바인딩을 통해 UI를 표시

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
            cowPanel.Name = "";

            cowPanel.Width = panel.Width;
            cowPanel.Height = panel.Height;

            panel.Children.Add(cowPanel);
        }



        /// <summary>
        /// 유찰된 화면 넣기
        /// </summary>
        public void DisplayUnSold(VirtualizingStackPanel panel, gValues cowInfo)
        {

            // 데이터 바인딩을 통해 UI를 표시
            UserControl cowPanel = null;
            if (_boardinfo.Size.Equals("128,128") || _boardinfo.Size.Equals("128*128"))
            {
                cowPanel = new AuctionUnSold();
            }
            else // 무조건 128,64라고 가정하고
            {
                cowPanel = new AuctionUnSold_64();
            }
            cowPanel.DataContext = cowInfo;

            cowPanel.Width = panel.Width;
            cowPanel.Height = panel.Height;

            panel.Children.Add(cowPanel);

            


        }

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

        public void FindPanel(string pName, System.Collections.ObjectModel.ObservableCollection<VirtualizingStackPanel> panels,gValues gv, int auctionmethod)
        {
            if(gv.Code.Equals("SV"))
            {
                if (singleAuctionmethodFlag==true && !gv.CowDistinction.Equals("5"))
                {
                    _timer.Start();
                }

                singleAuctionmethodFlag = true;
                
            }

            // 이름으로 패널 찾기
            VirtualizingStackPanel panel1 = FindPanel(p => p.Name == pName, panels);

            // ...
            int auctionStatus = int.Parse(gv.AuctionResultStatus);
            if (panel1 != null)
            {
                // 뷰모델 객체 제거
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
                 //컴퓨터가 여러개로 쪼개져 운영할경우 동기화 하기 위해 사용
                if (gv.IsRunning == true && singleAuctionmethodFlag ==true)
                {
                    Debug.WriteLine("단일경매단일경매단일경매단일경매단일경매 시작");
                    singleAuctionmethodFlag = false;


                    foreach (AuctionContPanelViewModel vm in RunningViewModel)
                    {
                        DisplayRunningPageNum(vm._panel, vm.CowInfo, 1);
                    }
                    _timer.Stop();
                }
            }


        }
        /// <summary>
        /// 전체 생성된 패널 cow_1~cow_n까지 중 원하는거 찾기
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="panels"></param>
        /// <returns></returns>
        private VirtualizingStackPanel? FindPanel(Func<VirtualizingStackPanel, bool> predicate, System.Collections.ObjectModel.ObservableCollection<VirtualizingStackPanel> panels)
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


        /// <summary>
        /// 진행중일때 화면 번갈아 가면서 표출
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (AuctionContPanelViewModel viewModel in RunningViewModel)
            {
                DisplayRunningPageNum(viewModel._panel, viewModel.CowInfo, _rotationIndex + 1);
            }

            _timer.Interval = TimeSpan.FromSeconds(pageTime[_rotationIndex]);

            _rotationIndex = (_rotationIndex + 1) % _totalRunningPage;
        }

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
