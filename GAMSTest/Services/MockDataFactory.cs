using CowAuctionSmall.Models.Structures;

namespace GAMSTest.Services;

public sealed class MockDataFactory
{
    private readonly UserInfo _userInfo;

    public MockDataFactory(UserInfo userInfo) => _userInfo = userInfo;

    public gValues Create(int boardNumber, string status, bool running)
    {
        var auction = _userInfo.Auction;
        return new gValues
        {
            SpaceIndex = boardNumber.ToString(), SipNumber = boardNumber.ToString(),
            AuctionResultStatus = status, IsRunning = running,
            BidderName = auction?.BidderName ?? "Y", LowestPriceTitle = auction?.LowestPriceTitle ?? "최저가",
            Is_Nh_QQuri = auction?.IsShowQQuri ?? "Y", Nh_ability_1_num = "100", Nh_ability_1_str = "A",
            Nh_ability_2_num = "90", Nh_ability_2_str = "B", Nh_ability_3_num = "80", Nh_ability_3_str = "A",
            Nh_ability_4_num = "70", Nh_ability_4_str = "B", Is_Ｎh_Excellent = "Y",
            Is_Mother_Ｎh_Excellent = "Y", Bidder = "301", BidderNum = "301", BidderString = "301",
            BidPrice = "1,200,000", CowDistinction = "2", EntityNumber = "000000000000"
        };
    }
}
