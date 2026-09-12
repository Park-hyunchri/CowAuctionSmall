using CowAuctionSmall.Models.Structures;

namespace GAMSTest.Services;

public sealed class MockDataFactory
{
    private readonly UserInfo _userInfo;

    public MockDataFactory(UserInfo userInfo) => _userInfo = userInfo;

    public gValues Create(int boardNumber, string status, bool running)
    {
        var auction = _userInfo.Auction;
        var remainder = boardNumber % 4;
        var sex = remainder switch { 0 => "암", 1 => "수", 2 => "거세", _ => "암소" };
        var distinction = remainder switch { 0 or 1 => "1", 2 => "2", _ => "3" };

        return new gValues
        {
            SpaceIndex = boardNumber.ToString(), SipNumber = boardNumber.ToString(),
            AuctionResultStatus = status, IsRunning = running, ProcessStatus = 8001,
            Sex = sex, CowDistinction = distinction,
            BidderName = auction?.BidderName ?? "Y", LowestPriceTitle = auction?.LowestPriceTitle ?? "최저가",
            Is_Nh_QQuri = auction?.IsShowQQuri ?? "Y", Is_Ｎh_Excellent = "Y", Is_Mother_Ｎh_Excellent = "Y",
            Note = "테스트 데이터 입니다", LowestPrice = "3,200", Weight = "450",
            EntityNumber = "002 1234 5678 9", EntityNumberShort = "5678", OwnerName = "홍길동", Location = "관내",
            Birth = "24.01.15(10개월령)", KPN = "1450", Blood = "3", CalvingNumber = "2",
            RegistrationCategory = "혈통", MotherLevel = "고등",
            Reproduction_Imsin_Sujung_Date = "24.05.10(120일)", Reproduction_Sujung_KPN = "1450",
            bodyWeightInColdNum = "19.24", bodyWeightInColdString = "A", Nh_ability_1_num = "19.24", Nh_ability_1_str = "A",
            longestMuscleCrossSectionNum = "3.39", longestMuscleCrossSectionString = "B", Nh_ability_2_num = "3.39", Nh_ability_2_str = "B",
            fatThicknessOnBackNum = "-0.69", fatThicknessOnBackString = "C", Nh_ability_3_num = "-0.69", Nh_ability_3_str = "C",
            intramuscularFatContentNum = "0.51", intramuscularFatContentString = "D", Nh_ability_4_num = "0.51", Nh_ability_4_str = "D",
            BidPrice = status == "22" ? "1,200,000" : "-", Bidder = status == "22" ? "301" : "-",
            BidderNum = status == "22" ? "301" : "", BidderString = status == "22" ? "김낙찰" : ""
        };
    }
}
