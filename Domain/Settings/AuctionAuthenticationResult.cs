namespace CowAuctionSmall.Domain.Settings
{
    public sealed class AuctionAuthenticationResult
    {
        public string AccessToken { get; }
        public string AuctionHouseCode { get; }

        public AuctionAuthenticationResult(string accessToken, string auctionHouseCode)
        {
            AccessToken = accessToken;
            AuctionHouseCode = auctionHouseCode;
        }
    }
}
