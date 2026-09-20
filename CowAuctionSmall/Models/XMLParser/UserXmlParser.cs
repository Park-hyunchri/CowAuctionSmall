using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CowAuctionSmall.Models.Structures;

namespace CowAuctionSmall.Models.XMLParser
{
    public class UserXmlParser
    {
        public UserInfo ParseXml(string xmlFilePath)
        {
            UserInfo userInfo = new UserInfo();

            XDocument xmlDoc = XDocument.Load(xmlFilePath);
            var root = xmlDoc.Root;
            if (root == null)
            {
                return userInfo;
            }

            var authElement = root.Element("Authentication");
            if (authElement != null)
            {
                userInfo.Authentication = new Authentication
                {
                    Address = authElement.Element("Address")?.Value,
                    UserID = authElement.Element("UserID")?.Value,
                    Password = authElement.Element("Password")?.Value
                };
            }

            var currentInfoElement = root.Element("CurrentInfo");
            if (currentInfoElement != null)
            {
                userInfo.CurrentInfo = new CurrentInfo
                {
                    Address = currentInfoElement.Element("Address")?.Value,
                    AddressEPD = currentInfoElement.Element("AddressEPD")?.Value,
                    AddressQcn = currentInfoElement.Element("AddressQcn")?.Value,
                    Date = currentInfoElement.Element("Date")?.Value
                };
            }

            var auctionElement = root.Element("Auction");
            if (auctionElement != null)
            {
                var auction = new Auction
                {
                    Address = auctionElement.Element("Address")?.Value,
                    Port = auctionElement.Element("Port")?.Value,
                    Channel = auctionElement.Element("Channel")?.Value,
                    Priority = auctionElement.Element("Priority")?.Value,
                    AuctionHouseCode = auctionElement.Element("AuctionHouseCode")?.Value,
                    StartPosition = auctionElement.Element("StartPosition")?.Value,
                    CowBirth = auctionElement.Element("CowBirth")?.Value,
                    BoardPage = auctionElement.Element("BoardPage")?.Value,
                    BoardPageTime = auctionElement.Element("BoardPageTime")?.Value,
                    BoardPageTime2 = auctionElement.Element("BoardPageTime2")?.Value,
                    BoardPageTime3 = auctionElement.Element("BoardPageTime3")?.Value,
                    BoardPageTime4 = auctionElement.Element("BoardPageTime4")?.Value,
                    BidderName = auctionElement.Element("BidderName")?.Value,
                    IsShowOwnerName = auctionElement.Element("IsShowOwnerName")?.Value,
                    IsPaternityMatch = auctionElement.Element("IsPaternityMatch")?.Value ?? "Y", // 💡 추가
                    ChangeSexName = auctionElement.Element("ChangeSexName")?.Value,
                    SelectShowWeight_EPD = auctionElement.Element("SelectShowWeight_EPD")?.Value,
                    IsShowQQuri = auctionElement.Element("IsShowQQuri")?.Value,
                    PageTimerPort = auctionElement.Element("PageTimerPort")?.Value,
                    EnableSubFallback = auctionElement.Element("EnableSubFallback")?.Value,
                    SubFallbackTimeoutMs = auctionElement.Element("SubFallbackTimeoutMs")?.Value,
                    // 💡 [추가] 색상 태그 읽기
                    EntityNumberForeground = auctionElement.Element("EntityNumberForeground")?.Value,
                    EntityNumberShortForeground = auctionElement.Element("EntityNumberShortForeground")?.Value,
                    EntityNumberShortBackground = auctionElement.Element("EntityNumberShortBackground")?.Value,
                    // 💡 [신규 추가] users.XML 태그 파싱 (기본값 설정)
                    LocationForeground = auctionElement.Element("LocationForeground")?.Value ?? "lime",
                    EpdGradeForeground = auctionElement.Element("EpdGradeForeground")?.Value ?? "Yellow",
                    // 💡 users.xml에서 LowestPriceTitle 읽기 (없을 경우 기본값 "내정가")
                    LowestPriceTitle = auctionElement.Element("LowestPriceTitle")?.Value ?? "최저가",
                };

                var pageSettingElement = auctionElement.Element("PageSetting") ?? root.Element("PageSetting");
                if (pageSettingElement != null)
                {
                    auction.PageSetting = new PageSetting
                    {
                        PageTimerPort = pageSettingElement.Element("PageTimerPort")?.Value,
                        BoardPage = pageSettingElement.Element("BoardPage")?.Value,
                        BoardPageTime = pageSettingElement.Element("BoardPageTime")?.Value,
                        BoardPageTime2 = pageSettingElement.Element("BoardPageTime2")?.Value,
                        BoardPageTime3 = pageSettingElement.Element("BoardPageTime3")?.Value,
                        BoardPageTime4 = pageSettingElement.Element("BoardPageTime4")?.Value,
                        EnableSubFallback = pageSettingElement.Element("EnableSubFallback")?.Value,
                        SubFallbackTimeoutMs = pageSettingElement.Element("SubFallbackTimeoutMs")?.Value
                    };
                }

                userInfo.Auction = auction;
            }

            return userInfo;
        }
    }
}
