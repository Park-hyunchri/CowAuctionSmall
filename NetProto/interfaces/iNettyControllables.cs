using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Handlers;
using CowAuctionSmall.NetProto.models;

namespace CowAuctionSmall.NetProto.interfaces
{
    public interface iNettyControllable
    {
        void onActiveChannel(IChannelHandlerContext ctx); // Channel Active 상태 반환

        //string onActiveChannel(IChannel channel, ConnectionInfo ConnInfo); // Channel Active 상태 반환

        void OnResponseConnectionInfo(ResponseConnectionInfo responseConnectionInfo); // 접속 정보 인증 응답

        void OnCurrentAuctionData(String data); // 엔트리 정보 수신

        void onCheckSession(IChannelHandlerContext ctx, AuctionCheckSession auctionCheckSession); // 경매 서버 접속 유효 확인 요청

        void onChannelInactive(int port); // 서버와 연결 끊어졌을경우

        void onConnectionException(); // 서버와 연결 실패
    }
}
