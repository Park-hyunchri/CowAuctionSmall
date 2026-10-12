using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views.Size128_128.Running;
using GAMSTest.Models;
using GAMSTest.Views;

namespace GAMSTest.Services;

public sealed class DisplayTestController : IDisposable
{
    private sealed class Slot(int number, Border host)
    {
        public int Number { get; } = number;
        public Border Host { get; } = host;
        public Dictionary<string, FrameworkElement> Views { get; } = new();
        public AuctionContPanelViewModel? ViewModel { get; set; }
        public RunningNoteHost128? RunningNoteHost { get; set; }
    }

    private readonly Canvas _canvas;
    private readonly DispatcherTimer _timer;
    private readonly AuctionViewFactory _factory = new();
    private readonly List<Slot> _slots = new();
    private TestSettings _settings = new();
    private SampleKind _sample;
    private Color? _baseColor;
    private byte _brightness = 255;
    private int _cycleIndex;
    private string? _currentModeKey;
    private DisplayMode[] _cycleModes = [];
    public event EventHandler? StateChanged;
    public IReadOnlyList<DisplayMode> Modes { get; private set; } = Array.Empty<DisplayMode>();
    public int SlotCount => _slots.Count;
    public int CachedViewCount => _slots.Sum(slot => slot.Views.Count);
    public bool IsCycling => _timer.IsEnabled;
    public bool IsDisposed { get; private set; }
    public string CurrentMode { get; private set; } = "번호";
    public double LastPreparationMs { get; private set; }
    public double LastTransitionMs { get; private set; }

    public DisplayTestController(Canvas canvas)
    {
        _canvas = canvas;
        _timer = new DispatcherTimer(DispatcherPriority.Background, canvas.Dispatcher);
        _timer.Tick += OnTick;
    }

    public void Configure(TestSettings settings, SampleKind sample)
    {
        VerifyAccess();
        var (width, height) = TestSettings.GetDimensions(settings.Resolution);
        if (settings.Rows.Count == 0 || settings.Rows.Any(row => row.Count == 0))
            throw new ArgumentException("비어 있는 전광판 배치는 사용할 수 없습니다.");
        if (settings.Rows.Sum(row => row.Count) > 400)
            throw new ArgumentException("한 번에 최대 400개 슬롯을 점검할 수 있습니다.");
        Stop();
        ClearSlots();
        _settings = settings.Snapshot();
        _sample = sample;
        _canvas.Width = settings.Rows.Max(row => row.Count) * width;
        _canvas.Height = settings.Rows.Count * height;
        for (var row = 0; row < settings.Rows.Count; row++)
        for (var column = 0; column < settings.Rows[row].Count; column++)
        {
            var host = new Border { Width = width, Height = height, Background = Brushes.Black, ClipToBounds = true };
            Canvas.SetLeft(host, column * width);
            Canvas.SetTop(host, row * height);
            _canvas.Children.Add(host);
            _slots.Add(new Slot(settings.Rows[row][column], host));
        }
        var options = settings.ToViewOptions();
        var modes = new List<DisplayMode>();
        foreach (var number in AuctionViewFactory.GetRunningPageNumbers(options))
        {
            var view = _factory.CreateRunning(options, number);
            if (view != null)
            {
                var supported = !AuctionViewFactory.IsPlaceholder(view);
                modes.Add(new DisplayMode($"RUN{number}", $"진행 {number}페이지" + (supported ? "" : " (미구현)"), number, supported));
            }
        }
        if (modes.FirstOrDefault(mode => mode.IsSupported) is { } first)
            modes.Insert(0, new DisplayMode("WAIT", "경매 대기", first.Page));
        modes.Add(new DisplayMode("SOLD", "낙찰" + (AuctionViewFactory.IsPlaceholder(_factory.CreateSold(options)) ? " (미구현)" : ""), 0,
            !AuctionViewFactory.IsPlaceholder(_factory.CreateSold(options))));
        modes.Add(new DisplayMode("UNSOLD", "유찰" + (AuctionViewFactory.IsPlaceholder(_factory.CreateUnSold(options)) ? " (미구현)" : ""), 0,
            !AuctionViewFactory.IsPlaceholder(_factory.CreateUnSold(options))));
        Modes = modes;
        ShowNumbers();
    }

