using CowAuctionSmall.ViewModels;
using Microsoft.Extensions.DependencyInjection;
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

namespace CowAuctionSmall
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer _shutdownTimer;
        public MainWindow()
        {
            InitializeComponent();
            Mouse.OverrideCursor = Cursors.None;
            this.DataContext = App.Current.Services.GetService<MainWindowViewModel>();
            SetupShutdownTimer();
        }
        private void SetupShutdownTimer()
        {
            _shutdownTimer = new DispatcherTimer();
            _shutdownTimer.Tick += ShutdownTimer_Tick;
            _shutdownTimer.Interval = TimeSpan.FromMinutes(1); // 1분마다 체크
            _shutdownTimer.Start();
        }

        private void ShutdownTimer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now.Hour == 23 && DateTime.Now.Minute == 30)
            {
                Application.Current.Shutdown();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
