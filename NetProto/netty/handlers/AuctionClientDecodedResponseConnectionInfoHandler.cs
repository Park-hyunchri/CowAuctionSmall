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
 * 사용자 접속 정보 보낸 후 결과 수신
 */
    public class AuctionClientDecodedResponseConnectionInfoHandler : SimpleChannelInboundHandler<ResponseConnectionInfo>
    {
        private iNettyControllable mController;

        public AuctionClientDecodedResponseConnectionInfoHandler(iNettyControllable controller)
        {
            this.mController = controller;
        }

        //@Override
        protected override void ChannelRead0(IChannelHandlerContext ctx, ResponseConnectionInfo responseConnectionInfo)
        {
            if (mController != null)
            {
                mController.OnResponseConnectionInfo(responseConnectionInfo);
            }
            //Debug.WriteLine("[" + System.DateTime.Now.ToString() + "]" + " ==>> called ResponseConnectionInfoHandler.ChannelRead0");
        }
    }
}
