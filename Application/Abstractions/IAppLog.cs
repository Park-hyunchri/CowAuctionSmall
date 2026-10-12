namespace CowAuctionSmall.Application.Abstractions
{
    public interface IAppLog
    {
        void LogInfo(string message);
        void LogWarn(string message);
        void LogError(string message);
    }
}
