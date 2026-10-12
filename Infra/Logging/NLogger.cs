using CowAuctionSmall.Application.Abstractions;
using NLog;
using System;
using System.IO;

namespace CowAuctionSmall.Infra.Logging
{
    public sealed class NLogger : IAppLog
    {
        // NLog 인스턴스 생성
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Lazy<NLogger> lazy = new Lazy<NLogger>(() => new NLogger());

        // NLogger 싱글톤 인스턴스
        public static NLogger Instance { get { return lazy.Value; } }

        // Private constructor to prevent instantiation
        private NLogger()
        {
        }

        // 로그 메시지 기록 메서드
        public void LogInfo(string message)
        {
            Logger.Info(message);
        }

        public void LogWarn(string message)
        {
            Logger.Warn(message);
        }

        public void LogError(string message)
        {
            Logger.Error(message);
        }
    }
}
