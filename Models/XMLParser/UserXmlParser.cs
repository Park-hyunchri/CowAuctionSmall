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

            var authElement = xmlDoc.Root.Element("Authentication");
            if (authElement != null)
            {
                userInfo.Authentication = new Authentication
                {
                    Address = authElement.Element("Address")?.Value,
                    UserID = authElement.Element("UserID")?.Value,
                    Password = authElement.Element("Password")?.Value
                };
            }

            var currentInfoElement = xmlDoc.Root.Element("CurrentInfo");
            if (currentInfoElement != null)
            {
                userInfo.CurrentInfo = new CurrentInfo
                {
                    Address = currentInfoElement.Element("Address")?.Value,
                    AddressEPD = currentInfoElement.Element("AddressEPD")?.Value,
                    Date = currentInfoElement.Element("Date")?.Value
                };
            }

            var auctionElement = xmlDoc.Root.Element("Auction");
            if (auctionElement != null)
            {
                userInfo.Auction = new Auction
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
                    BidderName = auctionElement.Element("BidderName")?.Value,
                    IsShowOwnerName = auctionElement.Element("IsShowOwnerName")?.Value,
                    ChangeSexName = auctionElement.Element("ChangeSexName")?.Value,
                    IsGoatAuction = auctionElement.Element("IsGoatAuction")?.Value
                    
                };
            }

            return userInfo;
        }
    }
}
