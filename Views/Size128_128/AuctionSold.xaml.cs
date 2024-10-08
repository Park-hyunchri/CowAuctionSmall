using CowAuctionSmall.Models;
using DocumentFormat.OpenXml.InkML;
using System;
using System.Windows;
using System.Windows.Controls;
using Canvas = System.Windows.Controls.Canvas;

namespace CowAuctionSmall.Views.Size128_128
{
    /// <summary>
    /// AuctionSold.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AuctionSold : UserControl
    {
        public AuctionSold()
        {
            InitializeComponent();
            Loaded += AuctionSold_Loaded;
        }
        private void AuctionSold_Loaded(object sender, RoutedEventArgs e)
        {

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (note.Text.Length > 8)
                {
                    if (note.ActualWidth > 120)
                    {
                        StartScrollingAnimation();
                    }
                }

                if (bidder.Text.Length > 6)
                {
                    StartScrollingAnimation2();
                }
            }), System.Windows.Threading.DispatcherPriority.Render);


        }

        private void StartScrollingAnimation()
        {
            FlowTextAnimation scrollingText = new FlowTextAnimation(note, canvas); // note는 TextBlock, canvas는 애니메이션할 패널
            scrollingText.Start(); // 속도 설정 (속도 값이 클수록 빠름)
        }

        private void StartScrollingAnimation2()
        {
            FlowTextAnimation scrollingText2 = new FlowTextAnimation(bidder, canvasBidder); // note는 TextBlock, canvas는 애니메이션할 패널
            scrollingText2.Start2(); // 속도 설정 (속도 값이 클수록 빠름)
        }
    }
}
