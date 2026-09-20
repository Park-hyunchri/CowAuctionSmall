using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CowAuctionSmall.NetProto.netty
{
    class GlobalDefine
    {
        /**
         * 소켓 통신 메시지 구분자
         */
        public static readonly char DELIMITER = '|';
        public static readonly string DELIMITER_REGEX = "\\|"; // 메시지를 split할 때 사용할 정규표현식

        /**
         * 네티 패킷 최대 크기 (기본값: 1024, 설정 파일에서 읽기 가능)
         */
        private static int _nettyMaxFrameLength = 1024;
        public static int NETTY_MAX_FRAME_LENGTH
        {
            get => _nettyMaxFrameLength;
            set
            {
                if (value >= 512 && value <= 8192) // 허용 범위 제한
                {
                    _nettyMaxFrameLength = value;
                    Console.WriteLine($"✅ NETTY_MAX_FRAME_LENGTH 설정됨: {value}");
                }
                else
                {
                    Console.WriteLine($"⚠️ 잘못된 프레임 크기 설정 시도 ({value}), 기본값 유지");
                }
            }
        }
        /**
         * 네티 정보
         */
        /// <summary>
        /// 설정 파일에서 `NETTY_MAX_FRAME_LENGTH` 값을 불러옴.
        /// </summary>
        public static void LoadSettings()
        {
            try
            {
                string maxFrameLengthStr = ConfigurationManager.AppSettings["NettyMaxFrameLength"];
                if (!string.IsNullOrEmpty(maxFrameLengthStr) && int.TryParse(maxFrameLengthStr, out int value))
                {
                    NETTY_MAX_FRAME_LENGTH = value; // 유효한 값이면 적용
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 설정 로드 실패: {ex.Message}");
            }
        }
    }
}
