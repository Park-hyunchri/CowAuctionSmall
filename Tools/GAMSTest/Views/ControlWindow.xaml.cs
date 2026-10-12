using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;
using GAMSTest.Models;
using GAMSTest.Services;
using GAMSTest.ViewModels;

namespace GAMSTest.Views;

public partial class ControlWindow : Window
{
    private readonly DisplayWindow _display;
    private readonly ControlWindowViewModel _viewModel;
    private readonly NamedPipeCommandServer _commandServer;
    private bool _closing;

    public ControlWindow() : this(new TestSettings()) { }

    public ControlWindow(TestSettings settings, Action? launchAuction = null, string commandPipeName = NamedPipeCommandServer.PipeName)
    {
        InitializeComponent();
        _display = new DisplayWindow { Left = settings.DisplayLeft, Top = settings.DisplayTop };
        var controller = new DisplayTestController(_display.Surface);
        _viewModel = new ControlWindowViewModel(controller, settings, launchAuction ?? LaunchAuction,
            message => MessageBox.Show(this, message, "GAMS 테스트 제어", MessageBoxButton.OK, MessageBoxImage.Information), SelectBoard);
        DataContext = _viewModel;
        _commandServer = new NamedPipeCommandServer(command => Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_closing) return;
            if (string.Equals(command, "STOP", StringComparison.OrdinalIgnoreCase)) Close();
            else _viewModel.HandleExternalCommand(command);
        })), commandPipeName);
        _display.Closing += OnDisplayClosing;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Left = SystemParameters.WorkArea.Left + 10;
        Top = SystemParameters.WorkArea.Bottom - ActualHeight - 10;
        _display.Show();
        Activate();
    }

    private string? SelectBoard()
    {
        var picker = new OpenFileDialog
        {
            Filter = "전광판 배치 XML (*.xml)|*.xml",
            Title = "Board.XML 열기",
            CheckFileExists = true,
            FileName = _viewModel.Settings.BoardXmlPath ?? "Board.XML"
        };
        return picker.ShowDialog(this) == true ? picker.FileName : null;
    }

    private void LaunchAuction()
    {
        if (AuctionApplicationLauncher.ActivateExisting()) return;
        string? path = AuctionApplicationLauncher.FindExecutable(AppContext.BaseDirectory, Environment.CurrentDirectory);
        if (path == null)
        {
            var picker = new OpenFileDialog { Filter = "출하 AMS (CowAuctionSmall.exe)|CowAuctionSmall.exe", Title = "출하 AMS 실행 파일 선택" };
            if (picker.ShowDialog(this) != true) return;
            path = picker.FileName;
        }
        AuctionApplicationLauncher.Launch(path);
    }

    private void OnDisplayClosing(object? sender, CancelEventArgs e)
    {
        if (_closing) return;
        e.Cancel = true;
        Dispatcher.BeginInvoke(new Action(Close));
    }
    private void OnClosed(object? sender, EventArgs e)
    {
        _closing = true;
        _commandServer.Dispose();
        Loaded -= OnLoaded;
        _display.Closing -= OnDisplayClosing;
        _viewModel.Dispose();
        _display.Close();
    }
}
