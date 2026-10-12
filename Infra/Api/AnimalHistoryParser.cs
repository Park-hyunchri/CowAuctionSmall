using CowAuctionSmall.Domain.AnimalHistory;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CowAuctionSmall.Infra.Api
{
    public static class AnimalHistoryParser
    {
        public static AnimalHistoryResult ParseAnimalHistory(string xml)
        {
            try
            {
                using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1024 * 1024
                });
                var root = XDocument.Load(reader);
                string Read(XContainer node, string name) => node.Descendants().FirstOrDefault(e => e.Name.LocalName == name)?.Value.Trim() ?? "";
                string code = Read(root, "resultCode");
                if (code.Length == 0) code = Read(root, "returnReasonCode");
                if (code != "00")
                    return new AnimalHistoryResult { Status = code == "22" ? HistoryStatus.QuotaExceeded :
                        new[] { "20", "21", "30", "31", "32" }.Contains(code) ? HistoryStatus.AuthenticationError : HistoryStatus.InvalidResponse };
                if (!root.Descendants().Any(e => e.Name.LocalName == "items"))
                    return new AnimalHistoryResult { Status = HistoryStatus.InvalidResponse };
                var result = new AnimalHistoryResult();
                foreach (var item in root.Descendants().Where(e => e.Name.LocalName == "item"))
                {
                    if (Read(item, "infoType") != "2") return new AnimalHistoryResult { Status = HistoryStatus.InvalidResponse };
                    string date = Read(item, "regYmd"), type = Read(item, "regType"), owner = Read(item, "farmerNm");
                    string address = Read(item, "farmAddr");
                    if (!DateTime.TryParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
                        type.Length == 0 || owner.Length == 0)
                        return new AnimalHistoryResult { Status = HistoryStatus.InvalidResponse };
                    if (!result.Entries.Any(e => e.Date == date && e.Type == type && e.Owner == owner && e.FarmAddress == address))
                        result.Entries.Add(new AnimalHistoryEntry { Date = date, Type = type, Owner = owner, FarmAddress = address });
                }
                result.Entries = result.Entries.OrderBy(e => e.Date, StringComparer.Ordinal).ToList();
                return result;
            }
            catch (Exception ex) when (ex is XmlException || ex is ArgumentException)
            {
                return new AnimalHistoryResult { Status = HistoryStatus.InvalidResponse };
            }
        }
    }
}
