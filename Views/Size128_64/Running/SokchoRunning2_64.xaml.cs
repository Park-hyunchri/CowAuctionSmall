using CowAuctionSmall.Presentation.Animation;
using CowAuctionSmall.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CowAuctionSmall.Views.Size128_64.Running
{
    public partial class SokchoRunning2_64 : UserControl
    {
        private FlowTextAnimation? _scrollingText;
        private System.Windows.Threading.DispatcherOperation? _pendingScroll;
        public SokchoRunning2_64()
        {
            InitializeComponent();
            Loaded += SokchoRunning2_64_Loaded;
            Unloaded += (_, _) =>
            {
                _pendingScroll?.Abort();
                _pendingScroll = null;
                _scrollingText?.Stop();
                _scrollingText = null;
            };
            DataContextChanged += (_, _) =>
            {
                _scrollingText?.Stop();
                _scrollingText = null;
                if (IsLoaded) SokchoRunning2_64_Loaded(this, new RoutedEventArgs());
            };
        }

        private void SokchoRunning2_64_Loaded(object sender, RoutedEventArgs e)
        {
            _pendingScroll?.Abort();
            _pendingScroll = Dispatcher.BeginInvoke(new Action(() =>
            {
                _pendingScroll = null;
                if (!IsLoaded) return;
                note.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                note.Arrange(new Rect(note.DesiredSize));

                StartScrollingAnimation();
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void StartScrollingAnimation()
        {
            if (DataContext is AuctionContPanelViewModel viewModel)
            {
                _scrollingText?.Stop();
                _scrollingText = new FlowTextAnimation(note, canvas, viewModel);
                _scrollingText.Start();
            }
        }
    }
}
