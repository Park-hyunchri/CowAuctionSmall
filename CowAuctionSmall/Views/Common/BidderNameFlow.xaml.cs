using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CowAuctionSmall.Views.Common
{
    /// <summary>
    /// 지정한 글자 수를 초과하는 낙찰자 이름을 한 줄로 흘려보내는 컨트롤입니다.
    /// </summary>
    public partial class BidderNameFlow : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(BidderNameFlow),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ScrollThresholdProperty = DependencyProperty.Register(
            nameof(ScrollThreshold),
            typeof(int),
            typeof(BidderNameFlow),
            new PropertyMetadata(5, OnAnimationSettingChanged));

        public static readonly DependencyProperty SpeedProperty = DependencyProperty.Register(
            nameof(Speed),
            typeof(double),
            typeof(BidderNameFlow),
            new PropertyMetadata(18.0, OnAnimationSettingChanged));

        private FlowTextAnimation? _flowTextAnimation;
        private DispatcherOperation? _initializeOperation;

        public BidderNameFlow()
        {
            InitializeComponent();
            Loaded += BidderNameFlow_Loaded;
            Unloaded += BidderNameFlow_Unloaded;
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public int ScrollThreshold
        {
            get => (int)GetValue(ScrollThresholdProperty);
            set => SetValue(ScrollThresholdProperty, value);
        }

        public double Speed
        {
            get => (double)GetValue(SpeedProperty);
            set => SetValue(SpeedProperty, value);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == FontSizeProperty && IsLoaded)
            {
                InitializeAnimation();
            }
        }

        private void BidderNameFlow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeAnimation();
        }

        private void BidderNameFlow_Unloaded(object sender, RoutedEventArgs e)
        {
            DisposeAnimation();
        }

        private static void OnAnimationSettingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidderNameFlow control && control.IsLoaded)
            {
                control.InitializeAnimation();
            }
        }

        private void InitializeAnimation()
        {
            DisposeAnimation();

            _initializeOperation = Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                _initializeOperation = null;
                if (!IsLoaded)
                {
                    return;
                }

                _flowTextAnimation = new FlowTextAnimation(
                    flowText,
                    flowCanvas,
                    speed: Speed,
                    useRenderTransform: true,
                    minScrollTextLength: ScrollThreshold,
                    forceScrollWhenLengthExceeded: true);
                _flowTextAnimation.Start();
            }));
        }

        private void DisposeAnimation()
        {
            if (_initializeOperation?.Status == DispatcherOperationStatus.Pending)
            {
                _initializeOperation.Abort();
            }
            _initializeOperation = null;

            _flowTextAnimation?.Stop();
            _flowTextAnimation = null;
        }
    }
}
