using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CowAuctionSmall.Models.Structures
{

    public class UserInfo
    {
        public Authentication? Authentication { get; set; } = null;
        public CurrentInfo? CurrentInfo { get; set; } = null;
        public Auction? Auction { get; set; } = null;

    }
    /// <summary>
    /// 토큰 만들 때 or 접속할 때
    /// </summary>
    public class Authentication
    {
        public string? Address { get; set; } = "";
        public string? UserID { get; set; } = "";
        public string? Password { get; set; } = "";
    }
    /// <summary>
    /// Address = 오늘 경매하는 경매 품목 API
    /// AddressEPD = 오늘 경매하는 경매 품목의 유전능력 API
    /// Date = 테스트용으로 쓰일 것 ex) 20240402
    /// </summary>
    public class CurrentInfo
    {
        public string? Address { get; set; } = "";
        public string? AddressEPD { get; set; } = "";
        public string? Date { get; set; } = "";
    }

    /// <summary>
    /// 옵션 사항
    /// </summary>
    public class Auction
    {
        public string? Address { get; set; } = "";
        public string? Port { get; set; } = "";
        public string? Channel { get; set; } = "";
        public string? Priority { get; set; } = "";
        public string? AuctionHouseCode { get; set; } = "";
        public string? StartPosition { get; set; } = "";
        public string? CowBirth { get; set; } = "";
        public string? BoardPage { get; set; } = "1";
        public string? BoardPageTime { get; set; } = "";
        public string? BoardPageTime2 { get; set; } = "";
        public string? BoardPageTime3 { get; set; } = "";
        public string? BidderName { get; set; } = "";
        public string? IsShowOwnerName { get; set; } = "";
        public string? ChangeSexName { get; set; } = "Y";
        public string? IsGoatAuction { get; set; } = "";
    }
}
