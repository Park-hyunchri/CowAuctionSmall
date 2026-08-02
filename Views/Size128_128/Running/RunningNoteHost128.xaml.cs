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
        // 1. 호스트 내부 뷰 콘텐츠를 바인딩하는 의존성 프로퍼티
        public static readonly DependencyProperty PageContentProperty = DependencyProperty.Register(
            nameof(PageContent),
            typeof(object),
            typeof(RunningNoteHost128),
            new PropertyMetadata(null, OnPageContentChanged));

        public object PageContent
        {
            get => GetValue(PageContentProperty);
            set => SetValue(PageContentProperty, value);
        }

        private FlowTextAnimation? _flowTextAnimation;

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
            DisposeAnimation();

            // DispatcherPriority.Background를 첫 번째 인자로 배치
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                // RunningNoteHost128.xaml의 <TextBlock x:Name="note"> 및 <Canvas x:Name="canvas"> 참조
                if (note == null || canvasNote == null) return;

                note.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                note.Arrange(new Rect(note.DesiredSize));

                // 💡 [수정 1] 생성자에 note(TextBlock)와 canvas(Canvas) 인자 전달
                _flowTextAnimation = new FlowTextAnimation(note, canvasNote);

                // 💡 [수정 2] Start()는 인자 없이 호출
                _flowTextAnimation.Start();
            }));
        }

        /// <summary>
        /// 애니메이션 메모리 리소스 해제
        /// </summary>
        private void DisposeAnimation()
        {
            if (_flowTextAnimation != null)
            {
                _flowTextAnimation.Stop();
                _flowTextAnimation = null;
            }
        }
    }
}