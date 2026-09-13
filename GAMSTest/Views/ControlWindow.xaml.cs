using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using CowAuctionSmall.Models.XMLParser;
using GAMSTest.Services;
using GAMSTest.ViewModels;
namespace GAMSTest.Views;
public partial class ControlWindow : Window
{
    private const int SW_RESTORE = 9;
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
    private readonly DisplayWindow _display;
    private readonly NamedPipeCommandServer _commandServer;
    private bool _isClosing;
    public ControlWindow()
    {
        InitializeComponent(); Loaded += (_, _) => { Left = 10; Top = SystemParameters.WorkArea.Bottom - ActualHeight - 10; };
        var user = new UserXmlParser().ParseXml(System.IO.Path.Combine(AppContext.BaseDirectory, "Config", "users.XML"));
        _display = new DisplayWindow(user); DataContext = new ControlWindowViewModel(_display.Controller.ShowNumbers, _display.Controller.StartPageCycle, _display.Controller.ShowState, _display.Controller.Fill); _display.Show(); _display.Controller.ShowNumbers();
        _commandServer = new NamedPipeCommandServer(command =>
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_isClosing) return;

                if (string.Equals(command, "STOP", StringComparison.OrdinalIgnoreCase))
                {
                    Close();
                    return;
                }

                _display.Controller.HandleExternalCommand(command);
            })));
    }
    private void OpenDisplay(object sender, RoutedEventArgs e) { if (!_display.IsVisible) _display.Show(); _display.Activate(); }
    private void LaunchCowAuction(object sender, RoutedEventArgs e)
    {
        foreach (var process in Process.GetProcessesByName("CowAuctionSmall"))
        {
            if (process.MainWindowHandle == IntPtr.Zero) continue;
            ShowWindowAsync(process.MainWindowHandle, SW_RESTORE); SetForegroundWindow(process.MainWindowHandle); return;
        }
        var candidates = new[] { Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CowAuctionSmall.exe"), Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "CowAuctionSmall", "bin", "Debug", "net9.0-windows", "CowAuctionSmall.exe")) };
        var exePath = Array.Find(candidates, File.Exists);
        if (exePath != null) Process.Start(new ProcessStartInfo { FileName = exePath, WorkingDirectory = Path.GetDirectoryName(exePath)! });
    }
    protected override void OnClosed(EventArgs e)
    {
        if (_isClosing)
        {
            base.OnClosed(e);
            return;
        }

        _isClosing = true;
        _commandServer.Dispose();
        _display.Controller.ShowNumbers();
        if (_display.IsVisible)
        {
            _display.Close();
        }

        Application.Current.Shutdown();
        base.OnClosed(e);
    }
}
