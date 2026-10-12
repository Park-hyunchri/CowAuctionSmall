using System;

namespace CowAuctionSmall.Application.Auction
{
    public enum AuctionInputKind { Other, Method, Started, Completed, Result, Refresh, Finish, DisplayDate }

    /// <summary>프로토콜 배열은 복제하고 큐 경계에서 경매 이벤트의 의미를 확정한다.</summary>
    public sealed class AuctionInput
    {
        public AuctionInputKind Kind { get; }
        private readonly string[] _fields;
        public bool InvalidatesRequests => Kind != AuctionInputKind.Other;
        public string[] CopyFields() => (string[])_fields.Clone();

        public AuctionInput(string[] fields)
        {
            ArgumentNullException.ThrowIfNull(fields);
            _fields = (string[])fields.Clone();
            Kind = _fields.Length == 0 ? AuctionInputKind.Other : _fields[0] switch
            {
                "AT" => AuctionInputKind.Method,
                "AF" => AuctionInputKind.Result,
                "SZ" when _fields.Length > 5 && (_fields[5]?.ToUpperInvariant() is "P" or "Y" or "N") => AuctionInputKind.DisplayDate,
                "AS" when _fields.Length > 6 => _fields[6] switch
                {
                    "8001" => AuctionInputKind.Refresh,
                    "8003" => AuctionInputKind.Started,
                    "8004" => AuctionInputKind.Started,
                    "8006" => AuctionInputKind.Completed,
                    "8007" => AuctionInputKind.Finish,
                    _ => AuctionInputKind.Other
                },
                _ => AuctionInputKind.Other
            };
        }
    }
}
