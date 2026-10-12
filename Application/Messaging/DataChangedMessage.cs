using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Application.Auction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 메신저로 사용할 클래스 정의 
/// 비동기로 뭔가 넘겨주고 싶을 때 사용
/// </summary>
namespace CowAuctionSmall.Application.Messaging
{
    public class DataChangedMessage
    {
        public IReadOnlyList<gValues> Data { get; }
        public bool IsFullSnapshot { get; }
        public BoardSourceStatistics? SourceStatistics { get; }

        public DataChangedMessage(IEnumerable<gValues> data, bool isFullSnapshot = false, BoardSourceStatistics? sourceStatistics = null)
        {
            var rows = data.ToArray();
            Data = Array.AsReadOnly(rows.Where(item => item != null).Select(item => item.Clone()).ToArray());
            IsFullSnapshot = isFullSnapshot;
            SourceStatistics = sourceStatistics ?? (isFullSnapshot ? BoardSourceStatistics.From(rows) : null);
        }
    }
    public class DataResponseConnectionInfoMessage
    {
        public string ResultCode { get; }

        public DataResponseConnectionInfoMessage(string resultCode)
        {
            ResultCode = resultCode;
        }
    }

    public class DataStringArrMessage
    {
        public string[] Data { get; }

        public DataStringArrMessage(string[] data)
        {
            Data = (string[])data.Clone();
        }
    }

    public class DataStringMessage
    {
        public string Data { get; }

        public DataStringMessage(string data)
        {
            Data = data;
        }
    }

    public class DataStringMessage8007
    {
        public string Data { get; }

        public DataStringMessage8007(string data)
        {
            Data = data;
        }
    }

    public class DataToServerGetArrMsg
    {
        public string[] Data { get; }
        public string? Refresh { get; }


        public DataToServerGetArrMsg(string[] data, string? refresh=null)
        {
            Data = (string[])data.Clone();
            Refresh = refresh;
        }
    }

    public class DataToServerGetMsg
    {
        public string Data { get; }

        public DataToServerGetMsg(string data)
        {
            Data = data;
        }
    }

    public class DataToServerConnMsg
    {
        public string Data { get; }

        public DataToServerConnMsg(string data)
        {
            Data = data;
        }
    }

    public class DataToServerGetAF_SD
    {
        public string[] Data { get; }

        public DataToServerGetAF_SD(string[] data)
        {
            Data = (string[])data.Clone();
        }
    }

    public class DisplaySelectRefresh
    {
        public string Data { get; }

        public DisplaySelectRefresh(string data)
        {
            Data = data;
        }
    }

    public class NettyConnectionResultMessage
    {
        public string ResultCode { get; }

        public NettyConnectionResultMessage(string resultCode)
        {
            ResultCode = resultCode;
        }
    }

    public class RefreshAuctionSV_Message
    {
        public string Data { get; }
        public RefreshAuctionSV_Message(string data)
        {
            Data = data;
        }
    }

    public class PageIndicatorStateMessage
    {
        public int CurrentPage { get; }
        public int TotalPages { get; }
        public bool IsMaster { get; }
        public bool IsFrozen { get; }
        public bool IsSubFallbackActive { get; }
        public string FreezeReason { get; }
        public bool IsTimerRunning { get; }

        public PageIndicatorStateMessage(
            int currentPage,
            int totalPages,
            bool isMaster,
            bool isFrozen,
            bool isSubFallbackActive,
            string freezeReason = "",
            bool isTimerRunning = false)
        {
            CurrentPage = currentPage;
            TotalPages = totalPages;
            IsMaster = isMaster;
            IsFrozen = isFrozen;
            IsSubFallbackActive = isSubFallbackActive;
            FreezeReason = freezeReason;
            IsTimerRunning = isTimerRunning;
        }
    }

}
