using CowAuctionSmall.Models;
using DocumentFormat.OpenXml.InkML;
using System.Windows;
using System.Windows.Controls;
using Canvas = System.Windows.Controls.Canvas;
namespace CowAuctionSmall.Views.Size128_128
{
    /// <summary>
    /// AuctionUnSold.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AuctionUnSold : UserControl
    {

        public AuctionUnSold()
        {
            InitializeComponent();
            Loaded += AuctionUnSold_Loaded;
        }

        private void AuctionUnSold_Loaded(object sender, RoutedEventArgs e)
        {

            if (note.ActualWidth > 120)
            {
                StartScrollingAnimation();
            }
        }

        private void StartScrollingAnimation()
        {
            FlowTextAnimation scrollingText = new FlowTextAnimation(note, canvas); // note는 TextBlock, canvas는 애니메이션할 패널
            scrollingText.Start(); // 속도 설정 (속도 값이 클수록 빠름)
        }

    }
}
