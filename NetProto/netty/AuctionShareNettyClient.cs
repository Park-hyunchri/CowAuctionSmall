using CowAuctionSmall.Models;
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
        private IEventLoopGroup? group = null;  // Netty 이벤트 루프 그룹. 연결 종료 시 반드시 해제해야 함.
        private IChannel? channel = null;       // Netty 채널. 연결 종료 시 반드시 해제해야 함.
        private NLogger logger = NLogger.Instance;

        private AuctionShareNettyClient(Builder builder)
        {
            this.port = builder.port;
            CreateNettyClientWait(builder);

        }

        //private async void CreateNettyClientWait(Builder builder)
        private void CreateNettyClientWait(Builder builder)
        {
            logger.LogInfo("AuctionDelegate CreateNettyClientWait 시작 곧 var task1 = Task.Run(() 실행");
            // Netty 클라이언트 연결을 비동기적으로 실행
            var task1 = Task.Run(() => this.startClient(builder.host, builder.port, builder.controller));

            // Task가 완료될 때까지 대기 (비동기 처리의 이점을 활용하지 못함)
            while (!task1.IsCompleted) { }

            if (task1.Status == TaskStatus.Faulted)
            {
                // 예외 발생 시 각 예외 메시지를 출력
                foreach (var e in task1.Exception.InnerExceptions)
                {
                    logger.LogError("CreateNettyClientWait 에러 task1.Exception.InnerExceptions : "+ e.Message);
                    Console.WriteLine(e.Message);
                }
            }
            logger.LogInfo("AuctionDelegate CreateNettyClientWait 끝 \n - task1.Status : " + task1.Status + "\n - task1.Result : "+ task1.Result+ "\n - task1 : "+ task1.ToString());
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
            // 메시지를 채널을 통해 전송
            channel?.WriteAndFlushAsync(message + "\r\n");
        }

        public async Task StopClientAsync()
        {
            // 클라이언트 종료 시 자원 해제
            if (this.group != null)
            {
                await this.group.ShutdownGracefullyAsync();  // 이벤트 루프 그룹의 자원 해제
            }
            this.group = null;
        }


        public bool isActive()
        {
            // 채널이 활성 상태인지 확인
            return channel?.IsActive ?? false;
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
            string targetHost = "xn--e20bw05b.kr";
#else
            string targetHost = "cowauction.kr";
#endif

            IChannel? bch = null;
            this.group = new MultithreadEventLoopGroup();  // 멀티스레드 이벤트 루프 그룹 생성

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

                        // TLS 핸들러 추가
                        pipeline.AddLast("tls", new TlsHandler(
                            stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true),
                            new ClientTlsSettings(targetHost))
                        );

                        // 프레임 디코더, 문자열 디코더/인코더 및 커스텀 핸들러 추가
                        pipeline.AddLast(new DelimiterBasedFrameDecoder(1024, Delimiters.LineDelimiter()));
                        pipeline.AddLast(new StringDecoder(System.Text.Encoding.UTF8));
                        pipeline.AddLast(new AuctionClientInboundDecoder(nc));
                        pipeline.AddLast(new AuctionClientDecodedResponseConnectionInfoHandler(nc)); // 사용자 정보 결과 수신 핸들러
                        pipeline.AddLast(new AuctionClientDecodedCheckSessionHandler(nc));          // 경매 서버 접속 유효 확인 핸들러
                        pipeline.AddLast(new AuctionClientDecodedCurrentEntityInfoHandler(nc));     // 경매 정보 결과 수신 핸들러
                        pipeline.AddLast(new StringEncoder(System.Text.Encoding.UTF8));
                    }));

                bch = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(host), port));  // 비동기 연결 시도
            }
            catch (Exception e)
            {
                logger.LogError("# startClient: " + e.Message);
                Console.WriteLine("# startClient: " + e.Message);

                // 연결 실패 시 컨트롤러에 예외를 알리고 클라이언트를 종료
                nc.onConnectionException();
                await StopClientAsync();
            }

            this.channel = bch;  // 연결된 채널 저장
            return bch;
        }
    }
}
