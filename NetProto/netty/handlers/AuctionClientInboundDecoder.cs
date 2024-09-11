using CowAuctionSmall.Models;
using CowAuctionSmall.NetProto.interfaces;
using CowAuctionSmall.NetProto.models;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace CowAuctionSmall.NetProto.netty.handlers
{
    /**
     * 서버로부터 받은 메세지 수신
     * 
     */
    public class AuctionClientInboundDecoder : MessageToMessageDecoder<String>
    {
        private iNettyControllable mController;
        private NLogger logger;
        public AuctionClientInboundDecoder(iNettyControllable controller)
        {
            logger = NLogger.Instance;
            mController = controller;
        }

        //@Override
        public override void ChannelActive(IChannelHandlerContext ctx)
        {
            base.ChannelActive(ctx);
            // 서버와 연결 성공시            
            //Console.WriteLine(" ==> called AuctionClientInboundDecoder.ChannelActive");
            mController.onActiveChannel(ctx);
        }

        //@Override
        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            EndPoint address = (EndPoint)ctx.Channel.RemoteAddress;

            mController.onChannelInactive(((IPEndPoint)address).Port); // 서버와 연결 끊어졌을경우
            base.ChannelInactive(ctx);
        }

        //@Override
        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
        {
            EndPoint address = (EndPoint)ctx.Channel.RemoteAddress;
            mController.onChannelInactive(((IPEndPoint)address).Port);
            Console.WriteLine(exception.StackTrace);
            base.ExceptionCaught(ctx, exception);
        }

        /**
         * 서버로 부터 받은 메세지 판별 후 객체 생성
         */
        //@Override
        protected override void Decode(IChannelHandlerContext ctx, String message, List<Object> _out)
        {
            Debug.WriteLine("[" + System.DateTime.Now.ToString()+ "]" + "MSG>>" + message);
            logger.LogInfo("[" + System.DateTime.Now.ToString() + "]" + "MSG>>" + message);
            String[] Msgs = message.Split(GlobalDefine.NETTY_INFO.DELIMITER);

            string foo = message.Substring(0, 2);

            switch (foo)
            {
                case "SS":      //접속유효처리 확인
                    _out.Add(new AuctionCheckSession());
                    break;
                case "AR":      //접속 결과
                    _out.Add(new ResponseConnectionInfo(Msgs[1], Msgs[2], Msgs[3], Msgs[4]));
                    break;
                case "SV":      //CurrentInfo
                    _out.Add(message);
                    break;
                default:
                    _out.Add(message);
                    break;
            }
        }
    }
}
