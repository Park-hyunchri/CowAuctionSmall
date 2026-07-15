using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.Models.XMLParser;
using CowAuctionSmall.NetProto.netty;
using CowAuctionSmall.Services;
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
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public App()
        {
            // NLog 초기화
            InitializeLogging();

            // 전역 예외 핸들러 등록
            RegisterGlobalExceptionHandlers();

            Services = ConfigureServices();

            this.InitializeComponent();
            ApplyDisplayColors();
        }
        private static void InitializeLogging()
        {
            // NLog 설정 파일 로드
            string configFile = "NLog.config";

            // 구식 생성자를 대체하여 NLog 설정 로드
            LogManager.ThrowConfigExceptions = true; // 오류 발생 시 예외를 던지도록 설정 (필요에 따라 true/false로 설정)
            var xmlLoggingConfiguration = new NLog.Config.XmlLoggingConfiguration(configFile);

            // NLog의 현재 설정으로 적용
            LogManager.Configuration = xmlLoggingConfiguration;
        }


        public new static App Current => (App)Application.Current;

        public IServiceProvider Services { get; }
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            //ViewModel
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<AuctionContPanelViewModel>();

            //Model 나중에 인터페이스로 변경 예정
            services.AddSingleton<BoardXmlParser>();
            services.AddSingleton<DisplayColorXmlParser>();
            services.AddSingleton<UserXmlParser>();
            services.AddSingleton<XmlParserCont>();
            services.AddSingleton<NettyAsyncMsgProcess>();
            services.AddSingleton<DisplayColorResourceService>();

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
                return new ServerConn(http); // ServerConn(HttpClient http) 생성자 필요
            });
            services.AddSingleton<ServerGetData>();

            


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
                var displayColorsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "DisplayColors.XML");
                Services.GetRequiredService<DisplayColorResourceService>().ApplyFromXml(displayColorsPath, Resources);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "DisplayColors 설정 적용 실패. 기본 색상을 사용합니다.");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            RenderOptions.ProcessRenderMode = RenderMode.Default;
            var tier = RenderCapability.Tier >> 16;

            base.OnStartup(e);
            Logger.Info($"Application started. Render tier: {tier}");
        }
        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("Application exiting. Start cleanup.");

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

            Logger.Info("Application exited.");
            base.OnExit(e);
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

