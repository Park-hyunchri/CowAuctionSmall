using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.NetProto.interfaces;
using CowAuctionSmall.NetProto.models;
using CowAuctionSmall.Services;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CowAuctionSmall.NetProto.netty.handlers
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
            base.ChannelActive(ctx);
            Debug.WriteLine("✅ 서버와 연결 성공");
            
            
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
            Debug.WriteLine(exception.StackTrace);
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
        private bool _refreshTRunning = false;
        private bool _stateSS = false;
        private readonly object _lockObj = new object();

        // 서버에서 받은 메시지 판별 후 객체 생성
        protected override void Decode(IChannelHandlerContext ctx, string message, List<object> _out)
        {
            Debug.WriteLine($"[{DateTime.Now}] MSG>> {message}");

            string[] Msgs = message.Split(GlobalDefine.DELIMITER);
            string foo = message.Substring(0, 2);

            if (!foo.Equals("SV"))
            {
                if (!message.Contains("SS"))
                {
                    logger.LogInfo($"[{DateTime.Now}] MSG>> {message}");
                }
            }

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

                    if (_isRefreshMsg)
                    {
                        Debug.WriteLine("새로고침 디코더로 SV 메시지 처리");
                        Debug.WriteLine($"[{DateTime.Now}] SV 메시지 즉시 처리: {message}");
                        string qnc = message.Split("|")[3];

                        // 🔴 여기서 직접 await 못 함
                        _ = HandleRefreshAsync(qnc);   // fire-and-forget 비동기 처리

                        _isRefreshMsg = false;
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

        private async Task HandleRefreshAsync(string qnc)
        {
            try
            {
                var conn = ServerConn.Instance;
                if (conn == null)
                {
                    logger.LogError("PostQcn(refresh) 실패: ServerConn 인스턴스가 없습니다.");
                    return;
                }

                var placeholderUser = new UserInfo();
                var qcnResult = await conn.PostQcn(
                    placeholderUser,
                    string.Empty,
                    string.Empty,
                    qnc,
                    "refresh");
                if(qcnResult != null) 
                {
                    Debug.WriteLine($"QCN Result: {qcnResult}");
                    var aucDt = qcnResult.AucDt;
                    //aucDt 와 오늘 날짜 비교
                    string date = DateTime.Now.ToString("yyyyMMdd");
                    if (aucDt != DateTime.Now.ToString("yyyyMMdd"))
                    {
                        //오늘 경매날인데 다른 경매 데이터를 보여줘야한다면? 일단은 새로고침 건너뛰기
                        List<string> qcnAuctionList = await conn.SvInfoRequest(placeholderUser, string.Empty, date, "refresh");
                        if (qcnAuctionList.Count>0)
                        {
                            Debug.WriteLine($"경매 날짜({aucDt})가 오늘 날짜({date})와 다릅니다. 새로고침을 건너뜁니다.");
                            logger.LogInfo("경매 날짜({aucDt})가 오늘 날짜({date})와 다릅니다. 새로고침을 건너뜁니다.");
                            return;
                        }
                        qcnAuctionList = await conn.SvInfoRequest(placeholderUser, string.Empty, aucDt, "refresh");

                        _svMessages.Insert(0, "99"); // 단일 경매 방식
                        string[] combinedMessages = qcnAuctionList.ToArray();
                        Debug.WriteLine($"[{DateTime.Now}] SV 메시지 처리: {combinedMessages}");
                        WeakReferenceMessenger.Default.Send(new DataToServerGetArrMsg(combinedMessages, "refresh"));

                        _svMessages.Clear();
                        Debug.WriteLine("✅ PostQcn(refresh) 성공");
                    }
                    
                }
                
                
                // 필요하면 여기서 qcnResult 후처리
                // e.g. Debug.WriteLine($"QCN Result: {qcnResult}");
            }
            catch (Exception ex)
            {
                // 예외 로깅
                logger.LogError($"PostQcn(refresh) 실패: {ex}");
            }
        }


        // SS 메시지 처리 (연결 상태 체크)\

        // SS 메시지 처리 (연결 상태 체크, 한 번만 수행)
        private void HandleSSMessage()
        {
            lock (_lockObj)
            {
                if (_stateSS)
                {
                    Debug.WriteLine("✅ SS 상태 이미 확인됨, 추가 확인 불필요");
                    return;
                }

                _stateSS = true; // 처음 한 번만 체크
                Debug.WriteLine("✅ SS 상태 확인 완료: 정상");
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
                Debug.WriteLine("타이머 시작됨: 10초 후 실행");
                WeakReferenceMessenger.Default.Send(new DataStringMessage("연결성 상태확인 중.."));

                lock (_lockObj)
                {
                    _refreshTRunning = true;
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
        private void OnSvTimerElapsed(object state)
        {
            lock (_lockObj)
            {
                if (_svMessages.Count > 0)
                {
                    _svMessages.Insert(0, "20"); // 단일 경매 방식
                    string[] combinedMessages = _svMessages.ToArray();
                    Debug.WriteLine($"[{DateTime.Now}] SV 메시지 처리: {combinedMessages}");
                    WeakReferenceMessenger.Default.Send(new DataToServerGetArrMsg(combinedMessages));

                    _svMessages.Clear();
                }

                _svTimer?.Dispose();
                _svTimer = null;
            }
        }

        private bool _isRefreshMsg = false;
        private void OnRefreshMsg(object recipient, RefreshAuctionSV_Message message)
        {
            Debug.WriteLine("새로고침 디코더");
            // 2초 동안만 _isRefreshMsg = true 유지
            _isRefreshMsg = true;
            Task.Delay(2000).ContinueWith(_ =>
            {
                _isRefreshMsg = false;
            });
        }

        // SS 메시지 타이머 이벤트 (연결 체크)
        private void OnTimerElapsed(object state)
        {
            lock (_lockObj)
            {
                if (_stateSS)
                {
                    Debug.WriteLine("✅ SS 상태 확인 완료: 정상");
                    logger.LogInfo("✅ SS 상태 확인 완료: 정상");
                    _refreshTRunning = true;
                }
                else
                {
                    Debug.WriteLine("❌ SS 상태 확인 실패");
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
            string fileName = Process.GetCurrentProcess().MainModule.FileName;

            Task.Run(() =>
            {
                Process.Start(fileName);
                Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
            });
        }

        // 타이머 리셋
        private void ResetRefreshTimer()
        {
            _stateSS = false;
            _refreshTimer?.Dispose();
            _refreshTimer = null;
            _refreshTRunning = false;
            Debug.WriteLine("SS 타이머 종료됨");
            WeakReferenceMessenger.Default.Send(new DataStringMessage("연결성 상태 SS"));
        }

    }
}
