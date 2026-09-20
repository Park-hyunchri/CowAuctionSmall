using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models;
using CowAuctionSmall.NetProto.interfaces;
using CowAuctionSmall.NetProto.models;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CowAuctionSmall.NetProto.netty
{
    class NettyControllable : iNettyControllable
    {
        public String HouseCode = "";
        public string Uname = "";
        public string Token = "";
        public string Priority = "";
        public string Channel ="";

        private NLogger logger;

        public NettyControllable(string _HouseCode, string _Uname, string _Token, string _Channel, string _Priority)
        {
            this.HouseCode = _HouseCode;
            this.Uname = _Uname;
            this.Token = _Token;
            this.Priority = _Priority;
            this.Channel = _Channel;

            logger = NLogger.Instance;
        }

        public void onActiveChannel(IChannelHandlerContext ctx)
        {
            ConnectionInfo authInfo = new ConnectionInfo(this.HouseCode, this.Uname, this.Token, this.Channel, this.Priority);
            String message = authInfo.getEncodedMessage();

            // Instance 생성전이라 CTX 채널로 전송 해야 함
            //AuctionDelegate.getInstance().sendMessage(message);
            ctx.Channel.WriteAndFlushAsync(message + "\r\n");
        }

        public void onChannelInactive(int port)
        {
            //throw new NotImplementedException();
            Debug.WriteLine("## NETTY  DISCONNECT !! ");
            logger.LogError("## NETTY  DISCONNECT !! ");
        }

        public void onCheckSession(IChannelHandlerContext ctx, AuctionCheckSession auctionCheckSession)
        {
            //throw new NotImplementedException();
            // 서버에서 요청이 오면 사용자 정보 보내줌.
            //Debug.WriteLine("[세션 정보 보냄 ] ");
            AuctionDelegate.getInstance().sendMessage(new AuctionReponseSession(Uname, Channel, Priority).getEncodedMessage());
        }

        public void onConnectionException()
        {
            const string msg = "[응찰서버 연결 예외]";
            Debug.WriteLine(msg);
            logger.LogError(msg);

            // 연결 실패를 상위 흐름에 알린다.
            WeakReferenceMessenger.Default.Send(new DataStringMessage(msg));
            WeakReferenceMessenger.Default.Send(new NettyConnectionResultMessage("2001"));
        }
        /// <summary>
        /// 서버로 오는 데이터1
        /// </summary>
        /// <param name="data"></param>
        public void OnResponseConnectionInfo(ResponseConnectionInfo responseConnectionInfo)
        {
            //gMain.Main.mNetStateList.Enqueue(responseConnectionInfo);
            Debug.WriteLine("OnResponseConnectionInfo 에서 호출         {0} \n ", responseConnectionInfo.ToString());

            WeakReferenceMessenger.Default.Send(new DataResponseConnectionInfoMessage(responseConnectionInfo));
        }
        /// <summary>
        /// 서버로 오는 데이터2
        /// </summary>
        /// <param name="data"></param>
        public void OnCurrentAuctionData(String data)
        {
            //gMain.Main.mNetMessageList.Enqueue(data);
            Debug.WriteLine("OnCurrentAuctionData 에서 호출         {0} \n ", data);
            WeakReferenceMessenger.Default.Send(new DataStringArrMessage(data.Split('|')));
        }
    }
}
