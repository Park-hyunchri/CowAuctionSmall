
using CowAuctionSmall.NetProto.netty;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CowAuctionSmall.NetProto.models
{

    [Serializable]
    public class ConnectionInfo
    {
        //private static long serialVersionUID = 4703227204913480236L;

        public static char ORIGIN = 'A';

        public static char TYPE = 'I';


        public string mAuctionHouseCode { get; private set; } //축협코드 
        public string mUserMemNum { get; private set; } // 거래인관리번호
        public string mAuthToken { get; private set; } //인증토큰
        public string mChannel { get; private set; } //접속 요청 채널
        public String mOS { get; private set; } // 사용 채널
        public String mAuctionJoinNum { get; private set; } = string.Empty; // 경매참가번호(패킷데이터에서 제외)


        public ConnectionInfo(String auctionHouseCode, String userMemNum, String authToken, String channel, String os)
        {
            mAuctionHouseCode = auctionHouseCode ?? "UNKNOWN";
            mUserMemNum = userMemNum ?? "UNKNOWN";
            mAuthToken = authToken ?? "INVALID";
            mChannel = channel ?? "DEFAULT";
            mOS = os ?? "UNKNOWN";
        }

        /// <summary>
        /// 문자열로 인코딩된 메시지 반환
        /// </summary>
        public string getEncodedMessage()
        {
            return new StringBuilder()
                .Append(ORIGIN).Append(TYPE).Append(GlobalDefine.DELIMITER)
                .Append(mAuctionHouseCode).Append(GlobalDefine.DELIMITER)
                .Append(mUserMemNum).Append(GlobalDefine.DELIMITER)
                .Append(mAuthToken).Append(GlobalDefine.DELIMITER)
                .Append(mChannel).Append(GlobalDefine.DELIMITER)
                .Append(mOS)
                .ToString();
        }

        public override string ToString()
        {
            return $"[ConnectionInfo] AuctionHouseCode={mAuctionHouseCode}, UserMemNum={mUserMemNum}, Channel={mChannel}, OS={mOS}";
        }

    }
}
