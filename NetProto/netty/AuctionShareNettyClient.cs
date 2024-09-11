using CowAuctionSmall.NetProto.interfaces;
using CowAuctionSmall.NetProto.netty.handlers;
using DotNetty.Codecs;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Net;
using System.Net.Security;
using System.Threading.Tasks;
using IChannel = DotNetty.Transport.Channels.IChannel;

namespace CowAuctionSmall.NetProto.netty
{
    class AuctionShareNettyClient
    {
        private int port = 0;
        private IEventLoopGroup? group = null;
        private IChannel? channel = null;

        private AuctionShareNettyClient(Builder builder)
        {
            this.port = builder.port;
            CreateNettyClientWait(builder);
        }

        //private async void CreateNettyClientWait(Builder builder)
        private void CreateNettyClientWait(Builder builder)
        {
            //KIH_1219: Netty 접속 실표시 Alert 표출되는거 수정 함. 여기서 지연
            //await Task.Run(() => this.startClient(builder.host, builder.port, builder.controller));
            ////channel = AuctionDelegate.getInstance().mClient.getChannel();

            var task1 = Task.Run(() => this.startClient(builder.host, builder.port, builder.controller));

            while (!task1.IsCompleted) { }

            if (task1.Status == TaskStatus.Faulted)
            {
                //throw new DotNetty.Transport.Channels.ConnectException("", task1.Exception.InnerExceptions[0]);
                foreach (var e in task1.Exception.InnerExceptions)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public class Builder
        {
            public String host;
            public int port;
            public iNettyControllable controller;

            public Builder(String host, int port)
            {
                this.host = host;
                this.port = port;
            }

            public Builder setController(iNettyControllable controller)
            {
                this.controller = controller;
                return this;
            }

            public AuctionShareNettyClient buildAndRun()
            {
                return new AuctionShareNettyClient(this);
            }
        }

        public void sendMessage(String message)
        {
            channel.WriteAndFlushAsync(message + "\r\n");
        }

        public void stopClient()
        {
            if(this.group != null)
                this.group.ShutdownGracefullyAsync();
            this.group = null;
        }

        public bool isActive()
        {
            if (channel != null)
            {
                return channel.IsActive;
            }
            else
            {
                return false; 
            }
        }

        public int getPort()
        {
            return port;
        }

        public IChannel? getChannel()
        {
            return channel;
        }

        public async Task<IChannel> startClient(String host, int port, iNettyControllable nc)
        {
            string BaseDir = AppDomain.CurrentDomain.BaseDirectory;

#if DEBUG
            //Console.WriteLine("BaseDir : " + Path.Combine(BaseDir, "X.509\\www.cowauction.kr_tomcat.pfx"));
            //X509Certificate2 cert = new X509Certificate2(Path.Combine(BaseDir, "X.509\\www.cowauction.kr_tomcat.pfx"), "ishift");
            //Console.WriteLine("DEBUG X.509");
            string targetHost = "xn--e20bw05b.kr";

#else
            //Console.WriteLine("BaseDir : " + Path.Combine(BaseDir, "X.509\\www.cowauction.kr_tomcat.pfx"));
            //X509Certificate2 cert = new X509Certificate2(Path.Combine(BaseDir, "X.509\\www.cowauction.kr_tomcat.pfx"), "ishift");
            //Console.WriteLine("RELEASE X.509");
            string targetHost = "cowauction.kr";
#endif
            //string targetHost = cert.GetNameInfo(X509NameType.DnsName, false);

            IChannel? bch = null;
            //var group = new MultithreadEventLoopGroup();
            this.group = new MultithreadEventLoopGroup();
            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(this.group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        //pipeline.AddLast("ssl", new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(host)));
                        //pipeline.AddAfter("ssl", "delimiter", new DelimiterBasedFrameDecoder(GlobalDefine.NETTY_INFO.NETTY_MAX_FRAME_LENGTH, Delimiters.LineDelimiter()));
                        //  [E] 사설 인증서 - 사용시 주석 해제

                        pipeline.AddLast("tls", new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
                        pipeline.AddLast(new DelimiterBasedFrameDecoder(1024,  Delimiters.LineDelimiter()));
                        pipeline.AddLast(new StringDecoder(System.Text.Encoding.UTF8));
                        pipeline.AddLast(new AuctionClientInboundDecoder(nc));
                        pipeline.AddLast(new AuctionClientDecodedResponseConnectionInfoHandler(nc));    // 사용자 정보 결과 수신
                        pipeline.AddLast(new AuctionClientDecodedCheckSessionHandler(nc));              // 경매 서버 접속 유효 확인
                        pipeline.AddLast(new AuctionClientDecodedCurrentEntityInfoHandler(nc));         // 경매 정보 결과 수신
                        pipeline.AddLast(new StringEncoder(System.Text.Encoding.UTF8));
                    }));

                bch = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(host), port));
            }
            catch (Exception e)
            {
                Console.WriteLine("# startClient: " + e.Message);
                // 연결 실패
                nc.onConnectionException();                
                stopClient();                
            }
            this.channel = bch;
            return bch;
        }
    }
}
