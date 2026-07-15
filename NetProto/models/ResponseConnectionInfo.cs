
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

        public string mAuctionHouseCode { get; private set; } //축협코드
        public string mUserMemNum { get; private set; } //사용자 회원번호
        public string mResult { get; private set; } //결과
        public string mAuctionJoinNum { get; private set; } //경매 참여 번호

        public ResponseConnectionInfo(string auctionHouseCode, string result, string userMemNum, string auctionJoinNum)
        {
            mAuctionHouseCode = auctionHouseCode ?? "UNKNOWN";
            mResult = result ?? "FAIL";
            mUserMemNum = userMemNum ?? "UNKNOWN";
            mAuctionJoinNum = auctionJoinNum ?? "0";
        }

        /// <summary>
        /// 문자열로 인코딩된 메시지 반환
        /// </summary>
        public string getEncodedMessage()
        {
            return new StringBuilder()
                .Append(ORIGIN).Append(TYPE).Append(GlobalDefine.DELIMITER)
                .Append(mAuctionHouseCode).Append(GlobalDefine.DELIMITER)
                .Append(mResult).Append(GlobalDefine.DELIMITER)
                .Append(mUserMemNum).Append(GlobalDefine.DELIMITER)
                .Append(mAuctionJoinNum)
                .ToString();
        }

        public override string ToString()
        {
            return $"[ResponseConnectionInfo] AuctionHouseCode={mAuctionHouseCode}, Result={mResult}, UserMemNum={mUserMemNum}, AuctionJoinNum={mAuctionJoinNum}";
        }

        public String getResult()
        {
            return mResult;
        }

    }
}
