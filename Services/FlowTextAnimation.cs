using CowAuctionSmall.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

/// <summary>
/// Marquee text animation using WPF animation clocks.
/// </summary>
public class FlowTextAnimation
{
    private const string SharedNoteKey = "__shared_note__";
    private const int MinScrollTextLength = 8;

    private readonly TextBlock _textBlock;
    private readonly Canvas _canvas;
    private readonly AuctionContPanelViewModel? _viewModel;
    private readonly double _speed;
    private readonly bool _useViewModel;
    private readonly bool _useRenderTransform;
    private readonly string? _pageKey;
    private TranslateTransform? _translate;

    private double _notePosition;
    private double _viewportWidth;
    private double _textWidth;
    private double _animationDistance;
    private DateTime _animationStartUtc;
    private bool _isAnimating;
    private bool _shouldScroll;
    private AnimationClock? _animationClock;
    private bool _unloadedHooked;
    private bool _sizeHooked;
    private bool _textHooked;
    private bool _visibilityHooked;
    private DependencyPropertyDescriptor? _textDescriptor;

    /// <summary>
    /// 뷰모델 연동형 마퀴 애니메이션을 생성한다.
    /// </summary>
    public FlowTextAnimation(TextBlock textBlock, Canvas canvas, AuctionContPanelViewModel viewModel, double speed = 15, bool useRenderTransform = true, string? pageKey = null)
    {
        _textBlock = textBlock;
        _canvas = canvas;
        _viewModel = viewModel;
        _speed = speed;
        _useViewModel = true;
        _useRenderTransform = useRenderTransform;
        _pageKey = pageKey;
    }

    /// <summary>
    /// 뷰모델 없이 동작하는 마퀴 애니메이션을 생성한다.
    /// </summary>
    public FlowTextAnimation(TextBlock textBlock, Canvas canvas, double speed = 15, bool useRenderTransform = true, string? pageKey = null)
    {
        _textBlock = textBlock;
        _canvas = canvas;
        _speed = speed;
        _useViewModel = false;
        _useRenderTransform = useRenderTransform;
        _pageKey = pageKey;
    }

    /// <summary>
    /// 애니메이션을 시작하거나 스크롤 여부를 평가한다.
    /// </summary>
    public void Start()
    {
        if (_isAnimating)
            return;

        HookUnload();
        HookTextChanged();
        HookVisibilityChanged();

        if (!_textBlock.IsVisible)
            return;

        PrepareLayout();

        if (!_shouldScroll)
        {
            HookSizeChanged();
            return;
        }

        StartAnimation();
    }

    /// <summary>
    /// 애니메이션과 이벤트 훅을 해제한다.
    /// </summary>
    public void Stop()
    {
        StopAnimation(false);
    }

    /// <summary>
    /// 애니메이션 상태를 중지하고 필요 시 이벤트 훅을 유지한다.
    /// </summary>
    private void StopAnimation(bool keepHooks, bool updatePosition = true)
    {
        if (!_isAnimating && keepHooks)
            return;

        if (_isAnimating)
        {
            if (updatePosition)
            {
                SaveCurrentPosition();
            }

            _isAnimating = false;
            StopClock();
        }

        if (!keepHooks)
        {
            UnhookUnload();
            UnhookSizeChanged();
            UnhookTextChanged();
            UnhookVisibilityChanged();
        }
    }

    /// <summary>
    /// 애니메이션 클록을 해제한다.
    /// </summary>
    private void StopClock()
    {
        if (_animationClock == null)
            return;

        _animationClock.Controller?.Stop();
        if (_useRenderTransform)
        {
            EnsureTranslateTransform().ApplyAnimationClock(TranslateTransform.XProperty, null);
        }
        else
        {
            _textBlock.ApplyAnimationClock(Canvas.LeftProperty, null);
        }

        _animationClock = null;
    }

