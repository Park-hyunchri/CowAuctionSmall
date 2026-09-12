using System.Windows;
using CowAuctionSmall.Models.Structures;

namespace GAMSTest.ViewModels;

public sealed class MockPanelViewModel
{
    public gValues CowInfo { get; }
    public string Note => CowInfo.Note ?? "테스트 데이터 입니다";
    public string SexDisc => CowInfo.Sex.Length < 2 ? $"{CowInfo.Sex} {CowInfo.StrCowDistinction}" : CowInfo.StrCowDistinction;
    public bool IsRunning => CowInfo.IsRunning;
    public bool HasPaternityMatch => !string.IsNullOrWhiteSpace(CowInfo.PaternityMatch) && CowInfo.PaternityMatch != "-";
    public Thickness NoteMargin => HasPaternityMatch ? new Thickness(32, 110, 0, 1) : new Thickness(2, 110, 0, 1);
    public string LowestPrice => CowInfo.LowestPrice;
    public string Weight => CowInfo.Weight;
    public string BidPrice => CowInfo.BidPrice;
    public string Bidder => CowInfo.Bidder;
    public string BidderNum => CowInfo.BidderNum;
    public string BidderString => CowInfo.BidderString;
    public string BidderName => CowInfo.BidderName;
    public string StrCowDistinction => CowInfo.StrCowDistinction;
    public string EntityNumber => CowInfo.EntityNumber;
    public string EntityNumberShort => CowInfo.EntityNumberShort;
    public string Location => CowInfo.Location;
    public string OwnerName => CowInfo.OwnerName;
    public string Birth => CowInfo.Birth;
    public string BirthMonth => CowInfo.BirthMonth;
    public string KPN => CowInfo.KPN;
    public string Blood => CowInfo.Blood;
    public string CalvingNumber => CowInfo.CalvingNumber;
    public string RegistrationCategory => CowInfo.RegistrationCategory;
    public string bodyWeightInColdNum => CowInfo.bodyWeightInColdNum;
    public string Reproduction_Imsin_Sujung_Date => CowInfo.Reproduction_Imsin_Sujung_Date;
    public string Reproduction_Sujung_KPN => CowInfo.Reproduction_Sujung_KPN;
    public MockPanelViewModel(gValues cowInfo) => CowInfo = cowInfo;
}
