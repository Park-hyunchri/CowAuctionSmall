using Newtonsoft.Json.Linq;
using System.Linq;

namespace CowAuctionSmall.Infra.Api
{
    public sealed class LoginResult
    {
        public string AccessToken { get; }
        public string AuctionHouseCode { get; }

        private LoginResult(string accessToken, string auctionHouseCode)
        {
            AccessToken = accessToken;
            AuctionHouseCode = auctionHouseCode;
        }

        public static LoginResult? FromResponse(JObject response)
        {
            if (response["success"]?.Type != JTokenType.Boolean || response["success"]!.Value<bool>() != true ||
                response["accessToken"]?.Type != JTokenType.String || response["naBzplc"]?.Type != JTokenType.String)
            {
                return null;
            }

            string? token = response["accessToken"]?.Value<string>();
            string? houseCode = response["naBzplc"]?.Value<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(houseCode) ||
                houseCode.Any(character => character < '0' || character > '9'))
            {
                return null;
            }

            return new LoginResult(token, houseCode);
        }
    }
}
