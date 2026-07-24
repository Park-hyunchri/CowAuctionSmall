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

namespace CowAuctionSmall.Views.Size128_128.CustomAUctionUnSold
{
    /// <summary>
    /// ChuncheonUnSold.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChuncheonUnSold : UserControl
    {
        private FlowTextAnimation? _flowTextAnimation;
        public ChuncheonUnSold()
        {
            InitializeComponent();
            Loaded += ChuncheonUnSold_Loaded;
            Unloaded += ChuncheonUnSold_Unloaded;
        }

        private void ChuncheonUnSold_Loaded(object sender, RoutedEventArgs e)
        {

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (note.Text.Length > 8)
                {
                    if (note.ActualWidth > 120 || note.Text.Replace(" ", "").Length > 8)
                    {
                        StartScrollingAnimation();
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void StartScrollingAnimation()
        {
            if (_flowTextAnimation == null)
            {
                if (DataContext is CowAuctionSmall.ViewModels.AuctionContPanelViewModel viewModel)
                {
                    _flowTextAnimation = new FlowTextAnimation(note, canvas, viewModel);
                }
                else
                {
                    _flowTextAnimation = new FlowTextAnimation(note, canvas);
                }
            }

            _flowTextAnimation.Start();
        }


        private void ChuncheonUnSold_Unloaded(object sender, RoutedEventArgs e)
        {
            _flowTextAnimation?.Stop();
        }
    }
}