using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Models;
using System;
using System.Diagnostics;
using System.Windows.Interop;
using System.Windows.Threading;
using static CowAuctionSmall.Models.Structures.AuctionStatus;

namespace CowAuctionSmall.Services
{
    public class NettyAsyncMsgProcess
    {
        private readonly WeakReferenceMessenger _messenger;
        private readonly WeakReferenceMessenger _messengerString;

        private readonly Dispatcher _dispatcher;

        private int _auctionmethod = 0;
        private NLogger logger;


        /// <summary>
        /// 네티 메시지 처리에 필요한 리소스를 초기화한다.
        /// </summary>
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
        /// <summary>
        /// 서버 접속 결과 메시지를 처리한다.
        /// </summary>
        private void OnResponceConnMsg(object recipient, DataResponseConnectionInfoMessage message)
        {
            DuplicateServerConn(message.Data.getResult());
        }

        /// <summary>
        /// 비동기적 메시지 처리
        /// </summary>
        /// <summary>
        /// 비동기 문자열 메시지를 분기 처리한다.
        /// </summary>
        private void OnStringArrMsg(object recipient, DataStringArrMessage message)
        {
            Debug.WriteLine("OnStringArrMsg OnStringArrMsg OnStringArrMsg");

            NettyCodeProcess(message.Data);
        }

        /// <summary>
        /// 접속 결과를 로그와 화면 메시지로 전달한다.
        /// </summary>
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

            //연결 결과 메시지 발송
            WeakReferenceMessenger.Default.Send(new NettyConnectionResultMessage(getResult));
        }



        /// <summary>
        /// 코드에 따라 네티 메시지를 분기 처리한다.
        /// </summary>
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

                case "SZ": //[일괄경매] 미응찰내역 표시 , 
                    //SZ | 8808990657202 | 20240422 | 0 | 1 | Y | 1 | 999
                    //코드 , 축협코드 , 날짜 , 구분(일괄 : 0 , 송아지 :1 , 비육우 :2 , 번식우 :3)
                    // 종합안내 F9~F12번 버튼 클릭시 호출 기본 영상 표시 및 미응찰 내역 표시
                    /*
                    기본영상  :  SZ|8808990657202|20240906|0|1|A|1|900
                    미 응 찰  :  SZ|8808990657202|20240906|0|1|Y|1|900
                    경매 대상 :  SZ|8808990657202|20240906|0|1|P|1|900
                    경매 결과 :  SZ|8808990657202|20240906|0|1|N|1|900
                     */
                    Debug.WriteLine(data.ToString());
                    Process_NettyState_SZ(data);
                    break;
                case "AF"://[단일경매] 동가 경매 후 일때 
                    Process_NettyState_AF(data);
                    break;

                case "SV": //[단일경매] 새로고침 또는 예정가 높이기 낮추기
                    //Process_NettyData_SV(data); //250120 처리 방향성 수정
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

            //경매 상태 메시지 만약 회차종료인경우 비동기 while문을 잠시 끄기위해 메시지 전송
            // ServerGetData OnStringMsg8007에서 처리
            WeakReferenceMessenger.Default.Send(new DataStringMessage8007(runningState.ToString()));


