using CowAuctionSmall.Infra.Netty.models;
using CowAuctionSmall.Infra.Netty.interfaces;
using DotNetty.Transport.Channels;
using System;


namespace CowAuctionSmall.Infra.Netty.netty.handlers
{
    /**
     * 서버로부터 세션 정보 요청 수신
     */
    public class AuctionClientDecodedCheckSessionHandler : SimpleChannelInboundHandler<AuctionCheckSession>
    {
        private readonly iNettyControllable mController;

        public AuctionClientDecodedCheckSessionHandler(iNettyControllable controller)
        {
            mController = controller ?? throw new ArgumentNullException(nameof(controller));
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
