using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.NetProto.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 메신저로 사용할 클래스 정의 
/// 비동기로 뭔가 넘겨주고 싶을 때 사용
/// </summary>
namespace CowAuctionSmall.Models
{
    public class DataChangedMessage
    {
        public List<gValues> Data { get; }

        public DataChangedMessage(List<gValues> data)
        {
            Data = data;
        }
    }
    public class DataResponseConnectionInfoMessage
    {
        public ResponseConnectionInfo Data { get; }

        public DataResponseConnectionInfoMessage(ResponseConnectionInfo data)
        {
            Data = data;
        }
    }

    public class DataStringArrMessage
    {
        public string[] Data { get; }

        public DataStringArrMessage(string[] data)
        {
            Data = data;
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
            Data = data;
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
            Data = data;
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

        public PageIndicatorStateMessage(
            int currentPage,
            int totalPages,
            bool isMaster,
            bool isFrozen,
            bool isSubFallbackActive)
        {
            CurrentPage = currentPage;
            TotalPages = totalPages;
            IsMaster = isMaster;
            IsFrozen = isFrozen;
            IsSubFallbackActive = isSubFallbackActive;
        }
    }

}
