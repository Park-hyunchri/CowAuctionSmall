using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models.Structures;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using NLog;
using System;
using System.Diagnostics;
using System.Windows.Interop;
using System.Windows.Threading;
using static CowAuctionSmall.Models.Structures.AuctionStatus;

namespace CowAuctionSmall.Models
{
    public class NettyAsyncMsgProcess
    {
        private readonly WeakReferenceMessenger _messenger;
        private readonly WeakReferenceMessenger _messengerString;

        private readonly Dispatcher _dispatcher;

        private int _auctionmethod = 0;
        private NLogger logger;

        public NettyAsyncMsgProcess()
        {
            logger = NLogger.Instance;
            _dispatcher = Dispatcher.CurrentDispatcher;

            _messenger = WeakReferenceMessenger.Default;
            _messenger.Register<DataResponseConnectionInfoMessage>(this, OnResponceConnMsg);

            _messengerString = WeakReferenceMessenger.Default;
            _messengerString.Register<DataStringArrMessage>(this, OnStringArrMsg);
        }

        /// <summary>
        /// 프로그램 시작시 중복접속여부 확인(중복접속시 서버로부터 반응이 X)
        /// </summary>
        private void OnResponceConnMsg(object recipient, DataResponseConnectionInfoMessage message)
        {
            DuplicateServerConn(message.Data.getResult());
        }

        /// <summary>
        /// 비동기적 메시지 처리
        /// </summary>
        private void OnStringArrMsg(object recipient, DataStringArrMessage message)
        {
            Debug.WriteLine("OnStringArrMsg OnStringArrMsg OnStringArrMsg");

            NettyCodeProcess(message.Data);
        }

        private void DuplicateServerConn(string getResult)
        {
            string msg = "";
            switch (getResult)
            {
                case "2000":
                    msg = "[응찰서버 접속 성공]\r\n";
                    break;
                case "2001":
                    msg = "[응찰서버 접속 실패]\r\n";
                    break;
                case "2002":
                    msg = "[응찰서버 중복 접속]\r\n";
                    break;
                default:
                    msg = "[------------------------]\r\n";
                    break;
            }
            Debug.WriteLine(msg);
            logger.LogInfo(msg);
            //메인뷰로 날림 메인뷰 맨밑의 TextBox 
            WeakReferenceMessenger.Default.Send(new DataStringMessage(msg));
            
        }
        


        private void NettyCodeProcess(string[] data)
        {
            string code = data[0];
            switch (code)
            {
                case "AT": //일괄 단일 구분 
                    _auctionmethod = Process_NettyState_AT(data);
                    break;

                case "AS": // 단일,일괄 스페이스바 땡
                    Process_NettyState_AS(data);
                    break;

                case "SD": // 카운트 다운
                    Process_NettyState_SD(data);
                    break;

                case "SZ": //[일괄경매] 미응찰내역 표시
                    //SZ | 8808990657202 | 20240422 | 0 | 1 | Y | 1 | 999
                    // 종합안내 F9~F12번 버튼 클릭시 호출 기본 영상 표시 및 미응찰 내역 표시
                    break;
                case "AF"://[단일경매] 동가 경매 후 일때 
                    Process_NettyState_AF(data);
                    break;

                case "SV": //[단일경매] 새로고침 또는 예정가 높이기 낮추기
                    Process_NettyData_SV(data);
                    break;

                default:
                    break;
            }
        }

        

