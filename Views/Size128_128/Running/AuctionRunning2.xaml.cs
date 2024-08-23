using CowAuctionSmall.Models;
using System.Windows;
using System.Windows.Controls;


using Canvas = System.Windows.Controls.Canvas;
namespace CowAuctionSmall.Views
{
    /// <summary>
    /// AuctionRunning2.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AuctionRunning2 : UserControl
    {

        public AuctionRunning2()
        {
            InitializeComponent();
            Loaded += AuctionRunning2_Loaded;
        }

        private void AuctionRunning2_Loaded(object sender, RoutedEventArgs e)
        {


            if (note.Text.Length>8)
            {
                if (note.ActualWidth > 120 || note.Text.Replace(" ", "").Length > 10)
                {
                    StartScrollingAnimation();
                }
            }
            
        }

        private void StartScrollingAnimation()
        {
            FlowTextAnimation scrollingText = new FlowTextAnimation(note, canvas); // note는 TextBlock, canvas는 애니메이션할 패널
            scrollingText.Start(); // 속도 설정 (속도 값이 클수록 빠름)
        }
    }
}
