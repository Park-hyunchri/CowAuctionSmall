using System;
using System.Windows;
using CowAuctionSmall.Models.XMLParser;
using GAMSTest.ViewModels;
namespace GAMSTest.Views;
public partial class ControlWindow : Window
{
    private readonly DisplayWindow _display;
    public ControlWindow()
    {
        InitializeComponent(); Loaded += (_, _) => { Left = 10; Top = SystemParameters.WorkArea.Bottom - ActualHeight - 10; };
        var user = new UserXmlParser().ParseXml(System.IO.Path.Combine(AppContext.BaseDirectory, "Config", "users.XML"));
        _display = new DisplayWindow(user); DataContext = new ControlWindowViewModel(_display.Controller.ShowNumbers, _display.Controller.StartPageCycle, _display.Controller.ShowState, _display.Controller.Fill); _display.Show(); _display.Controller.ShowNumbers();
    }
    private void OpenDisplay(object sender, RoutedEventArgs e) { if (!_display.IsVisible) _display.Show(); _display.Activate(); }
}
