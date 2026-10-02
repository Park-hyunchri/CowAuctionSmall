using CowAuctionSmall.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CowAuctionSmall.Views.Size128_64.Running
{
    public partial class SokchoRunning1_64 : UserControl
    {
        public SokchoRunning1_64()
        {
            InitializeComponent();
            Loaded += SokchoRunning1_64_Loaded;
        }

        private void SokchoRunning1_64_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                note.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                note.Arrange(new Rect(note.DesiredSize));

                if (note.ActualWidth > 0 && note.Text.Length > 8 && note.ActualWidth > 120)
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
