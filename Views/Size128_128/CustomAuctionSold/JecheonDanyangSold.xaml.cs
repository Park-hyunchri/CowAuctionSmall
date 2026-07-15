using System;
using System.Windows;
using System.Windows.Controls;

namespace CowAuctionSmall.Views.Size128_128.CustomAuctionSold
{
    /// <summary>
    /// JecheonDanyangSold.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class JecheonDanyangSold : UserControl
    {
        private FlowTextAnimation? _flowTextAnimation;
        public JecheonDanyangSold()
        {
            InitializeComponent();
            Loaded += JecheonDanyangSold_Loaded;
            Unloaded += QQuriUnSold_Unloaded;
        }
        private void JecheonDanyangSold_Loaded(object sender, RoutedEventArgs e)
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

        private void QQuriUnSold_Unloaded(object sender, RoutedEventArgs e)
        {
            _flowTextAnimation?.Stop();
        }
    }
}
