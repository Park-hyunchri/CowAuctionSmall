using CowAuctionSmall.Models;
using CowAuctionSmall.NetProto.netty;
using CowAuctionSmall.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CowAuctionSmall
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Timer? _shutdownTimer;
        private Timer? _memoryCheckTimer;
        private int _shutdownRequested;
        private NLogger logger;
        public MainWindow()
        {
            logger = NLogger.Instance;
            IsLicense();
            CheckMemoryUsageTimer(); // 메모리 사용량 체크 타이머 추가
            SetupShutdownTimer(); // 다음 자정에 정상 종료
            
            InitializeComponent();
            Mouse.OverrideCursor = Cursors.None;
            this.DataContext = App.Current.Services.GetService<MainWindowViewModel>();

        }

        private void IsLicense()
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
                    ShowLicenseError("라이센스가 만료되었습니다. 관리자에게 문의하세요.");
                }
            }
            else
            {
                ShowLicenseError("라이센스가 존재하지 않습니다. 관리자에게 문의하세요.");
            }
        }

        private void ShowLicenseError(string message)
        {
            MessageBox.Show(message);
            Application.Current.Shutdown();
        }

        // 다음 자정에 프로그램이 정상 종료되도록 타이머를 설정한다.
        private void SetupShutdownTimer()
        {
            DateTime now = DateTime.Now;
            DateTime nextMidnight = now.Date.AddDays(1);
            double interval = Math.Max(1, (nextMidnight - now).TotalMilliseconds);

            _shutdownTimer = new Timer(interval)
            {
                AutoReset = false
            };
            _shutdownTimer.Elapsed += ShutdownTimer_Tick;
            _shutdownTimer.Start();
            logger.LogInfo($"자정 자동 종료 예약: {nextMidnight:yyyy-MM-dd HH:mm:ss}");
        }

        // 타이머 틱 이벤트 핸들러
        private void ShutdownTimer_Tick(object? sender, ElapsedEventArgs e)
        {
            _shutdownTimer?.Stop();
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => _ = ShutdownApplicationAsync("자정 자동 종료")));
        }

        // 메모리 사용량 체크 타이머 추가
        private void CheckMemoryUsageTimer()
        {
            _memoryCheckTimer = new Timer(10000); // 10초마다 체크
            _memoryCheckTimer.Elapsed += CheckMemoryUsageTimer_Tick;
            _memoryCheckTimer.Start();
        }

        // 메모리 사용량 체크 타이머 틱 이벤트 핸들러
        private void CheckMemoryUsageTimer_Tick(object? sender, ElapsedEventArgs e)
        {
            // 현재 프로세스의 메모리 사용량을 가져옴 (바이트 단위)
            long memoryUsage = Process.GetCurrentProcess().WorkingSet64;

            // 메모리 사용량이 6GB (2 * 1024 * 1024 * 1024 바이트)보다 크면 프로그램 재시작
            if (memoryUsage > 6L * 1024 * 1024 * 1024)
            {
                var fileName = Process.GetCurrentProcess().MainModule.FileName;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        logger.LogError("메모리 사용량 오버로 인한 재 시작 3GB 초과");
                        // 새 프로세스 시작
                        Process.Start(fileName);

                        // 타이머 정지
                        _memoryCheckTimer.Stop();

                        // 현재 애플리케이션 종료
                        logger.LogError("프로그램 종료 : 메모리 사용량 초과");
                        Application.Current.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        // 예외 처리 (필요 시 로그 작성 또는 사용자에게 알림)
                        MessageBox.Show($"프로그램 재시작 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
        }

        // 💡 윈도우 닫힐 때 타이머 리소스 해제 (메모리 누수 완전 차단)
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (_shutdownTimer != null)
            {
                _shutdownTimer.Stop();
                _shutdownTimer.Dispose();
                _shutdownTimer = null;
            }

            if (_memoryCheckTimer != null)
            {
                _memoryCheckTimer.Stop();
                _memoryCheckTimer.Dispose();
                _memoryCheckTimer = null;
            }
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                await ShutdownApplicationAsync("ESC 키 입력");
            }
        }

        private async Task ShutdownApplicationAsync(string reason)
        {
            if (System.Threading.Interlocked.Exchange(ref _shutdownRequested, 1) == 1)
            {
                return;
            }

            logger.LogInfo($"프로그램 종료 : {reason}");

            try
            {
                var nettyDisposeTask = AuctionDelegate.getInstance().disposeClients();
                if (await Task.WhenAny(nettyDisposeTask, Task.Delay(1500)) != nettyDisposeTask)
                {
                    logger.LogWarn($"{reason}: Netty 종료 제한시간 초과");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"{reason}: Netty 종료 실패 - {ex.Message}");
            }

            Close();
        }
        private void MsgTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 스크롤을 맨 아래로 이동
            MsgTextBox.ScrollToEnd();
        }
    }
}
