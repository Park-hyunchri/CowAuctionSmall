using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CowAuctionSmall.NetProto.interfaces;
using CowAuctionSmall.NetProto.models;
using DotNetty.Transport.Channels;


namespace CowAuctionSmall.NetProto.netty.handlers
{
    /**
     * 서버로부터 세션 정보 요청 수신
     */
    public class AuctionClientDecodedCheckSessionHandler : SimpleChannelInboundHandler<AuctionCheckSession>
    {
        private iNettyControllable mController;

        public AuctionClientDecodedCheckSessionHandler(iNettyControllable controller)
        {
            this.mController = controller;
        }

        //@Override
        protected override void ChannelRead0(IChannelHandlerContext ctx, AuctionCheckSession auctionCheckSession)
        {
            if (mController != null)
            {
                mController.onCheckSession(ctx, auctionCheckSession);
            }           
        }
    }
}
