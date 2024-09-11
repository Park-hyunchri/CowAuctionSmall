using CowAuctionSmall.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
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
            IsLisence();
            InitializeComponent();
            Mouse.OverrideCursor = Cursors.None;
            this.DataContext = App.Current.Services.GetService<MainWindowViewModel>();
            SetupShutdownTimer();
        }

        private void IsLisence()
        {
            string path = @"C:\Windows\SysWOW64\windowsbootadmin64.dll";
            if (File.Exists(path))
            {
                string LicenseKey = System.IO.File.ReadAllText(path);
                if (LicenseKey == "200909EstablishedCONSTANTECCompany")
                {

                }
                else
                {
                    MessageBox.Show("라이센스가 만료되었습니다. 관리자에게 문의하세요.");
                    Application.Current.Shutdown();
                }
            }
            else
            {
                MessageBox.Show("라이센스가 존재하지 않습니다. 관리자에게 문의하세요.");
                Application.Current.Shutdown();
            }
        }
        private void SetupShutdownTimer()
        {
            _shutdownTimer = new DispatcherTimer();
            _shutdownTimer.Tick += ShutdownTimer_Tick;
            _shutdownTimer.Interval = TimeSpan.FromMinutes(5); // 5분마다 체크로 변경하여 CPU 사용량 감소
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // 타이머 해제하여 메모리 누수 방지
            if (_shutdownTimer != null)
            {
                _shutdownTimer.Stop();
                _shutdownTimer.Tick -= ShutdownTimer_Tick;
                _shutdownTimer = null;
            }
        }
    }
}
