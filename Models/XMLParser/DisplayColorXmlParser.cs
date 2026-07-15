using System.IO;
using System.Xml.Linq;
using CowAuctionSmall.Models;
using CowAuctionSmall.Models.Structures;

namespace CowAuctionSmall.Models.XMLParser
{
    public class DisplayColorXmlParser
    {
        public DisplayColorSettings ParseXml(string xmlFilePath)
        {
            var logger = NLogger.Instance;
            var settings = new DisplayColorSettings();

            if (!File.Exists(xmlFilePath))
            {
                logger.LogWarn($"DisplayColorXmlParser: 파일이 없습니다. 기본 색상을 사용합니다. ({xmlFilePath})");
                return settings;
            }

            XDocument xmlDoc = XDocument.Load(xmlFilePath);
            var root = xmlDoc.Root;
            if (root == null)
            {
                logger.LogWarn($"DisplayColorXmlParser: Root element가 없습니다. 기본 색상을 사용합니다. ({xmlFilePath})");
                return settings;
            }

            var entityNumberElement = root.Element("EntityNumber");
            if (entityNumberElement != null)
            {
                settings.EntityNumberForeground = entityNumberElement.Attribute("Foreground")?.Value;
            }

            var entityNumberShortElement = root.Element("EntityNumberShort");
            if (entityNumberShortElement != null)
            {
                settings.EntityNumberShortForeground = entityNumberShortElement.Attribute("Foreground")?.Value;
                settings.EntityNumberShortBackground = entityNumberShortElement.Attribute("Background")?.Value;
            }

            return settings;
        }
    }
}
