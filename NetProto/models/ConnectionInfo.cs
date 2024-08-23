
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

        private String mAuctionHouseCode; // 조합구분코드
        private String mUserMemNum; // 거래인관리번호
        private String mAuthToken; // 인증토큰
        private String mChannel; // 접속 요청 채널
        private String mOS; // 사용 채널
        private String mAuctionJoinNum; // 경매참가번호(패킷데이터에서 제외)


        public ConnectionInfo(String auctionHouseCode, String userMemNum, String authToken, String channel, String os)
        {
            mAuctionHouseCode = auctionHouseCode;
            mUserMemNum = userMemNum;
            mAuthToken = authToken;
            mChannel = channel;
            mOS = os;
        }

        public Boolean equals(Object obj)
        {
            return ((ConnectionInfo)obj).mUserMemNum.Equals(mUserMemNum);
        }

        public String getAuctionHouseCode()
        {
            return mAuctionHouseCode;
        }

        public void setAuctionHouseCode(String auctionHouseCode)
        {
            this.mAuctionHouseCode = auctionHouseCode;
        }

        public String getUserMemNum()
        {
            return mUserMemNum;
        }

        public void setUserMemNum(String userMemNum)
        {
            this.mUserMemNum = userMemNum;
        }

        public String getAuthToken()
        {
            return mAuthToken;
        }

        public void setAuthToken(String authToken)
        {
            this.mAuthToken = authToken;
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

        public String getAuctionJoinNum()
        {
            return this.mAuctionJoinNum;
        }

        public void setAuctionJoinNum(String auctionJoinNum)
        {
            mAuctionJoinNum = auctionJoinNum;
        }

        public String getEncodedMessage()
        {
            return String.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}", ORIGIN, TYPE, GlobalDefine.NETTY_INFO.DELIMITER, mAuctionHouseCode,
                    GlobalDefine.NETTY_INFO.DELIMITER, mUserMemNum, GlobalDefine.NETTY_INFO.DELIMITER, mAuthToken,
                    GlobalDefine.NETTY_INFO.DELIMITER, mChannel, GlobalDefine.NETTY_INFO.DELIMITER, mOS);
        }
    }
}
