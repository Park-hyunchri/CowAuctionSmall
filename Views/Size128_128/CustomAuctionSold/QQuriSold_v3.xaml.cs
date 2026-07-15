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

namespace CowAuctionSmall.Views.Size128_128.CustomAuctionSold
{
    /// <summary>
    /// QQuriSold_v3.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class QQuriSold_v3 : UserControl
    {
        private FlowTextAnimation? _flowTextAnimation;

        public QQuriSold_v3()
        {
            InitializeComponent();
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
