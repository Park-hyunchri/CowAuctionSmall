using System.Windows;
using GAMSTest.Views;
using GAMSTest.Services;

namespace GAMSTest;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var settings = TestSettingsLoader.LoadStartup(AppContext.BaseDirectory, Environment.CurrentDirectory);
            var window = new ControlWindow(settings);
            MainWindow = window;
            window.Show();
        }
        catch (Exception error) when (error is System.Xml.XmlException or System.IO.IOException or UnauthorizedAccessException or FormatException or ArgumentException)
        {
            MessageBox.Show("전광판 설정을 읽을 수 없습니다. Config의 Board.XML과 users.XML 형식을 확인해 주세요.",
                "GAMS 테스트 제어", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
