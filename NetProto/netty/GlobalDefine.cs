using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CowAuctionSmall.NetProto.netty
{
    class GlobalDefine
    {
        /**
         * 네티 정보
         */
        public static class NETTY_INFO
        {
            public static char DELIMITER = '|'; // 소켓 통신 메시지 구분자
            public static String DELIMITER_REGEX = "\\|"; // 소켓 통신 메시지를 split할 때 사용할 구분자의 정규표현식
            public static int NETTY_MAX_FRAME_LENGTH = 1024; // 네티 패킷 사이즈
        }
    }
}
