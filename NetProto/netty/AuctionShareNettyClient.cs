using CowAuctionSmall.NetProto.interfaces;
using CowAuctionSmall.NetProto.netty.handlers;
using DotNetty.Codecs;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using IChannel = DotNetty.Transport.Channels.IChannel;

namespace CowAuctionSmall.NetProto.netty
{
    /// <summary>
    /// Netty 클라이언트: 경매 서버와 연결 및 데이터 송수신을 담당.
    /// </summary>
    class AuctionShareNettyClient
    {
        private int port = 0;
        private IEventLoopGroup? group;
        private IChannel? channel;
        private Timer? pingTimer;
        private bool isReconnecting = false;
        private const int MAX_RETRY_COUNT = 5;
        private int retryCount = 0;
        private static readonly TimeSpan ChannelCloseTimeout = TimeSpan.FromMilliseconds(800);
        private static readonly TimeSpan GroupShutdownTimeout = TimeSpan.FromSeconds(2);

        /// <summary>
        /// 생성자: Netty 클라이언트 설정을 초기화하고 실행.
        /// </summary>
        private AuctionShareNettyClient(Builder builder)
        {
            this.port = builder.port;
            StartPingService();
            _ = CreateNettyClientWait(builder); // 비동기 실행
        }

        /// <summary>
        /// Netty 서버에 Ping을 보내 연결 상태 확인
        /// </summary>
        private void StartPingService()
        {
            pingTimer = new Timer(async (e) =>
            {
                if (channel != null && channel.Active)
                {
                    try
                    {
                        await channel.WriteAndFlushAsync("PING\r\n");
                        Console.WriteLine("📡 서버에 Ping 전송");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"⚠️ Ping 실패: {ex.Message}");
                        //HandleDisconnect();
                    }
                }
                else
                {
                    //HandleDisconnect();
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10)); // 10초마다 Ping 전송
        }

        /// <summary>
        /// Netty 클라이언트를 비동기적으로 실행하는 메서드
        /// </summary>
        private async Task CreateNettyClientWait(Builder builder)
        {
            try
            {
                var controller = builder.controller;
                if (controller == null)
                {
                    Debug.WriteLine("⚠️ Netty controller가 설정되지 않았습니다.");
                    return;
                }
                await startClient(builder.host, builder.port, controller);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Netty 연결 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// Netty 클라이언트 설정을 위한 Builder 패턴
        /// </summary>
        public class Builder
        {
            public string host;
            public int port;
            public iNettyControllable? controller;

            public Builder(string host, int port)
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

        /// <summary>
        /// Netty 서버로 메시지를 전송하는 메서드
        /// </summary>
        public void sendMessage(String message)
        {
            if (channel == null || !channel.Active)
            {
                return;
            }
            channel.WriteAndFlushAsync(message + "\r\n");
        }

        /// <summary>
        /// Netty 클라이언트를 종료하는 메서드
        /// </summary>
        public async Task stopClient()
        {
            var currentPingTimer = Interlocked.Exchange(ref pingTimer, null);
            currentPingTimer?.Dispose();

            var currentChannel = Interlocked.Exchange(ref channel, null);
            if (currentChannel != null)
            {
                try
                {
                    var closeTask = currentChannel.CloseAsync();
                    if (await Task.WhenAny(closeTask, Task.Delay(ChannelCloseTimeout)).ConfigureAwait(false) != closeTask)
                    {
                        Debug.WriteLine($"⚠️ Netty channel close timeout ({ChannelCloseTimeout.TotalMilliseconds}ms)");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Netty channel close error: {ex.Message}");
                }
            }

            var currentGroup = Interlocked.Exchange(ref group, null);
            if (currentGroup != null)
            {
                try
                {
                    var shutdownTask = currentGroup.ShutdownGracefullyAsync(TimeSpan.Zero, GroupShutdownTimeout);
                    if (await Task.WhenAny(shutdownTask, Task.Delay(GroupShutdownTimeout + TimeSpan.FromMilliseconds(500))).ConfigureAwait(false) != shutdownTask)
                    {
                        Debug.WriteLine($"⚠️ Netty group shutdown timeout ({GroupShutdownTimeout.TotalSeconds:0.0}s)");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Netty group shutdown error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Netty 클라이언트가 활성 상태인지 확인하는 메서드
        /// </summary>
        public bool isActive()
        {
            return channel != null && channel.Active;
        }

        public int getPort()
        {
            return port;
        }

        public IChannel? getChannel()
        {
            return channel;
        }

        /// <summary>
        /// Netty 클라이언트를 시작하는 메서드
        /// </summary>
        public async Task<IChannel> startClient(string host, int port, iNettyControllable nc)
        {
            group = new MultithreadEventLoopGroup();
            IChannel bch = null;

            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        // TLS(SSL) 보안 처리
                        pipeline.AddLast("tls", new TlsHandler(
                            stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true),
                            new ClientTlsSettings("xn--e20bw05b.kr")
                        ));

                        // 패킷 데이터 처리 (구분자 기반)
                        pipeline.AddLast(new DelimiterBasedFrameDecoder(1024, Delimiters.LineDelimiter()));
                        pipeline.AddLast(new StringDecoder(System.Text.Encoding.UTF8));

                        // Netty 클라이언트 핸들러 등록
                        pipeline.AddLast(new AuctionClientInboundDecoder(nc));
                        pipeline.AddLast(new AuctionClientDecodedResponseConnectionInfoHandler(nc));
                        pipeline.AddLast(new AuctionClientDecodedCheckSessionHandler(nc));
                        pipeline.AddLast(new AuctionClientDecodedCurrentEntityInfoHandler(nc));
                        pipeline.AddLast(new StringEncoder(System.Text.Encoding.UTF8));
                    }));

                // Netty 서버에 접속 시도
                bch = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(host), port));
            }
            catch (Exception e)
            {
                Console.WriteLine($"❌ Netty 클라이언트 시작 오류: {e.Message}");
                try
                {
                    nc.onConnectionException();
                }
                catch (Exception callbackEx)
                {
                    Debug.WriteLine($"onConnectionException 처리 중 예외: {callbackEx.Message}");
                }

                try
                {
                    await stopClient();
                }
                catch (Exception stopEx)
                {
                    Debug.WriteLine($"stopClient 처리 중 예외: {stopEx.Message}");
                }
            }

            channel = bch;
            return bch;
        }

        /*public async Task<IChannel> startClient(String host, int port, iNettyControllable nc)
        {
            string BaseDir = AppDomain.CurrentDomain.BaseDirectory;

#if DEBUG
            //Debug.WriteLine("BaseDir : " + Path.Combine(BaseDir, "X.509\\www.cowauction.kr_tomcat.pfx"));
            //X509Certificate2 cert = new X509Certificate2(Path.Combine(BaseDir, "X.509\\www.cowauction.kr_tomcat.pfx"), "ishift");
            //Debug.WriteLine("DEBUG X.509");
            string targetHost = "xn--e20bw05b.kr";

#else
            //Debug.WriteLine("BaseDir : " + Path.Combine(BaseDir, "X.509\\www.cowauction.kr_tomcat.pfx"));
            //X509Certificate2 cert = new X509Certificate2(Path.Combine(BaseDir, "X.509\\www.cowauction.kr_tomcat.pfx"), "ishift");
            //Debug.WriteLine("RELEASE X.509");
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
                Debug.WriteLine("# startClient: " + e.Message);
                // 연결 실패
                nc.onConnectionException();                
                stopClient();                
            }
            this.channel = bch;
            return bch;
        }*/
    }
}
