
using CowAuctionSmall.NetProto.netty;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CowAuctionSmall.NetProto.models
{
    [Serializable]
    public class ResponseConnectionInfo
    {
        public const char ORIGIN = 'A';
        public const char TYPE = 'R';

        private String mAuctionHouseCode; // 거점코드
        private String mUserMemNum; // 거래인관리번호
        private String mResult; // 결과코드
        private String mAuctionJoinNum; // 경매참가번호

        public ResponseConnectionInfo(String auctionHouseCode, String result, String userMemNum, String auctionJoinNum)
        {
            mAuctionHouseCode = auctionHouseCode;
            mUserMemNum = userMemNum;
            mResult = result;
            mAuctionJoinNum = auctionJoinNum;
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

        public String getResult()
        {
            return mResult;
        }

        public void setResult(String result)
        {
            this.mResult = result;
        }

        public String getAuctionJoinNum()
        {
            return mAuctionJoinNum;
        }

        public void setAuctionJoinNum(String auctionJoinNum)
        {
            this.mAuctionJoinNum = auctionJoinNum;
        }

        public String getEncodedMessage()
        {
            return String.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}", ORIGIN, TYPE, GlobalDefine.NETTY_INFO.DELIMITER, mAuctionHouseCode,
                    GlobalDefine.NETTY_INFO.DELIMITER, mResult, GlobalDefine.NETTY_INFO.DELIMITER, mUserMemNum,
                    GlobalDefine.NETTY_INFO.DELIMITER, mAuctionJoinNum);
        }

    }
}
