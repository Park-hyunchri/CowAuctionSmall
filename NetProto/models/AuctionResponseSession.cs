using CowAuctionSmall.NetProto.netty;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CowAuctionSmall.NetProto.models
{

    [Serializable]
    class AuctionReponseSession
    {
        //private static long serialVersionUID = 4703227204913480226L;

        public static char ORIGIN = 'A';
        public static char TYPE = 'A';

        private String mUserNo; // 회원(사원)번호
        private String mChannel; // 접속 요청 채널
        private String mOS; // 사용 채널

        public AuctionReponseSession(String userNo, String channel, String os)
        {
            mUserNo = userNo;
            mChannel = channel;
            mOS = os;
        }

        public String getUserNo()
        {
            return mUserNo;
        }

        public void setUserNo(String userNo)
        {
            this.mUserNo = userNo;
        }

        public String getChannel()
        {
            return mChannel;
        }

        public void setChannel(String channel)
        {
            this.mChannel = channel;
        }

        public String getOS()
        {
            return mOS;
        }

        public void setOS(String os)
        {
            this.mOS = os;
        }

        public String getEncodedMessage()
        {
            return String.Format("{0}{1}{2}{3}{4}{5}{6}{7}", ORIGIN, TYPE, GlobalDefine.NETTY_INFO.DELIMITER, mUserNo, GlobalDefine.NETTY_INFO.DELIMITER, mChannel, GlobalDefine.NETTY_INFO.DELIMITER, mOS);
        }
    }
}
