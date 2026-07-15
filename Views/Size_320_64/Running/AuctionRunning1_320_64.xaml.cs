using CowAuctionSmall.Models;
using CowAuctionSmall.ViewModels;
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

namespace CowAuctionSmall.Views.Size_320_64.Running
{
    /// <summary>
    /// AuctionRunning1_320_64.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AuctionRunning1_320_64 : UserControl
    {
        public AuctionRunning1_320_64()
        {
            InitializeComponent();
            Loaded += AuctionRunning1_320_64_Loaded;
        }
        private void AuctionRunning1_320_64_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                note.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                note.Arrange(new Rect(note.DesiredSize));

                // 강제 렌더링 후 너비 확인
                if (note.ActualWidth > 0 && note.Text.Length > 12 && note.ActualWidth > 148)
                {
                    StartScrollingAnimation();
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

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
