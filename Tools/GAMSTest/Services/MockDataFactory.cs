using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.AnimalHistory;
using GAMSTest.Models;

namespace GAMSTest.Services;

public static class MockDataFactory
{
    public static gValues Create(int boardNumber, TestSettings settings, SampleKind sample)
    {
        var empty = sample == SampleKind.Empty;
        var longName = sample == SampleKind.LongNames;
        var owner = longName ? "가상출하농장긴이름점검" : "가상농장";
        if (settings.MaskOwner || settings.IsShowOwnerName == "N") owner = owner[..1] + "*" + owner[2];
        else if (settings.IsShowOwnerName == "Y") owner = owner[..Math.Min(3, owner.Length)];
        var bidder = longName ? "가상낙찰농장긴이름점검" : "가상낙찰자";
        var value = new gValues
        {
            SpaceIndex = boardNumber.ToString(), SipNumber = boardNumber.ToString(),
            CowDistinction = settings.CowDistinction, LowestPriceTitle = settings.LowestPriceTitle,
            EntityNumber = empty ? "" : "1234 5678 9", MatherEntityNumber = "9876 5432 1",
            OwnerName = empty ? "" : owner, Location = empty ? "" : "가상지역",
            BidderName = settings.BidderName, BidderNum = empty ? "" : "112", BidderString = empty ? "" : bidder,
            Bidder = empty ? "" : settings.BidderName == "N" ? "112" : settings.BidderName == "X" ? "가*낙" : bidder,
            BidPrice = empty ? "" : "1234", LowestPrice = empty ? "" : "1050", Weight = empty ? "" : "312",
            Sex = empty ? "" : sample == SampleKind.ThreeCharacterSex ? "비거세" : settings.ChangeSexName == "Y" && settings.CowDistinction == "2" ? "비육" : settings.ChangeSexName == "Y" && settings.CowDistinction == "3" ? "암소" : "암",
            Birth = empty ? "" : settings.CowBirth == "N" ? "19개월" : "25.02.02", BirthMonth = empty ? "" : "19개월",
            RegistrationCategory = empty ? "" : "혈통", MotherLevel = "혈통", Blood = "3",
            KPN = empty ? "" : "KPN1234", Pregnant = "3", CalvingNumber = "2",
            Note = empty ? "" : "가상 자료입니다. 긴 비고 흐름과 글자 겹침을 확인합니다.",
            NoteChangeOwner = empty ? "" : "가상 이력 1회", PaternityMatch = empty || settings.IsPaternityMatch != "Y" ? "" : "친자",
            FrontNoteWord = empty || settings.IsPaternityMatch != "Y" ? "" : "친자", FrontNoteWordBrush = settings.HouseCode == "8808990817675" ? "Lime" : "Yellow",
            SelectShowWeight_EPD = settings.SelectShowWeightEpd,
            HistoryOwner = empty ? null : new AnimalHistoryOwner("가상소유주", "전라남도 가상군 가상면 점검로 123"),
            Is_Nh_QQuri = settings.Qquri, Is_Ｎh_Excellent = settings.NhExcellent,
            Is_Mother_Ｎh_Excellent = settings.MotherNhExcellent,
            bodyWeightInColdNum = "12.34", bodyWeightInColdString = "A",
            longestMuscleCrossSectionNum = "5.67", longestMuscleCrossSectionString = "B",
            fatThicknessOnBackNum = "-1.23", fatThicknessOnBackString = "A",
            intramuscularFatContentNum = "0.89", intramuscularFatContentString = "B",
            Nh_ability_1_num = empty ? "" : settings.AbilityNumber, Nh_ability_1_str = "A", Nh_ability_2_num = "5.67", Nh_ability_2_str = "B",
            Nh_ability_3_num = "-1.23", Nh_ability_3_str = "A", Nh_ability_4_num = "0.89", Nh_ability_4_str = "B"
        };
        return value;
    }
}
