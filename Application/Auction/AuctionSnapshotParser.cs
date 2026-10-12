using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CowAuctionSmall.Application.Auction
{
    /// <summary>전체 응답의 형식 검사와 정렬을 담당한다. 외부 필드 해석은 파서 계약에 위임한다.</summary>
    public sealed class AuctionSnapshotParser
    {
        private readonly IAuctionPacketParser _parser;

        public AuctionSnapshotParser(IAuctionPacketParser parser) => _parser = parser;

        public gValues DecodePacket(string message, UserInfo user) => _parser.ParsePacket(message, user);

        public AuctionItemUpdate DecodeUpdate(string message, UserInfo user, bool isSv, gValues? previous = null)
            => _parser.ParseUpdate(message, user, isSv, previous);

        public List<gValues> Parse(List<string> raw, UserInfo user)
        {
            var parsed = new List<gValues>(raw.Count);
            string? snapshotHouse = null;
            string? snapshotRound = null;
            string? configuredHouse = user.Auction?.AuctionHouseCode;
            var entries = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in raw)
            {
                if (string.IsNullOrWhiteSpace(row) || row.Split('|').Length < 35)
                    throw new FormatException("Auction snapshot contains an incomplete row.");
                var value = DecodeUpdate(row, user, false).Values;
                if (value == null) throw new FormatException("Auction snapshot contains an invalid row.");
                if (!entries.Add(AuctionItemStore.Key(value.SipNumber)))
                    throw new FormatException("Auction snapshot contains a duplicate entry number.");
                if (!string.IsNullOrEmpty(value.AuctionHouseCode))
                {
                    if ((!string.IsNullOrEmpty(configuredHouse) && value.AuctionHouseCode != configuredHouse) ||
                        (snapshotHouse != null && value.AuctionHouseCode != snapshotHouse))
                        throw new FormatException("Auction snapshot contains a different auction house.");
                    snapshotHouse = value.AuctionHouseCode;
                }
                if (!string.IsNullOrEmpty(value.AuctionRound))
                {
                    if (snapshotRound != null && value.AuctionRound != snapshotRound)
                        throw new FormatException("Auction snapshot contains multiple auction rounds.");
                    snapshotRound = value.AuctionRound;
                }
                parsed.Add(value);
            }
            return parsed.OrderBy(value => value.EntityNumber).ToList();
        }
    }
}
