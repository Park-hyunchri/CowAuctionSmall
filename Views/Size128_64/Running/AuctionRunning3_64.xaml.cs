using CowAuctionSmall.Models;
using DocumentFormat.OpenXml.InkML;
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
    /// AuctionRunning3_64.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AuctionRunning3_64 : UserControl
    {
        public AuctionRunning3_64()
        {
            InitializeComponent();
            //Loaded += AuctionRunning3_64_Loaded;
        }

        /*private void AuctionRunning3_64_Loaded(object sender, RoutedEventArgs e)
        {


            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (note.Text.Length > 8)
                {
                    if (note.ActualWidth > 120)// || note.Text.Replace(" ", "").Length > 8)
                    {
                        StartScrollingAnimation();
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void StartScrollingAnimation()
        {
            FlowTextAnimation scrollingText = new FlowTextAnimation(note, canvas); // note는 TextBlock, canvas는 애니메이션할 패널
            scrollingText.Start(); // 속도 설정 (속도 값이 클수록 빠름)
        }*/
    }
}