    /// <summary>
    /// 현재 위치를 저장한다.
    /// </summary>
    private void SaveCurrentPosition()
    {
        var position = GetCurrentPositionUtc(DateTime.UtcNow);

        if (_useViewModel && _viewModel != null)
        {
            var key = ResolveNoteKey();
            if (!string.IsNullOrWhiteSpace(key))
            {
                _viewModel.SetNotePosition(key, position);
            }
        }
        else
        {
            _notePosition = position;
        }
    }

    /// <summary>
    /// 애니메이션을 시작하거나 재시작한다.
    /// </summary>
    private void StartAnimation(double? startPositionOverride = null, bool forceRestart = false)
    {
        if (_isAnimating && !forceRestart)
            return;

        if (_isAnimating)
        {
            StopAnimation(true, updatePosition: false);
        }

        _isAnimating = true;

        var startPosition = ResolveStartPosition(startPositionOverride);
        BeginMarquee(startPosition);
    }

    /// <summary>
    /// WPF 애니메이션으로 마퀴를 시작한다.
    /// </summary>
    private void BeginMarquee(double startPosition)
    {
        if (_viewportWidth <= 0 || _textWidth <= 0 || _speed <= 0)
        {
            _isAnimating = false;
            return;
        }

        _animationDistance = _viewportWidth + _textWidth;
        if (_animationDistance <= 0)
        {
            _isAnimating = false;
            return;
        }

        var durationSeconds = _animationDistance / _speed;
        var animation = new DoubleAnimation
        {
            From = _viewportWidth,
            To = -_textWidth,
            Duration = TimeSpan.FromSeconds(durationSeconds),
            RepeatBehavior = RepeatBehavior.Forever
        };

        _animationClock = animation.CreateClock();

        if (_useRenderTransform)
        {
            EnsureTranslateTransform().ApplyAnimationClock(TranslateTransform.XProperty, _animationClock);
        }
        else
        {
            _textBlock.ApplyAnimationClock(Canvas.LeftProperty, _animationClock);
        }

        var offsetSeconds = ComputeOffsetSeconds(startPosition);
        _animationStartUtc = DateTime.UtcNow - TimeSpan.FromSeconds(offsetSeconds);
        _animationClock.Controller?.Begin();
        if (offsetSeconds > 0)
        {
            _animationClock.Controller?.SeekAlignedToLastTick(TimeSpan.FromSeconds(offsetSeconds), TimeSeekOrigin.BeginTime);
        }
    }

    /// <summary>
    /// 현재 위치 기준으로 애니메이션 시작 오프셋을 계산한다.
    /// </summary>
    private double ComputeOffsetSeconds(double startPosition)
    {
        var clamped = ClampPosition(startPosition);
        var normalized = _viewportWidth - clamped;
        if (normalized < 0)
            normalized = 0;
        if (normalized > _animationDistance)
            normalized = _animationDistance;

        return _speed > 0 ? normalized / _speed : 0;
    }

    /// <summary>
    /// 시작 위치를 결정한다.
    /// </summary>
    private double ResolveStartPosition(double? overridePosition)
    {
        if (overridePosition.HasValue)
        {
            return ClampPosition(overridePosition.Value);
        }

        if (_useViewModel && _viewModel != null)
        {
            var key = ResolveNoteKey();
            if (!string.IsNullOrWhiteSpace(key))
            {
                var saved = _viewModel.GetNotePosition(key);
                if (saved.HasValue)
                {
                    return ClampPosition(saved.Value);
                }
            }
        }

        return ClampPosition(_notePosition);
    }

    /// <summary>
    /// 위치를 유효 범위로 고정한다.
    /// </summary>
    private double ClampPosition(double position)
    {
        var min = -_textWidth;
        var max = _viewportWidth;
        if (position < min) return min;
        if (position > max) return max;
        return position;
    }

