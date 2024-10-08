using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.NetProto.interfaces;
using CowAuctionSmall.NetProto.models;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;

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
            //Debug.WriteLine(" ==> called AuctionClientInboundDecoder.ChannelActive");
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
        //@Override
        protected override void Decode(IChannelHandlerContext ctx, String message, List<Object> _out)
        {
            Debug.WriteLine("[" + System.DateTime.Now.ToString()+ "]" + "MSG>>" + message);
            
            String[] Msgs = message.Split(GlobalDefine.NETTY_INFO.DELIMITER);

            string foo = message.Substring(0, 2);
            if (!foo.Equals("SV"))
            {
                logger.LogInfo("[" + System.DateTime.Now.ToString() + "]" + "MSG>>" + message);
            }
            switch (foo)
            {
                case "SS":      //접속유효처리 확인
                    if (_refreshTRunning)
                    {
                        AddNettyState("SS");
                    }
                    _out.Add(new AuctionCheckSession());
                    break;
                case "AR":      //접속 결과
                    _out.Add(new ResponseConnectionInfo(Msgs[1], Msgs[2], Msgs[3], Msgs[4]));
                    if (Msgs[2].Equals("2000")) 
                    {
                        _refreshTRunning = true; // 타이머 시작을 표시
                        Debug.WriteLine("타이머 시작됨: 10초 후 실행");
                        WeakReferenceMessenger.Default.Send(new DataStringMessage("연결성 상태확인 중.."));
                        // 타이머 시작 (10초 후 단발성 실행)
                        _refreshTimer = new Timer(OnTimerElapsed, null, 10000, Timeout.Infinite);
                    }
                    break;
                case "SV":      //CurrentInfo
                    _out.Add(message);
                    break;
                default:
                    _out.Add(message);
                    break;
            }
        }

        private readonly object _lockObj = new object(); // 락 오브젝트 선언
        private Timer _refreshTimer;
        private bool _refreshTRunning = false;  // 타이머가 실행 중인지 확인하는 플래그
        private bool _stateSS = false; //SS신호가 왔는지 확인하는 플래그
        //새로고침 신호가 오면 호출될 함수 (단일 SV)
        private void AddNettyState(string state)
        {
            _stateSS = state.Equals("SS");
            // 항목을 리스트에 추가
            lock (_lockObj)
            {
                // 타이머가 실행 중이면 더 이상 타이머를 추가하지 않음
                if (!_refreshTRunning)
                {
                    Debug.WriteLine("타이머 시작됨: 10초 후 실행");

                    // 타이머 시작 (10초 후 단발성 실행)
                    _refreshTimer = new Timer(OnTimerElapsed, null, 15000, Timeout.Infinite);
                }
                else
                {
                    Debug.WriteLine("네티 연결성 체크 (SS) 확인 중..  15초만 기다려 주세요");
                }
            }


            //10초 뒤 실행됨
        }

        private void OnTimerElapsed(object state)
        {
            lock (_lockObj)
            {
                if (_stateSS ==true)
                {
                    Debug.WriteLine("SS 상태 확인 완료 Good");
                    _refreshTRunning = true; // 타이머 시작을 표시
                }
                else
                {
                    Debug.WriteLine("SS 상태 확인 완료 Bad");

                    // 현재 실행 중인 애플리케이션 경로 가져오기
                    var fileName = Process.GetCurrentProcess().MainModule.FileName;

                    // UI 스레드에서 실행되도록 확인
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 새 프로세스 시작
                        Process.Start(fileName);

                        // 현재 애플리케이션 종료
                        Application.Current.Shutdown();
                    });
                }
            }

            // 타이머를 종료하고, 실행 중 플래그를 리셋
            _stateSS = false;
            _refreshTimer?.Dispose();
            _refreshTimer = null;
            _refreshTRunning = false; // 타이머가 종료되었으므로 플래그 리셋
            Debug.WriteLine("SS 타이머 종료됨");
            //메인뷰로 날림 메인뷰 맨밑의 TextBox 
            WeakReferenceMessenger.Default.Send(new DataStringMessage("연결성 상태 SS"));
        }
    }
}
