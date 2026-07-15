using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CowAuctionSmall.NetProto.interfaces;
using DotNetty.Transport.Channels;


namespace CowAuctionSmall.NetProto.netty.handlers
{
    /**
 * 사용자 접속 정보 보낸 후 결과 수신
 */
    public class AuctionClientDecodedCurrentEntityInfoHandler : SimpleChannelInboundHandler<String>
    {
        private readonly iNettyControllable mController;

        public AuctionClientDecodedCurrentEntityInfoHandler(iNettyControllable controller)
        {
            mController = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        //@Override
        protected override void ChannelRead0(IChannelHandlerContext ctx, String data)
        {
            if (mController != null)
            {
                mController.OnCurrentAuctionData(data);                
            }
        }
    }
}