    /// <summary>
    /// 현재 시각 기준으로 스크롤 위치를 계산한다.
    /// </summary>
    private double GetCurrentPositionUtc(DateTime utcNow)
    {
        if (_animationDistance <= 0 || _speed <= 0)
            return _viewportWidth;

        var elapsed = (utcNow - _animationStartUtc).TotalSeconds;
        if (elapsed < 0)
            elapsed = 0;

        var traveled = (_speed * elapsed) % _animationDistance;
        var pos = _viewportWidth - traveled;
        if (pos < -_textWidth)
            pos += _animationDistance;

        return pos;
    }

    /// <summary>
    /// 텍스트와 뷰포트 크기를 측정하고 스크롤 여부를 결정한다.
    /// </summary>
    private void PrepareLayout()
    {
        var currentText = _textBlock.Text ?? string.Empty;
        _viewportWidth = _canvas.ActualWidth > 0 ? _canvas.ActualWidth : _canvas.RenderSize.Width;

        if (_useViewModel && _viewModel != null)
        {
            var key = ResolveNoteKey();
            if (!string.IsNullOrWhiteSpace(key))
            {
                if (!_viewModel.IsSameNoteText(key, currentText))
                {
                    _viewModel.UpdateNoteTextSnapshot(key, currentText);
                    _viewModel.SetNotePosition(key, _viewportWidth);
                }

                var saved = _viewModel.GetNotePosition(key);
                var resolved = saved ?? _viewportWidth;
                _viewModel.SetNotePosition(key, resolved);
                _notePosition = resolved;
            }
            else
            {
                _notePosition = _viewportWidth;
            }
        }
        else
        {
            _notePosition = _viewportWidth;
        }

        if (currentText.Length <= MinScrollTextLength || _viewportWidth <= 0)
        {
            _shouldScroll = false;
            ApplyPosition(0); // Reset position when text is too short to scroll
            return;
        }

        _textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        _textBlock.Arrange(new Rect(new Size(_textBlock.DesiredSize.Width, _textBlock.DesiredSize.Height)));

        _textWidth = _textBlock.ActualWidth > 0 ? _textBlock.ActualWidth : _textBlock.DesiredSize.Width;
        _shouldScroll = _textWidth > _viewportWidth && _textWidth > 0 && _viewportWidth > 0;

        if (!_shouldScroll)
        {
            ApplyPosition(0); // Reset position if text width is not larger than viewport
        }
    }

    /// <summary>
    /// 현재 위치를 렌더 트랜스폼 또는 Canvas 좌표로 반영한다.
    /// </summary>
    private void ApplyPosition(double position)
    {
        if (_useRenderTransform)
        {
            EnsureTranslateTransform().X = position;
            return;
        }

        Canvas.SetLeft(_textBlock, position);
    }

    /// <summary>
    /// 텍스트에 TranslateTransform을 확보한다.
    /// </summary>
    private TranslateTransform EnsureTranslateTransform()
    {
        if (_translate != null)
            return _translate;

        if (_textBlock.RenderTransform is TranslateTransform existing)
        {
            _translate = existing;
            return _translate;
        }

        if (_textBlock.RenderTransform == null || _textBlock.RenderTransform == Transform.Identity)
        {
            var created = new TranslateTransform();
            _textBlock.RenderTransform = created;
            _translate = created;
            return _translate;
        }

        var group = new TransformGroup();
        group.Children.Add(_textBlock.RenderTransform);
        var translate = new TranslateTransform();
        group.Children.Add(translate);
        _textBlock.RenderTransform = group;
        _translate = translate;
        return _translate;
    }

    /// <summary>
    /// 캔버스 크기 변경 이벤트를 구독한다.
    /// </summary>
    private void HookSizeChanged()
    {
        if (_sizeHooked)
            return;

        _canvas.SizeChanged += OnCanvasSizeChanged;
        _sizeHooked = true;
    }

    /// <summary>
    /// 캔버스 크기 변경 이벤트를 해제한다.
    /// </summary>
    private void UnhookSizeChanged()
    {
        if (!_sizeHooked)
            return;

        _canvas.SizeChanged -= OnCanvasSizeChanged;
        _sizeHooked = false;
    }

