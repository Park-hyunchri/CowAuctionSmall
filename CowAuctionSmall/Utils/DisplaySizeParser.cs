using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CowAuctionSmall.Utils
{
    /// <summary>
    /// 전광판 사이즈 문자열과 Enum 간의 변환을 담당하는 헬퍼 클래스
    /// </summary>
    public static class DisplaySizeParser
    {
        public enum DisplaySize
        {
            Unknown,
            Size128x64,
            Size128x128,
            Size160x64,
            Size320x64
        }

        /// <summary>
        /// 문자열을 DisplaySize Enum으로 변환합니다.
        /// 예: "128,64" → DisplaySize.Size128x64
        /// </summary>
        public static DisplaySize Parse(string size)
        {
            return size.Trim() switch
            {
                "128,64" => DisplaySize.Size128x64,
                "128,128" => DisplaySize.Size128x128,
                "160,64" => DisplaySize.Size160x64,
                "320,64" => DisplaySize.Size320x64,
                _ => DisplaySize.Unknown
            };
        }

        /// <summary>
        /// DisplaySize Enum을 원래 문자열로 변환합니다.
        /// 예: DisplaySize.Size128x64 → "128,64"
        /// </summary>
        public static string ToString(DisplaySize size)
        {
            return size switch
            {
                DisplaySize.Size128x64 => "128,64",
                DisplaySize.Size128x128 => "128,128",
                DisplaySize.Size160x64 => "160,64",
                DisplaySize.Size320x64 => "320,64",
                _ => "Unknown"
            };
        }
    }
}
