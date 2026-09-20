using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CowAuctionSmall.Models.Structures
{
    static public class AuctionStatus
    {
        public enum AF
        {
            NONE = 11,
            FINISH = 22,
            HOLD = 23,
            CANCEL = 24
        }

        public enum AS
        {
            NONE = 8001,                    // 경매 대기 (API로 데이터 수신해야 함) : NONE
            READY = 8002,                   // 경매 준비
            START = 8003,                   // 경매 시작
            PROGRESS = 8004,                // 경매 진행중
            PASS = 8005,                    // 경매 취소 : CANCEL ?
            COMPLETED = 8006,               // 경매 종료 : FINISH
            FINISH = 8007                   // 회차 종료
        }

        public enum AR
        {
            AUTH_OK = 2000,                // 인증 성공
            AUTH_FAIL = 2001,              // 인증 실패
            CONN_DUP = 2002,               //중복 접속
            NOT_READY_CONTROLLER = 2003,   //제어프로그램 준비 안된 상태
            OTHER_PROBLEM = 2004,          //기타 장애
            PROG_RUN_FAIL = 2005,          //프로그램 실행 불가
            CONN_EXPIRATION = 2006         //관전자 접속 만료
        }
    }
}