    /// <summary>
    /// 캔버스 크기 변경 시 스크롤 조건을 재평가한다.
    /// </summary>
    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width <= 0)
            return;

        if (!_textBlock.IsVisible)
            return;

        var currentPosition = _isAnimating ? GetCurrentPositionUtc(DateTime.UtcNow) : (double?)null;
        PrepareLayout();
        if (_shouldScroll)
        {
            UnhookSizeChanged();
            StartAnimation(currentPosition, forceRestart: true);
        }
        else
        {
            StopAnimation(true);
            HookSizeChanged();
        }
    }

    /// <summary>
    /// 언로드 이벤트를 구독한다.
    /// </summary>
    private void HookUnload()
    {
        if (_unloadedHooked)
            return;

        _textBlock.Unloaded += OnUnloaded;
        _canvas.Unloaded += OnUnloaded;
        _unloadedHooked = true;
    }

    /// <summary>
    /// 언로드 이벤트를 해제한다.
    /// </summary>
    private void UnhookUnload()
    {
        if (!_unloadedHooked)
            return;

        _textBlock.Unloaded -= OnUnloaded;
        _canvas.Unloaded -= OnUnloaded;
        _unloadedHooked = false;
    }

    /// <summary>
    /// 텍스트 변경 이벤트를 구독한다.
    /// </summary>
    private void HookTextChanged()
    {
        if (_textHooked)
            return;

        _textDescriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
        _textDescriptor?.AddValueChanged(_textBlock, OnTextChanged);
        _textHooked = true;
    }

    /// <summary>
    /// 텍스트 변경 이벤트를 해제한다.
    /// </summary>
    private void UnhookTextChanged()
    {
        if (!_textHooked)
            return;

        _textDescriptor?.RemoveValueChanged(_textBlock, OnTextChanged);
        _textDescriptor = null;
        _textHooked = false;
    }

    /// <summary>
    /// 가시성 변경 이벤트를 구독한다.
    /// </summary>
    private void HookVisibilityChanged()
    {
        if (_visibilityHooked)
            return;

        _textBlock.IsVisibleChanged += OnIsVisibleChanged;
        _visibilityHooked = true;
    }

    /// <summary>
    /// 가시성 변경 이벤트를 해제한다.
    /// </summary>
    private void UnhookVisibilityChanged()
    {
        if (!_visibilityHooked)
            return;

        _textBlock.IsVisibleChanged -= OnIsVisibleChanged;
        _visibilityHooked = false;
    }

    /// <summary>
    /// 가시성 변경 시 애니메이션을 중지하거나 재개한다.
    /// </summary>
    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!_textBlock.IsVisible)
        {
            StopAnimation(true);
            return;
        }

        PrepareLayout();
        if (_shouldScroll)
        {
            UnhookSizeChanged();
            StartAnimation(forceRestart: true);
        }
        else
        {
            StopAnimation(true);
            HookSizeChanged();
        }
    }

    /// <summary>
    /// 텍스트 변경 시 레이아웃을 재계산한다.
    /// </summary>
    private void OnTextChanged(object? sender, EventArgs e)
    {
        var key = ResolveNoteKey();
        if (_useViewModel && _viewModel != null && !string.IsNullOrWhiteSpace(key))
        {
            _viewModel.UpdateNoteTextSnapshot(key, _textBlock.Text);
        }

        PrepareLayout();
        if (!_textBlock.IsVisible)
            return;

        if (_shouldScroll)
        {
            UnhookSizeChanged();
            StartAnimation(forceRestart: true);
        }
        else
        {
            StopAnimation(true);
            HookSizeChanged();
        }
    }

    /// <summary>
    /// 언로드 시 애니메이션을 중지한다.
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Stop();
    }

    /// <summary>
    /// 페이지 전환과 무관하게 노트 위치를 공유하는 키를 반환한다.
    /// </summary>
    private string? ResolveNoteKey()
    {
        if (!_useViewModel || _viewModel == null)
            return null;

        return SharedNoteKey;
    }
}
