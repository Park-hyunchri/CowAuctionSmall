using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using GAMSTest.Models;
using GAMSTest.Services;
using static CowAuctionSmall.Presentation.Display.DisplaySizeParser;

namespace GAMSTest.ViewModels;

public sealed class ControlWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly DisplayTestController _controller;
    private readonly Action<string> _reportError;
    private int _brightness = 170;
    private Color _baseColor = Colors.Black;
    public event PropertyChangedEventHandler? PropertyChanged;
    public TestSettings Settings { get; private set; }
    public string BoardPathToolTip => Settings.BoardXmlPath ?? "기본 가상 배치 (Board.XML 미선택)";
    public sealed record ResolutionOption(DisplaySize Value, string Label);
    public ResolutionOption[] Resolutions { get; } =
    [
        new(DisplaySize.Size128x64, "128 × 64"), new(DisplaySize.Size128x128, "128 × 128"),
        new(DisplaySize.Size160x64, "160 × 64"), new(DisplaySize.Size320x64, "320 × 64")
    ];
    public DisplaySize SelectedResolution
    {
        get => Settings.Resolution;
        set
        {
            if (value == Settings.Resolution) return;
            Run(() =>
            {
                var settings = Settings.Snapshot();
                settings.Resolution = value;
                ApplyBoardSettings(settings);
            });
        }
    }
    public int Brightness
    {
        get => _brightness;
        set
        {
            _brightness = Math.Clamp(value, 0, 255);
            _controller.SetBrightness((byte)_brightness);
            _controller.Fill(_baseColor);
            Notify();
        }
    }
    public RelayCommand ShowBoardNumberCommand { get; }
    public RelayCommand ShowRun1Command { get; }
    public RelayCommand ShowRun2Command { get; }
    public RelayCommand ShowUnsoldCommand { get; }
    public RelayCommand ShowSoldCommand { get; }
    public RelayCommand StartCycleCommand { get; }
    public RelayCommand LaunchAuctionCommand { get; }
    public RelayCommand OpenBoardCommand { get; }
    public RelayCommand Red { get; }
    public RelayCommand Green { get; }
    public RelayCommand Blue { get; }
    public RelayCommand White { get; }
    public RelayCommand Black { get; }

    public ControlWindowViewModel(DisplayTestController controller, TestSettings settings, Action launchAuction, Action<string> reportError, Func<string?>? selectBoard = null)
    {
        _controller = controller;
        Settings = settings.Snapshot();
        _reportError = reportError;
        ShowBoardNumberCommand = new RelayCommand(() => Run(_controller.ShowNumbers));
        ShowRun1Command = ModeCommand("RUN1");
        ShowRun2Command = ModeCommand("RUN2");
        ShowUnsoldCommand = ModeCommand("UNSOLD");
        ShowSoldCommand = ModeCommand("SOLD");
        StartCycleCommand = new RelayCommand(() => Run(() => _controller.StartCycle(TimeSpan.FromSeconds(5))));
        LaunchAuctionCommand = new RelayCommand(() => Run(launchAuction));
        OpenBoardCommand = new RelayCommand(() => Run(() =>
        {
            string? path = selectBoard?.Invoke();
            if (string.IsNullOrWhiteSpace(path)) return;
            var settings = TestSettingsLoader.LoadBoard(path, Settings);
            ApplyBoardSettings(settings);
        }));
        Red = ColorCommand(Colors.Red); Green = ColorCommand(Colors.Green); Blue = ColorCommand(Colors.Blue);
        White = ColorCommand(Colors.White); Black = ColorCommand(Colors.Black);
        Apply();
        _controller.SetBrightness((byte)_brightness);
    }

    public void HandleExternalCommand(string command)
    {
        switch (command.Trim().ToUpperInvariant())
        {
            case "NUMBER": ShowBoardNumberCommand.Execute(null); break;
            case "RED": Red.Execute(null); break;
            case "GREEN": Green.Execute(null); break;
            case "BLUE": Blue.Execute(null); break;
        }
    }

    private void ApplyBoardSettings(TestSettings settings)
    {
        // Validate and apply before replacing the current settings. XML files remain unchanged.
        _controller.Configure(settings, SampleKind.Normal);
        Settings = settings;
        Notify(nameof(Settings));
        Notify(nameof(BoardPathToolTip));
        Notify(nameof(SelectedResolution));
        UpdateCommands();
    }

    private RelayCommand ModeCommand(string key) => new(
        () => Run(() => _controller.ShowMode(_controller.Modes.First(mode => mode.Key == key && mode.IsSupported))),
        () => _controller.Modes.Any(mode => mode.Key == key && mode.IsSupported));

    private RelayCommand ColorCommand(Color color) => new(() => Run(() => { _baseColor = color; _controller.Fill(color); }));

    private void Apply()
    {
        if (System.Windows.Application.Current is { } application)
        {
            foreach (string key in new[] { "EntityNumberForeground", "EntityNumberShortForeground", "LocationForeground", "EpdGradeForeground" })
                application.Resources.Remove(key);
            foreach (var (key, text) in Settings.Colors)
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(text));
                brush.Freeze();
                application.Resources[key] = brush;
            }
        }
        _controller.Configure(Settings, SampleKind.Normal);
        UpdateCommands();
    }

    private void UpdateCommands()
    {
        ShowRun1Command.NotifyCanExecuteChanged(); ShowRun2Command.NotifyCanExecuteChanged();
        ShowUnsoldCommand.NotifyCanExecuteChanged(); ShowSoldCommand.NotifyCanExecuteChanged();
    }

    private void Run(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.IO.IOException or UnauthorizedAccessException or FormatException or Win32Exception or System.Xml.XmlException)
        {
            if (ex is System.Xml.XmlException)
            {
                _reportError("Board.XML 형식이 올바르지 않습니다. 기존 화면을 유지합니다.");
                return;
            }
            _reportError(ex is System.IO.IOException or UnauthorizedAccessException or Win32Exception
                ? "파일을 열거나 프로그램을 실행할 수 없습니다. 경로와 권한을 확인해 주세요." : ex.Message);
        }
    }
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public void Dispose() => _controller.Dispose();
}
