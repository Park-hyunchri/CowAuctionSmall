using CowAuctionSmall.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CowAuctionSmall.Views.Size128_128.Running
{
    /// <summary>
    /// RunningNoteHost128.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RunningNoteHost128 : UserControl
    {
        public static readonly DependencyProperty PageContentProperty = DependencyProperty.Register(
            nameof(PageContent),
            typeof(object),
            typeof(RunningNoteHost128),
            new PropertyMetadata(null));

        private FlowTextAnimation? _flowTextAnimation;

        public RunningNoteHost128()
        {
            InitializeComponent();
            Loaded += RunningNoteHost128_Loaded;
            Unloaded += RunningNoteHost128_Unloaded;
        }

        public object? PageContent
        {
            get => GetValue(PageContentProperty);
            set => SetValue(PageContentProperty, value);
        }

        private void RunningNoteHost128_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(StartScrollingAnimation), DispatcherPriority.Render);
        }

        private void StartScrollingAnimation()
        {
            if (_flowTextAnimation == null)
            {
                if (DataContext is AuctionContPanelViewModel viewModel)
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

        private void RunningNoteHost128_Unloaded(object sender, RoutedEventArgs e)
        {
            _flowTextAnimation?.Stop();
        }
    }
}