            if (_auctionmethod != 0 && _auctionmethod == 10) //일괄경매인경우
            {
                string[] msg = new string[] { _auctionmethod.ToString(), data[0], data[6] };// 경매방식, 코드, 경매상태
                WeakReferenceMessenger.Default.Send(new DataToServerGetArrMsg(msg));
            }
            else // 단일 경매인경우
            {
                if (data.Length >= 7 && runningState == AS.PROGRESS || runningState == AS.COMPLETED ) // Check if data has at least 7 elements
                {
                    string[] msg = new string[] { _auctionmethod.ToString(), data[0], data[2], data[4], data[6] };// 경매방식, 코드 , 경매번호, 현재가격 , 경매상태
                    // Use the msg array here
                    WeakReferenceMessenger.Default.Send(new DataToServerGetArrMsg(msg));
                     
                }
                else if (runningState == AS.NONE)
                {
                    //MSG>>AS|8808990657202|||||8001|||||||
                    Debug.WriteLine("새로고침 시작");
                    WeakReferenceMessenger.Default.Send(new RefreshAuctionSV_Message("Refresh"));
                    string[] msg = new string[] { _auctionmethod.ToString(), "AS", "8001", "refresh" };// 경매방식, 코드 , 경매상태, 새로고침
                    WeakReferenceMessenger.Default.Send(new DataToServerGetArrMsg(msg));
                }
                else
                {
                    // Handle the case where data has less than 7 elements
                    Debug.WriteLine("data array has less than 7 elements!");
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
                        WeakReferenceMessenger.Default.Send(new DataToServerGetAF_SD(data));
                        break;
                    default:
                        break;
                }
            }
            else // 일괄 경매 방식
            {
                if (countCode == "F")
                {
                    // 일괄 경매 마감 신호도 ServerGetData에 전달하여 진행 상태와 화면을 즉시 정리한다.
                    WeakReferenceMessenger.Default.Send(new DataToServerGetAF_SD(data));
                }
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
        /// AF|8808990657202|5|22|1636|412|500: 
        /// {0} 
        /// 
        /// ESC 3번 취소: AF|8808990657202|3|24|||0
        /// </summary>
        /// 
        private async void Process_NettyState_AF(string[] data)
        {
            if (data == null || data.Length < 4)
            {
                logger.LogWarn("Process_NettyState_AF: 데이터 길이가 부족합니다.");
                return;
            }

            string resultCode = data[3];
            if (resultCode == "22" || resultCode == "23" || resultCode == "24")
            {
                // AF를 단일경매 종료의 우선 신호로 사용한다.
                WeakReferenceMessenger.Default.Send(new DataToServerGetAF_SD(data));
            }

            if (resultCode == "22" || resultCode == "23")
            {
                await ServerGetData.Instance.ReCheckSoldItemAsync(data); // 낙찰/유찰 보정
            }
        }


        /// <summary>
        /// 일괄 경매에서 경매 대상 클릭 이벤트를 처리한다.
        /// </summary>
        //일괄경매 경매대상 클릭시 표출(일괄)
        private void Process_NettyState_SZ(string[] data)
        {
            //일괄경매 새로고침을 누를 시
            //MSG>>AS|8808990657202||328|||8002||||||1|0 코드를 보냄..
            //날짜가 없어 해당하는 날짜로 데이터 못 호출함

            //경매 대상 :  SZ | 8808990657202 | 20240906 | 0 | 1 | P | 1 | 900
            //다른 코드는 현재사용처를 모름

            if (data == null || data.Length <= 5)
            {
                logger.LogWarn("Process_NettyState_SZ: 데이터 길이가 부족합니다.");
                return;
            }

            string szType = data[5].ToUpper();

            // 💡 'P'(경매대상), 'N'(경매결과), 'Y'(미응찰) 모두 처리하도록 조건 확장
            if (szType.Equals("P") || szType.Equals("N") || szType.Equals("Y"))
            {
                // ServerGetData의 OnChangeDeta로 날짜 전달
                WeakReferenceMessenger.Default.Send(new DataToServerConnMsg(data[2]));

                // ServerGetData의 OnStringArrMsg(SZ 분기)로 패킷 전달
                string[] msg = new string[] { _auctionmethod.ToString(), data[0], "8000", data[2] };
                WeakReferenceMessenger.Default.Send(new DataToServerGetArrMsg(msg));
            }

            /* if (data[5].ToUpper().Equals("P")) // 경매대상 키를 누를경우  GetCurrentInfo의 date 날짜를 강제 변경..
            {
                WeakReferenceMessenger.Default.Send(new DataToServerConnMsg(data[2]));

                string[] msg = new string[] { _auctionmethod.ToString(), data[0], "8000", data[2] };// 경매방식, 코드, 경매상태, 경매일자
                WeakReferenceMessenger.Default.Send(new DataToServerGetArrMsg(msg));
            } */

        }
    }
}