    public void ShowNumbers()
    {
        VerifyAccess(); Stop(); _baseColor = null;
        _currentModeKey = null;
        foreach (var slot in _slots)
        {
            Detach(slot);
            slot.Host.Background = Brushes.Black;
            slot.Host.Child = new BoardNumberView(slot.Number, slot.Host.Width, slot.Host.Height);
        }
        CurrentMode = "계류대 번호";
        Changed();
    }

    public void Fill(Color color)
    {
        VerifyAccess(); Stop(); _baseColor = color;
        ApplyColor();
    }

    public void SetBrightness(byte brightness)
    {
        VerifyAccess(); _brightness = brightness;
        if (_baseColor.HasValue) ApplyColor();
    }

    private void ApplyColor()
    {
        _currentModeKey = null;
        var color = _baseColor!.Value;
        var brush = new SolidColorBrush(Color.FromRgb((byte)(color.R * _brightness / 255), (byte)(color.G * _brightness / 255), (byte)(color.B * _brightness / 255)));
        brush.Freeze();
        foreach (var slot in _slots) { Detach(slot); slot.Host.Background = brush; }
        CurrentMode = $"단색 출력 · 강도 {_brightness}/255";
        Changed();
    }

    public void ShowMode(DisplayMode mode)
    {
        VerifyAccess(); Stop();
        ShowModeCore(mode);
    }

