using CowAuctionSmall.NetProto.netty;
using System;
using System.Linq;
using System.Text;

namespace CowAuctionSmall.NetProto.models
{

    [Serializable]
    class AuctionReponseSession
    {
        //private static long serialVersionUID = 4703227204913480226L;

        public static char ORIGIN = 'A';
        public static char TYPE = 'A';

        public string mUserNo { get; private set; } //회원(사원)번호
        public string mChannel { get; private set; }// 접속 요청 채널
        public string mOS { get; private set; } // 사용 채널

        public AuctionReponseSession(string userNo, string channel, string os)
        {
            mUserNo = userNo ?? "UNKNOWN";
            mChannel = channel ?? "DEFAULT";
            mOS = os ?? "UNKNOWN";
        }

        public string getEncodedMessage()
        {
            return new StringBuilder()
                .Append(ORIGIN).Append(TYPE).Append(GlobalDefine.DELIMITER)
                .Append(mUserNo).Append(GlobalDefine.DELIMITER)
                .Append(mChannel).Append(GlobalDefine.DELIMITER)
                .Append(mOS)
                .ToString();
        }

        public override string ToString()
        {
            return $"[AuctionReponseSession] UserNo={mUserNo}, Channel={mChannel}, OS={mOS}";
        }
    }
}
