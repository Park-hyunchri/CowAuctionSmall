using CowAuctionSmall.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            Sex.Text = Sex.Text.Length > 0 ? Sex.Text.Substring(0) : "";
            if (note.Text.Length > 8)
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