    private void ShowModeCore(DisplayMode mode, bool animateRunning = false)
    {
        if (!Modes.Contains(mode) || !mode.IsSupported) throw new ArgumentException("현재 설정에서 지원하지 않는 화면입니다.");
        bool isRunning = animateRunning && mode.Page > 0 && mode.Key != "WAIT";
        if (_currentModeKey == mode.Key)
        {
            foreach (var slot in _slots)
                if (slot.ViewModel != null) slot.ViewModel.IsRunning = isRunning;
            LastPreparationMs = 0;
            LastTransitionMs = 0;
            return;
        }
        _baseColor = null;
        var preparation = Stopwatch.StartNew();
        var prepared = _slots.Select(slot => GetOrCreate(slot, mode)).ToArray();
        LastPreparationMs = preparation.Elapsed.TotalMilliseconds;
        var transition = Stopwatch.StartNew();
        var options = _settings.ToViewOptions();
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            var view = prepared[i];
            bool useRunningHost = mode.Page > 0 && AuctionViewFactory.ShouldUseNoteHost(options, (UserControl)view);
            FrameworkElement content = view;
            if (useRunningHost)
            {
                var host = slot.RunningNoteHost ??= new RunningNoteHost128
                {
                    Width = slot.Host.Width, Height = slot.Host.Height,
                    ItemDisplay = slot.ViewModel!.ItemDisplay, ScrollOwner = slot.ViewModel
                };
                // Keep the note in the visual tree; only replace the page behind it.
                if (ReferenceEquals(slot.Host.Child, host))
                {
                    if (host.PageContent is FrameworkElement previous) previous.DataContext = null;
                }
                else
                {
                    Detach(slot);
                }
                slot.ViewModel!.IsRunning = isRunning;
                view.DataContext = slot.ViewModel;
                host.PageContent = view;
                content = host;
            }
            else
            {
                Detach(slot);
                slot.ViewModel!.IsRunning = isRunning;
                var raw = view is RunningNoteHost128 host ? (FrameworkElement)host.PageContent! : view;
                raw.DataContext = mode.Page > 0 ? slot.ViewModel : slot.ViewModel.ItemDisplay;
            }
            slot.Host.Background = Brushes.Black;
            if (!ReferenceEquals(slot.Host.Child, content)) slot.Host.Child = content;
        }
        _currentModeKey = mode.Key;
        LastTransitionMs = transition.Elapsed.TotalMilliseconds;
        CurrentMode = mode.Label;
        Changed();
    }

    private FrameworkElement GetOrCreate(Slot slot, DisplayMode mode)
    {
        if (slot.Views.TryGetValue(mode.Key, out var cached)) return cached;
        var options = _settings.ToViewOptions();
        var sample = MockDataFactory.Create(slot.Number, _settings, _sample);
        var isRunning = mode.Page > 0;
        var view = isRunning ? _factory.CreateRunning(options, mode.Page)! : mode.Key == "SOLD" ? _factory.CreateSold(options) : _factory.CreateUnSold(options);
        slot.ViewModel ??= new AuctionContPanelViewModel(sample, new VirtualizingStackPanel(), Modes.Count(mode => mode.Page > 0));
        // 활성 화면에 부착할 때만 DataContext를 연결하여 숨긴 진행 화면의 트리거를 멈춘다.
        view.Width = slot.Host.Width;
        view.Height = slot.Host.Height;
        FrameworkElement content = view;
        if (!isRunning && AuctionViewFactory.ShouldUseNoteHost(options))
        {
            content = new RunningNoteHost128
            {
                Width = slot.Host.Width, Height = slot.Host.Height, PageContent = view,
                ItemDisplay = slot.ViewModel.ItemDisplay
            };
        }
        content.Width = slot.Host.Width;
        content.Height = slot.Host.Height;
        slot.Views.Add(mode.Key, content);
        return content;
    }

    public void PrepareAll()
    {
        VerifyAccess();
        var watch = Stopwatch.StartNew();
        foreach (var mode in Modes.Where(mode => mode.IsSupported))
        foreach (var slot in _slots) GetOrCreate(slot, mode);
        LastPreparationMs = watch.Elapsed.TotalMilliseconds;
        Changed();
    }

    public void StartCycle(TimeSpan interval)
    {
        VerifyAccess();
        if (interval < TimeSpan.FromSeconds(1) || interval > TimeSpan.FromMinutes(10))
            throw new ArgumentException("순환 간격은 1~600초로 입력하세요.");
        Stop();
        var supported = Modes.Where(mode => mode.IsSupported && mode.Key != "WAIT" && (mode.Page == 0 || mode.Page <= _settings.PageCount))
            .OrderBy(mode => mode.Key == "UNSOLD" ? 100 : mode.Key == "SOLD" ? 101 : mode.Page).ToArray();
        if (supported.Length == 0) throw new InvalidOperationException("순환 가능한 경매 화면이 없습니다.");
        PrepareAll();
        _cycleIndex = 0;
        _cycleModes = supported;
        ShowModeCore(supported[0], animateRunning: true);
        _timer.Interval = interval;
        _timer.Start();
        Changed();
    }

    private void OnTick(object? sender, EventArgs args)
    {
        var supported = _cycleModes;
        if (supported.Length == 0) { Stop(); return; }
        _cycleIndex = (_cycleIndex + 1) % supported.Length;
        ShowModeCore(supported[_cycleIndex], animateRunning: true);
    }

    public void Stop()
    {
        _canvas.Dispatcher.VerifyAccess();
        _timer.Stop();
        Changed();
    }

    private static void Detach(Slot slot)
    {
        if (slot.Host.Child is FrameworkElement current)
        {
            var raw = current is RunningNoteHost128 host ? host.PageContent as FrameworkElement : current;
            if (raw != null) raw.DataContext = null;
        }
        slot.Host.Child = null;
        if (slot.RunningNoteHost != null) slot.RunningNoteHost.PageContent = null;
        if (slot.ViewModel != null) slot.ViewModel.IsRunning = false;
    }

    private void ClearSlots()
    {
        _currentModeKey = null;
        foreach (var slot in _slots)
        {
            Detach(slot);
            foreach (var host in slot.Views.Values.OfType<RunningNoteHost128>())
            {
                host.PageContent = null; host.ScrollOwner = null; host.ItemDisplay = null;
            }
            if (slot.RunningNoteHost is { } runningHost)
            {
                runningHost.PageContent = null; runningHost.ScrollOwner = null; runningHost.ItemDisplay = null;
                slot.RunningNoteHost = null;
            }
            slot.Views.Clear();
            slot.ViewModel?.Dispose();
        }
        _canvas.Children.Clear();
        _slots.Clear();
    }

    private void VerifyAccess()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _canvas.Dispatcher.VerifyAccess();
    }
    private void Changed() => StateChanged?.Invoke(this, EventArgs.Empty);
    public void Dispose()
    {
        if (IsDisposed) return;
        Stop();
        _timer.Tick -= OnTick;
        ClearSlots();
        IsDisposed = true;
        StateChanged = null;
    }
}
