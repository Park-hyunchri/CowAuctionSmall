using CowAuctionSmall.Infra.Netty.models;
using CowAuctionSmall.Infra.Netty.interfaces;
using CowAuctionSmall.Infra.Logging;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Domain.Auction;
using CommunityToolkit.Mvvm.Messaging;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CowAuctionSmall.Infra.Netty.netty.handlers
{
    /**
     * 서버로부터 받은 메세지 수신
     * 
     */
    public class AuctionClientInboundDecoder : MessageToMessageDecoder<String>
    {
        private readonly iNettyControllable mController;
        private NLogger logger;
        private readonly WeakReferenceMessenger _msgRefreshString;
        public AuctionClientInboundDecoder(iNettyControllable controller)
        {
            logger = NLogger.Instance;
            mController = controller ?? throw new ArgumentNullException(nameof(controller));
            _msgRefreshString = WeakReferenceMessenger.Default;
            _msgRefreshString.Register<RefreshAuctionSV_Message>(this, OnRefreshMsg);
        }

        //@Override
        public override void ChannelActive(IChannelHandlerContext ctx)
        {
            ServerGetData.Instance?.Ingress.InvalidateSession();
            base.ChannelActive(ctx);
            AsyncDebugOutput.WriteLine("✅ 서버와 연결 성공");
            
            
            mController.onActiveChannel(ctx);
        }

        //@Override
        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            EndPoint address = (EndPoint)ctx.Channel.RemoteAddress;

            ServerGetData.Instance?.Ingress.InvalidateSession();
            _msgRefreshString.UnregisterAll(this);
            _svTimer?.Dispose();
            _refreshTimer?.Dispose();
            mController.onChannelInactive(((IPEndPoint)address).Port); // 서버와 연결 끊어졌을경우
            base.ChannelInactive(ctx);
        }



        //@Override
        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
        {
            mController.onConnectionException();
            AsyncDebugOutput.WriteLine(exception.StackTrace);
            base.ExceptionCaught(ctx, exception);
        }

        /**
         * 서버로 부터 받은 메세지 판별 후 객체 생성
         */
        private const int RefreshDelay = 15000; // 15초 대기 후 실행
        private const int SVDelay = 2000; // 2초 대기 후 실행

        private List<string> _svMessages = new List<string>();
        private Timer? _svTimer;
        private Timer? _refreshTimer;
        private bool _stateSS = false;
        private readonly object _lockObj = new object();

        // 서버에서 받은 메시지 판별 후 객체 생성
        protected override void Decode(IChannelHandlerContext ctx, string message, List<object> _out)
        {
            // 사용자 요청에 따라 분기·필드 분리 전 수신 원문을 Debug 출력에서 확인한다.
            AsyncDebugOutput.WriteRaw(message);
            if (string.IsNullOrEmpty(message) || message.Length < 2) return;
            string[] Msgs = message.Split(GlobalDefine.DELIMITER);
            string foo = message.Substring(0, 2);
            if (foo is not ("SV" or "SS"))
                logger.LogInfo("auction-received " + AuctionSignalSummary.Diagnostic(Msgs));

            switch (foo)
            {
                case "SS": // 접속 유효성 확인
                    HandleSSMessage();
                    _out.Add(new AuctionCheckSession());
                    break;

                case "AR": // 접속 결과 처리
                    HandleARMessage(Msgs, _out);
                    break;

                case "SV": // CurrentInfo
                    _out.Add(message);

                    if (Msgs.Length > 3 && Stopwatch.GetTimestamp() <= Volatile.Read(ref _refreshWindowUntil))
                    {
                        AsyncDebugOutput.WriteLine("새로고침 디코더로 SV 메시지 처리");
                        AsyncDebugOutput.WriteLine("SV 새로고침 감지");
                        string qnc = message.Split("|")[3];

                        // 🔴 여기서 직접 await 못 함
                        RequestRefresh(qnc);

                        Interlocked.Exchange(ref _refreshWindowUntil, 0);
                    }

                    /*                    if (ServerGetData._latestAuctionDataList == null || ServerGetData._latestAuctionDataList.Count ==0 )
                                        {
                                            HandleSVMessage(message, _out);
                                        }
                                        else // 단건으로 여러개 호출 되는데 어거 어떻게 처리하냐.. 새로고침일때는 여러개가 단건씩 들어오고 최저가 변경시 1개만 들어오는데 호출받는 입장에서는 이게 여러건인지 아닌지 어떻게 알아서 처리하냐구
                                        {

                                        }*/
                    break;

                default:
                    _out.Add(message);
                    break;
            }
        }

        private void RequestRefresh(string qcn)
        {
            ServerGetData.Instance?.RequestRefresh(qcn);
        }

        // SS 메시지 처리 (연결 상태 체크)\

        // SS 메시지 처리 (연결 상태 체크, 한 번만 수행)
        private void HandleSSMessage()
        {
            lock (_lockObj)
            {
                if (_stateSS)
                {
                    AsyncDebugOutput.WriteLine("✅ SS 상태 이미 확인됨, 추가 확인 불필요");
                    return;
                }

                _stateSS = true; // 처음 한 번만 체크
                AsyncDebugOutput.WriteLine("✅ SS 상태 확인 완료: 정상");
                logger.LogInfo("✅ SS 상태 확인 완료: 정상");
            }
        }


        // AR 메시지 처리 (접속 결과)
        private void HandleARMessage(string[] Msgs, List<object> _out)
        {
            if (Msgs.Length < 5) return; // 예외 처리 추가

            _out.Add(new ResponseConnectionInfo(Msgs[1], Msgs[2], Msgs[3], Msgs[4]));

            if (Msgs[2].Equals("1000"))
            {
                AsyncDebugOutput.WriteLine("타이머 시작됨: 10초 후 실행");
                WeakReferenceMessenger.Default.Send(new DataStringMessage("연결성 상태확인 중.."));

                lock (_lockObj)
                {
                    _refreshTimer?.Dispose();
                    _refreshTimer = new Timer(OnTimerElapsed, null, 10000, Timeout.Infinite);
                }
            }
        }

        // SV 메시지 처리 (2초간 메시지를 모아서 처리)
        private void HandleSVMessage(string message, List<object> _out)
        {
            lock (_lockObj)
            {
                _svMessages.Add(message);

                if (_svTimer == null)
                {
                    _svTimer = new Timer(OnSvTimerElapsed, _out, SVDelay, Timeout.Infinite);
                }
            }
        }

        // 2초 후 SV 메시지 처리
        private void OnSvTimerElapsed(object? state)
        {
            lock (_lockObj)
            {
                if (_svMessages.Count > 0)
                {
                    _svMessages.Insert(0, "20"); // 단일 경매 방식
                    string[] combinedMessages = _svMessages.ToArray();
                    AsyncDebugOutput.WriteLine($"[{DateTime.Now}] SV 메시지 처리: {combinedMessages}");
                    WeakReferenceMessenger.Default.Send(new DataToServerGetArrMsg(combinedMessages));

                    _svMessages.Clear();
                }

                _svTimer?.Dispose();
                _svTimer = null;
            }
        }

        private long _refreshWindowUntil;
        private void OnRefreshMsg(object recipient, RefreshAuctionSV_Message message)
        {
            Interlocked.Exchange(ref _refreshWindowUntil, Stopwatch.GetTimestamp() + 2 * Stopwatch.Frequency);
        }

        // SS 메시지 타이머 이벤트 (연결 체크)
        private void OnTimerElapsed(object? state)
        {
            lock (_lockObj)
            {
                if (_stateSS)
                {
                    AsyncDebugOutput.WriteLine("✅ SS 상태 확인 완료: 정상");
                    logger.LogInfo("✅ SS 상태 확인 완료: 정상");
                }
                else
                {
                    AsyncDebugOutput.WriteLine("❌ SS 상태 확인 실패");
                    logger.LogInfo("❌ SS 상태 확인 실패");

                    RestartApplication();
                }
            }

            // 타이머 종료 및 초기화
            ResetRefreshTimer();
        }

        // 애플리케이션 재시작
        private void RestartApplication()
        {
            WeakReferenceMessenger.Default.Send(new ApplicationRestartRequestedMessage());
        }

        // 타이머 리셋
        private void ResetRefreshTimer()
        {
            _stateSS = false;
            _refreshTimer?.Dispose();
            _refreshTimer = null;
            AsyncDebugOutput.WriteLine("SS 타이머 종료됨");
            WeakReferenceMessenger.Default.Send(new DataStringMessage("연결성 상태 SS"));
        }

    }
}
