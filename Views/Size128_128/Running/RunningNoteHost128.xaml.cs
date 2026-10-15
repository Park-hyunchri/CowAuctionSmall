using CowAuctionSmall.Presentation.Animation;
using System;
using CowAuctionSmall.ViewModels;
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
        // 1. 호스트 내부 뷰 콘텐츠를 바인딩하는 의존성 프로퍼티
        public static readonly DependencyProperty PageContentProperty = DependencyProperty.Register(
            nameof(PageContent),
            typeof(object),
            typeof(RunningNoteHost128),
            new PropertyMetadata(null, OnPageContentChanged));

        public object? PageContent
        {
            get => GetValue(PageContentProperty);
            set => SetValue(PageContentProperty, value);
        }

        public static readonly DependencyProperty ItemDisplayProperty = DependencyProperty.Register(
            nameof(ItemDisplay), typeof(AuctionItemDisplayViewModel), typeof(RunningNoteHost128),
            new PropertyMetadata(null, OnItemDisplayChanged));

        public AuctionItemDisplayViewModel? ItemDisplay
        {
            get => (AuctionItemDisplayViewModel?)GetValue(ItemDisplayProperty);
            set => SetValue(ItemDisplayProperty, value);
        }

        public static readonly DependencyProperty ScrollOwnerProperty = DependencyProperty.Register(
            nameof(ScrollOwner), typeof(AuctionContPanelViewModel), typeof(RunningNoteHost128),
            new PropertyMetadata(null, OnScrollOwnerChanged));

        public AuctionContPanelViewModel? ScrollOwner
        {
            get => (AuctionContPanelViewModel?)GetValue(ScrollOwnerProperty);
            set => SetValue(ScrollOwnerProperty, value);
        }

        public static readonly DependencyProperty UseCalfPaternityBadgeProperty = DependencyProperty.Register(
            nameof(UseCalfPaternityBadge), typeof(bool), typeof(RunningNoteHost128), new PropertyMetadata(false));

        public bool UseCalfPaternityBadge
        {
            get => (bool)GetValue(UseCalfPaternityBadgeProperty);
            set => SetValue(UseCalfPaternityBadgeProperty, value);
        }

        private FlowTextAnimation? _flowTextAnimation;
        private DispatcherOperation? _pendingAnimation;

        public RunningNoteHost128()
        {
            InitializeComponent();
            Loaded += RunningNoteHost128_Loaded;
            Unloaded += RunningNoteHost128_Unloaded;
        }

        private void RunningNoteHost128_Loaded(object sender, RoutedEventArgs e)
        {
            InitNoteAnimation();
        }

        private void RunningNoteHost128_Unloaded(object sender, RoutedEventArgs e)
        {
            DisposeAnimation();
        }

        private static void OnItemDisplayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (RunningNoteHost128)d;
            var previous = (e.OldValue as AuctionItemDisplayViewModel)?.Snapshot;
            var current = (e.NewValue as AuctionItemDisplayViewModel)?.Snapshot;
            bool sameAnimal = previous != null && current != null &&
                previous.TraceNumber == current.TraceNumber && previous.SipNumber == current.SipNumber &&
                previous.SpaceIndex == current.SpaceIndex;
            if (!sameAnimal)
            {
                host.DisposeAnimation();
                host.ScrollOwner?.ResetNotePositions();
            }
            host.InitNoteAnimation();
        }

        private static void OnScrollOwnerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (RunningNoteHost128)d;
            host.DisposeAnimation();
            host.InitNoteAnimation();
        }

        private static void OnPageContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RunningNoteHost128 host && host.IsLoaded)
            {
                host.InitNoteAnimation();
            }
        }

        /// <summary>
        /// 비고 텍스트 애니메이션 초기화 및 작동
        /// </summary>
        private void InitNoteAnimation()
        {
            if (!IsLoaded || ItemDisplay == null || _flowTextAnimation != null || _pendingAnimation != null) return;

            // DispatcherPriority.Background를 첫 번째 인자로 배치
            _pendingAnimation = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                _pendingAnimation = null;
                if (!IsLoaded || ItemDisplay == null || _flowTextAnimation != null) return;
                // RunningNoteHost128.xaml의 <TextBlock x:Name="note"> 및 <Canvas x:Name="canvas"> 참조
                if (note == null || canvasNote == null) return;

                note.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                note.Arrange(new Rect(note.DesiredSize));

                // 💡 ViewModel이 있으면 ViewModel을 포함하는 생성자 호출
                if (ScrollOwner is AuctionContPanelViewModel viewModel)
                {
                    _flowTextAnimation = new FlowTextAnimation(note, canvasNote, viewModel, speed: 18, useRenderTransform: true, coalesceTextChanges: true);
                }
                else
                {
                    _flowTextAnimation = new FlowTextAnimation(note, canvasNote, speed: 18, useRenderTransform: true, coalesceTextChanges: true);
                }

                _flowTextAnimation.Start();
            }));
        }

        /// <summary>
        /// 애니메이션 메모리 리소스 해제
        /// </summary>
        private void DisposeAnimation()
        {
            _pendingAnimation?.Abort();
            _pendingAnimation = null;
            if (_flowTextAnimation != null)
            {
                _flowTextAnimation.Stop();
                _flowTextAnimation = null;
            }
        }
    }
}
