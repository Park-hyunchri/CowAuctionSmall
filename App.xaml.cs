using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.Models.XMLParser;
using CowAuctionSmall.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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

            Services = ConfigureServices();

            this.InitializeComponent();
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
            services.AddSingleton<AuctionContPanelViewModel>();

            //Model 나중에 인터페이스로 변경 예정
            services.AddSingleton<BoardXmlParser>();
            services.AddSingleton<UserXmlParser>();
            services.AddSingleton<XmlParserCont>();
            services.AddSingleton<NettyAsyncMsgProcess>(); 

            services.AddSingleton<ServerConn>();
            services.AddSingleton<ServerGetData>();

            


            //services.AddSingleton<IMessenger, Messenger>();

            /*            services.AddSingleton<IFilesService, FilesService>();
                        services.AddSingleton<ISettingsService, SettingsService>();
                        services.AddSingleton<IClipboardService, ClipboardService>();
                        services.AddSingleton<IShareService, ShareService>();
                        services.AddSingleton<IEmailService, EmailService>();*/

            return services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Logger.Info("Application started.");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("Application exited.");
            base.OnExit(e);
        }
    }
    
}
