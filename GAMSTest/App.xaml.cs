using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using CowAuctionSmall.Models.XMLParser;

namespace GAMSTest;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) => { MessageBox.Show($"UI 예외 발생:\n{args.Exception.Message}\n\n위치:\n{args.Exception.StackTrace}", "오류 방어", MessageBoxButton.OK, MessageBoxImage.Error); args.Handled = true; };
        var userInfo = new UserXmlParser().ParseXml(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "users.XML"));
        if (userInfo?.Auction == null) return;
        Brush ConvertBrush(string? value, Brush fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value)); }
            catch { return fallback; }
        }
        Resources["EntityNumberForeground"] = ConvertBrush(userInfo.Auction.EntityNumberForeground, Brushes.Silver);
        Resources["EntityNumberShortForeground"] = ConvertBrush(userInfo.Auction.EntityNumberShortForeground, Brushes.Red);
        Resources["EntityNumberShortBackground"] = ConvertBrush(userInfo.Auction.EntityNumberShortBackground, Brushes.Black);
        Resources["LocationForeground"] = ConvertBrush(userInfo.Auction.LocationForeground, Brushes.Magenta);
        Resources["EpdGradeForeground"] = ConvertBrush(userInfo.Auction.EpdGradeForeground, Brushes.Silver);
    }
}
