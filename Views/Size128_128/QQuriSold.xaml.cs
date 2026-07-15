using System;
using System.Windows;
using System.Windows.Controls;

namespace CowAuctionSmall.Views.Size128_128
{
    /// <summary>
    /// QQuriSold.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class QQuriSold : UserControl
    {
        private FlowTextAnimation? _flowTextAnimation;
        public QQuriSold()
        {
            InitializeComponent();
            Loaded += StandardSold_Loaded;
            Unloaded += QQuriSold_Unloaded;
        }
        private void StandardSold_Loaded(object sender, RoutedEventArgs e)
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

        private void StartScrollingAnimation()
        {
            if (_flowTextAnimation == null)
            {
                if (DataContext is CowAuctionSmall.ViewModels.AuctionContPanelViewModel viewModel)
                {
                    _flowTextAnimation = new FlowTextAnimation(note, canvas, viewModel, useRenderTransform: true, pageKey: Name);
                }
                else
                {
                    _flowTextAnimation = new FlowTextAnimation(note, canvas, useRenderTransform: true, pageKey: Name);
                }
            }

            _flowTextAnimation.Start();
        }

        private void QQuriSold_Unloaded(object sender, RoutedEventArgs e)
        {
            _flowTextAnimation?.Stop();
        }
    }
}
