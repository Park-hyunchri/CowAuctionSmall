using CowAuctionSmall.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CowAuctionSmall.Views.Size128_64.Running
{
    /// <summary>
    /// AuctionRunning2_64.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AuctionRunning2_64 : UserControl
    {
        public AuctionRunning2_64()
        {
            InitializeComponent();
            Loaded += AuctionRunning2_64_Loaded;
        }

        private void AuctionRunning2_64_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                note.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                note.Arrange(new Rect(note.DesiredSize));

                // 강제 렌더링 후 너비 확인
                if (note.ActualWidth > 0 && note.Text.Length > 8 && note.ActualWidth > 120)
                {
                    StartScrollingAnimation();
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        /*        private void StartScrollingAnimation()
                {
                    FlowTextAnimation scrollingText = new FlowTextAnimation(note, canvas); // note는 TextBlock, canvas는 애니메이션할 패널
                    scrollingText.Start(); // 속도 설정 (속도 값이 클수록 빠름)
                }*/
        private void StartScrollingAnimation()
        {
            if (DataContext is AuctionContPanelViewModel viewModel)
            {
                FlowTextAnimation scrollingText = new FlowTextAnimation(note, canvas, viewModel);
                scrollingText.Start();
            }
        }

    }
}
