using System.Diagnostics;
using CowAuctionSmall.Infra.Logging;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Infra.Netty.netty;
using CowAuctionSmall.Infra.XmlSettings;
using CowAuctionSmall.Infra.Api;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Infra.Persistence;
using CowAuctionSmall.Application.AnimalHistory;
using CowAuctionSmall.Domain.Settings;
using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;

namespace CowAuctionSmall
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static Logger Logger = null!;
        private static LoggingLifetime? _loggingLifetime;
        private static LogArchiveService? _logArchive;

        public App()
        {
            // NLog 초기화
            InitializeLogging();

            // 전역 예외 핸들러 등록
            RegisterGlobalExceptionHandlers();

            Services = ConfigureServices();
            WeakReferenceMessenger.Default.Register<ApplicationRestartRequestedMessage>(this, (_, _) => RestartApplication());

            this.InitializeComponent();
            ApplyDisplayColors();
        }
        private static void InitializeLogging()
        {
            AsyncDebugOutput.Initialize();
            _loggingLifetime = new LoggingLifetime(LogManager.LogFactory, message =>
            {
                AsyncDebugOutput.WriteLine(message);
                WeakReferenceMessenger.Default.Send(new DataStringMessage(message));
            });
            // NLog 설정 파일 로드
            string configFile = "NLog.config";

            // 구식 생성자를 대체하여 NLog 설정 로드
            LogManager.ThrowConfigExceptions = true; // 오류 발생 시 예외를 던지도록 설정 (필요에 따라 true/false로 설정)
            var xmlLoggingConfiguration = new NLog.Config.XmlLoggingConfiguration(configFile);

            // NLog의 현재 설정으로 적용
            LogManager.Configuration = xmlLoggingConfiguration;
            Logger = LogManager.GetCurrentClassLogger();
            _logArchive = new LogArchiveService(Path.Combine(AppContext.BaseDirectory, "Logs"), result =>
            {
                string message = $"[로그 압축] 완료={result.Archived}, 만료삭제={result.Deleted}, 유지={result.Skipped}, 재시도={result.Failed}";
                if (result.Failed > 0) Logger.Warn(message);
                else Logger.Info(message);
            });
            _logArchive.Start();
        }


        public new static App Current => (App)System.Windows.Application.Current;

        public IServiceProvider Services { get; }
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            //ViewModel
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<AuctionContPanelViewModel>();

            //Model 나중에 인터페이스로 변경 예정
            services.AddSingleton<BoardXmlParser>();
            services.AddSingleton<UserXmlParser>();
            services.AddSingleton<XmlParserCont>();
            services.AddSingleton<NettyAsyncMsgProcess>();

            // services.AddSingleton<ServerConn>();
            services.AddSingleton<ServerConn>(sp =>
            {
                var handler = new SocketsHttpHandler
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.All,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                    MaxConnectionsPerServer = 50,
                    UseCookies = false
                };
                var http = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };
                return new ServerConn(http, sp.GetRequiredService<DailyCacheService>()); // ServerConn(HttpClient http) 생성자 필요
            });
            services.AddSingleton<IAppLog>(_ => NLogger.Instance);
            services.AddSingleton<IAuctionDataSource, ServerAuctionDataSource>();
            services.AddSingleton<IAuctionSessionRuntime, AuctionSessionRuntime>();
            services.AddSingleton<IAuctionPacketParser, AnimalParseData>();
            services.AddSingleton<AuctionSnapshotParser>();
            services.AddSingleton<ServerGetData>();
            services.AddSingleton<DailyCacheService>();
            services.AddSingleton<IDailyCacheStore>(sp => sp.GetRequiredService<DailyCacheService>());
            services.AddSingleton<IAnimalHistoryClient>(_ => new AnimalHistoryClient());
            services.AddSingleton<AnimalHistoryService>();

            


            //services.AddSingleton<IMessenger, Messenger>();

            /*            services.AddSingleton<IFilesService, FilesService>();
                        services.AddSingleton<ISettingsService, SettingsService>();
                        services.AddSingleton<IClipboardService, ClipboardService>();
                        services.AddSingleton<IShareService, ShareService>();
                        services.AddSingleton<IEmailService, EmailService>();*/

            return services.BuildServiceProvider();
        }

        private void ApplyDisplayColors()
        {
            try
            {
                // 1. users.XML 정보 가져오기
                var xmlParser = Services.GetRequiredService<XmlParserCont>();
                var (_, userInfo) = xmlParser.XmlPaserResult();

                if (userInfo?.Auction != null)
                {
                    // 2. users.XML 설정값으로 전역 리소스 브러시 적용
                    ApplyBrushResource("EntityNumberForeground", userInfo.Auction.EntityNumberForeground ?? "Yellow");
                    ApplyBrushResource("EntityNumberShortForeground", userInfo.Auction.EntityNumberShortForeground ?? "White");
                    ApplyBrushResource("EntityNumberShortBackground", userInfo.Auction.EntityNumberShortBackground ?? "Black");

                    // 💡 [신규 추가] 출하자 지역 및 육종 등급 색상 리소스 등록
                    ApplyBrushResource("LocationForeground", userInfo.Auction.LocationForeground ?? "lime");
                    ApplyBrushResource("EpdGradeForeground", userInfo.Auction.EpdGradeForeground ?? "Yellow");

                    Logger.Info("users.XML 기반 전광판 색상 적용 완료");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "users.XML 기반 색상 설정 적용 실패. 기본 색상을 사용합니다.");
            }
        }

        // 💡 안전하게 SolidColorBrush 생성 및 Freeze 후 Resources에 대입하는 헬퍼 메서드
        private void ApplyBrushResource(string resourceKey, string colorValue)
        {
            try
            {
                var converted = ColorConverter.ConvertFromString(colorValue.Trim());
                if (converted is Color color)
                {
                    var brush = new SolidColorBrush(color);
                    brush.Freeze(); // 성능 최적화
                    Resources[resourceKey] = brush;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"유효하지 않은 색상입니다. key={resourceKey}, value={colorValue}, err={ex.Message}");
            }
        }
        private void RestartApplication()
        {
            string fileName = Process.GetCurrentProcess().MainModule!.FileName;
            _ = Task.Run(() =>
            {
                Process.Start(fileName);
                Dispatcher.Invoke(Shutdown);
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("Application exiting. Start cleanup.");
            WeakReferenceMessenger.Default.Unregister<ApplicationRestartRequestedMessage>(this);

            Services.GetService<NettyAsyncMsgProcess>()?.Dispose();

            try
            {
                var mainWindowViewModel = Services.GetService<MainWindowViewModel>();
                if (mainWindowViewModel != null)
                {
                    mainWindowViewModel.Dispose();
                    Logger.Info("OnExit: MainWindowViewModel disposed.");
                }
                else
                {
                    Logger.Warn("OnExit: MainWindowViewModel service not found.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "OnExit: MainWindowViewModel dispose failed.");
            }

            try
            {
                var serverGetData = Services.GetService<ServerGetData>();
                if (serverGetData != null)
                {
                    serverGetData.Dispose();
                    Logger.Info("OnExit: ServerGetData disposed.");
                }
                else
                {
                    Logger.Warn("OnExit: ServerGetData service not found.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "OnExit: ServerGetData dispose failed.");
            }

            try
            {
                var nettyDisposeTask = Task.Run(() => AuctionDelegate.getInstance().disposeClients());
                if (nettyDisposeTask.Wait(TimeSpan.FromSeconds(3)))
                {
                    Logger.Info("OnExit: Netty client disposed.");
                }
                else
                {
                    Logger.Warn("OnExit: Netty client dispose timeout (3s).");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "OnExit: Netty client dispose failed.");
            }

            try
            {
                Services.GetService<AnimalHistoryService>()?.Dispose();
                Services.GetService<DailyCacheService>()?.Dispose();
            }
            finally
            {
                if (_logArchive?.Stop(TimeSpan.FromSeconds(1)) == false)
                    Logger.Warn("[로그 압축] 종료 대기 시간 초과: 미완료 로그는 다음 실행에서 재시도합니다.");
                Logger.Info("Application exited.");
                long loggingStopStarted = Stopwatch.GetTimestamp();
                var loggingBudget = TimeSpan.FromSeconds(2);
                var fileResult = _loggingLifetime?.Stop(loggingBudget);
                if (fileResult != null && (!fileResult.Completed || !fileResult.Flushed || fileResult.Statistics.Pending > 0))
                    AsyncDebugOutput.WriteLine($"[파일 로그 종료] 완료={fileResult.Completed}, flush={fileResult.Flushed}, 미완료={fileResult.Statistics.Pending}");
                var remaining = loggingBudget - Stopwatch.GetElapsedTime(loggingStopStarted);
                AsyncDebugOutput.Stop(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
                base.OnExit(e);
            }
        }
        /// <summary>
        /// 전역 예외 핸들러를 등록합니다.
        /// - UI 스레드 예외: DispatcherUnhandledException
        /// - 백그라운드 스레드 예외: UnhandledException
        /// - 비동기 Task 예외: UnobservedTaskException
        /// </summary>
        private void RegisterGlobalExceptionHandlers()
        {
            // UI 스레드 예외 처리
            this.DispatcherUnhandledException += (sender, e) =>
            {
                Logger.Error(e.Exception, "⚠ UI 스레드에서 예외 발생");
                //MessageBox.Show("예기치 못한 오류가 발생했습니다.\n\n" + e.Exception.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true; // 앱을 계속 실행할 수 있도록 함 (또는 false로 설정하면 종료됨)
                //Environment.Exit(1); // c (필요 시 제거 가능)
            };

            // 백그라운드 스레드 예외 처리
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Logger.Fatal(ex, "❌ 도메인 레벨 예외 발생");
                }
                else
                {
                    Logger.Fatal("❌ 도메인 예외 발생 (Exception 객체 아님)");
                }
            };

            // 비동기 Task 예외 처리 (await 누락 등)
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Logger.Error(e.Exception, "⚠ 비관찰 Task 예외 발생");
                e.SetObserved(); // 예외가 '처리됨'으로 표시됨 (프로세스 크래시 방지)
            };
        }

    }

}

