using System;

namespace CowAuctionSmall.NetProto.models
{
    /**
     * 경매 서버 접속 정보 유효 확인 처리
     * 
     * 서버 -> 공통
     * 
     * SK
     *
     */

    public class AuctionCheckSession
    {
        public const char ORIGIN = 'S';
        public const char TYPE = 'S';

        public AuctionCheckSession()
        {

        }

        public String getEncodedMessage()
        {
            return String.Format("{0}{1}", ORIGIN, TYPE);
        }
    }
}
