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
using System.Windows.Threading;

namespace CowAuctionSmall.Views.Size128_128.Running
{
    /// <summary>
    /// RunningNoteHost128_Running.xaml에 대한 상호 작용 논리
    /// </summary>
    // <summary>
    /// RunningNoteHost128.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RunningNoteHost128_Running : UserControl
    {
        // 1. 호스트 내부 뷰 콘텐츠를 바인딩하는 의존성 프로퍼티
        public static readonly DependencyProperty PageContentProperty = DependencyProperty.Register(
            nameof(PageContent),
            typeof(object),
            typeof(RunningNoteHost128_Running),
            new PropertyMetadata(null, OnPageContentChanged));

        public object PageContent
        {
            get => GetValue(PageContentProperty);
            set => SetValue(PageContentProperty, value);
        }

        private FlowTextAnimation? _flowTextAnimation;

        public RunningNoteHost128_Running()
        {
            InitializeComponent();
            Loaded += RunningNoteHost128_Loaded;
            Unloaded += RunningNoteHost128_Unloaded;
            DataContextChanged += RunningNoteHost128_DataContextChanged; // 💡 데이터 변경 감지 추가
        }

        private void RunningNoteHost128_Loaded(object sender, RoutedEventArgs e)
        {
            InitNoteAnimation();
        }

        private void RunningNoteHost128_Unloaded(object sender, RoutedEventArgs e)
        {
            DisposeAnimation();
        }

        private void RunningNoteHost128_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            InitNoteAnimation(); // 💡 소 개체 정보가 바뀌면 애니메이션 재시작
        }

        private static void OnPageContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RunningNoteHost128_Running host && host.IsLoaded)
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

                // 💡 ViewModel이 있으면 ViewModel을 포함하는 생성자 호출
                if (DataContext is CowAuctionSmall.ViewModels.AuctionContPanelViewModel viewModel)
                {
                    _flowTextAnimation = new FlowTextAnimation(note, canvasNote, viewModel, speed: 18, useRenderTransform: true);
                }
                else
                {
                    _flowTextAnimation = new FlowTextAnimation(note, canvasNote, speed: 18, useRenderTransform: true);
                }

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

