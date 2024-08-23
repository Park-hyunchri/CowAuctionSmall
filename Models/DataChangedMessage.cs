using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.NetProto.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public DataToServerGetArrMsg(string[] data)
        {
            Data = data;
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
}