        /// <summary>
        /* AS
         * 구분자 | 조합구분코드 | 출품번호 | 경매회차 | 시작가 | 
         * 현재응찰자수 | 경매상태(NONE / READY / START / PROGRESS / PASS / COMPLETED / FINISH) | 
         * 1순위회원번호 | 2순위회원번호 | 3순위회원번호 | 경매진행완료출품수 | 경매잔여출품수 | 일괄경매구간번호 | 
         * 경매유형코드(0:일괄 / 1:송아지 / 2:비육우 / 3:번식우)
         * 
         * AS | 8808990657202 | 1 | 305 | 380 | 0 | 8002 | | | | 8 | -8 | 0 | 0 
         * */
        /// </summary>
        private void Process_NettyState_AS(string[] data)
        {
            AS runningState = (AS)Convert.ToInt32(data[6]);

            if (_auctionmethod != 0 && (_auctionmethod == 10)) //일괄경매인경우
            {
                string[] msg = new string[] { _auctionmethod.ToString(), data[0], data[6] };// 경매방식, 코드, 경매상태
                WeakReferenceMessenger.Default.Send(new DataToServerGetArrMsg(msg));
            }
            else // 단일 경매인경우
            {
                if (data.Length >= 7 && runningState == AS.PROGRESS) // Check if data has at least 7 elements
                {
                    if (data[6].Equals("8007")) //회차종료
                    {
                        WeakReferenceMessenger.Default.Send(new DataStringMessage8007("8007"));
                    }
                    else
                    {
                        string[] msg = new string[] { _auctionmethod.ToString(), data[0], data[2], data[4], data[6] };// 경매방식, 코드 , 경매번호, 현재가격 , 경매상태
                        // Use the msg array here
                        WeakReferenceMessenger.Default.Send(new DataToServerGetArrMsg(msg));
                    }
                }
                else
                {
                    // Handle the case where data has less than 7 elements
                    Console.WriteLine("data array has less than 7 elements!");
                    if(data[6].Equals("8007")) //회차종료
                    {
                        WeakReferenceMessenger.Default.Send(new DataStringMessage8007("8007"));
                    }
                }
            }
        }

        /// <summary>
        /// 단일,일괄 방식으로 경매여부 , 
        /// ex) MSG>>AT|8808990657202|20
        /// data[0] : 코드
        /// data[1] : 축협코드
        /// data[2] : 경매방식
        /// </summary>
        private int Process_NettyState_AT(string[] data)
        {
            if (data == null || data.Length < 3)
                return 0;

            // "10": 일괄경매, "20":단일경매
            WeakReferenceMessenger.Default.Send(new DataStringMessage(data[2]));
            return Convert.ToInt32(data[2]);
        }

        /// <summary>
        /// 단일경매 시 카운트 다운 
        /// SD|8808990657202|C|3:
        /// SD|8808990657202|F|-1 중간에 취소시 경매프로그램에서 ESC 누름
        /// </summary>
        private void Process_NettyState_SD(string[] data)
        {
            string countCode = data[2] != null ? data[2].ToUpper() : "";
            if (_auctionmethod == 20) //단일경매 방식
            {
                switch (countCode)
                {
                    case "C": //경매 카운트
                        break;
                    case "F":// 경매도중 종료
                        break;
                    default:
                        break;
                }
            }
            else // 일괄 경매 방식
            {
                
            }
            
        }

        //SV|8808990657202|4|305|1|410002166217287|01|341593|1|강주언||21.06.07(34개월 11일)|KPN1320|수|혈통|410002116998884|2|0|4|장수21-04-1728|232418522|02|장수|3|1|0|400|340|친자확인, 뒤다리상처|23||0||M|4|N||35|혈통
        /// <summary>
        /// 단일 일때는 (정보의 변경) 
        ///     새로고침
        ///     예정가 높이기 또는 낮추기
        /// </summary>
        private void Process_NettyData_SV(string[] data)
        {
            string[] tempData = new string[data.Length + 1];
            tempData[0] = _auctionmethod.ToString();
            Array.Copy(data, 0, tempData, 1, data.Length);

            WeakReferenceMessenger.Default.Send(new DataToServerGetArrMsg(tempData));
        }

        /// <summary>
        /// 단일경매, 동가 입력 후 결과값 메시지
        /// 코드, 축협코드, 경매번호, 낙찰여부 , ? , 참가번호 , 낙찰가격
        /// AF|8808990657202|5|22|1636|412|500: OnCurrentAuctionData 에서 호출         {0} 
        /// </summary>
        private void Process_NettyState_AF(string[] data)
        {
            //경매 도중 ESC를 누른경우
            if (data[data.Length-1].Equals("0"))
            {
                WeakReferenceMessenger.Default.Send(new DataToServerGetMsg("F"));

            }
        }
    }
}
