using System.Windows;
using System.Windows.Media;
using CowAuctionSmall.Models.Structures;

namespace GAMSTest.ViewModels;

public sealed class MockPanelViewModel
{
    public gValues? CowInfo { get; set; }
    public string Sex { get => CowInfo?.Sex ?? "-"; set { if (CowInfo != null) CowInfo.Sex = value; } }
    public string CowDistinction { get => CowInfo?.CowDistinction ?? "1"; set { if (CowInfo != null) CowInfo.CowDistinction = value; } }
    public string SelectShowWeight_EPD { get => CowInfo?.SelectShowWeight_EPD ?? "Y"; set { if (CowInfo != null) CowInfo.SelectShowWeight_EPD = value; } }
    public string Note { get => CowInfo?.Note ?? "테스트 데이터 입니다"; set { if (CowInfo != null) CowInfo.Note = value; } }
    public string SexDisc { get { if (CowInfo == null) return "-"; var sex = CowInfo.Sex ?? string.Empty; var dist = CowInfo.StrCowDistinction ?? string.Empty; return sex.Length < 2 ? $"{sex} {dist}".Trim() : dist; } set { } }
    public bool IsRunning { get => CowInfo?.IsRunning ?? false; set { if (CowInfo != null) CowInfo.IsRunning = value; } }
    public bool HasPaternityMatch { get => CowInfo?.PaternityMatch == "일치" || CowInfo?.FrontNoteWord == "일치"; set { } }
    public string FrontNoteWord { get => CowInfo?.FrontNoteWord ?? (HasPaternityMatch ? "일치" : string.Empty); set { if (CowInfo != null) CowInfo.FrontNoteWord = value; } }
    public Brush FrontNoteWordBrush { get => (Brush)new BrushConverter().ConvertFromString(CowInfo?.FrontNoteWordBrush ?? "Lime")!; set { } }
    public Thickness NoteMargin { get => HasPaternityMatch ? new Thickness(32, 110, 0, 1) : new Thickness(2, 110, 0, 1); set { } }
    public string LowestPrice { get => CowInfo?.LowestPrice ?? string.Empty; set { if (CowInfo != null) CowInfo.LowestPrice = value; } }
    public string Weight { get => CowInfo?.Weight ?? "0"; set { if (CowInfo != null) CowInfo.Weight = value; } }
    public string BidPrice { get => CowInfo?.BidPrice ?? string.Empty; set { if (CowInfo != null) CowInfo.BidPrice = value; } }
    public string Bidder { get => CowInfo?.Bidder ?? string.Empty; set { if (CowInfo != null) CowInfo.Bidder = value; } }
    public string BidderNum { get => CowInfo?.BidderNum ?? string.Empty; set { if (CowInfo != null) CowInfo.BidderNum = value; } }
    public string BidderString { get => CowInfo?.BidderString ?? string.Empty; set { if (CowInfo != null) CowInfo.BidderString = value; } }
    public string BidderName { get => CowInfo?.BidderName ?? string.Empty; set { if (CowInfo != null) CowInfo.BidderName = value; } }
    public string StrCowDistinction { get => CowInfo?.StrCowDistinction ?? string.Empty; set { } }
    public string EntityNumber { get => CowInfo?.EntityNumber ?? string.Empty; set { if (CowInfo != null) CowInfo.EntityNumber = value; } }
    public string EntityNumberShort { get => CowInfo?.EntityNumberShort ?? string.Empty; set { if (CowInfo != null) CowInfo.EntityNumberShort = value; } }
    public string Location { get => CowInfo?.Location ?? string.Empty; set { if (CowInfo != null) CowInfo.Location = value; } }
    public string OwnerName { get => CowInfo?.OwnerName ?? string.Empty; set { if (CowInfo != null) CowInfo.OwnerName = value; } }
    public string Birth { get => CowInfo?.Birth ?? string.Empty; set { if (CowInfo != null) CowInfo.Birth = value; } }
    public string BirthMonth { get => CowInfo?.BirthMonth ?? string.Empty; set { if (CowInfo != null) CowInfo.BirthMonth = value; } }
    public string Pregnant { get => CowInfo?.Pregnant ?? "-"; set { if (CowInfo != null) CowInfo.Pregnant = value; } }
    public string KPN { get => CowInfo?.KPN ?? string.Empty; set { if (CowInfo != null) CowInfo.KPN = value; } }
    public string Blood { get => CowInfo?.Blood ?? string.Empty; set { if (CowInfo != null) CowInfo.Blood = value; } }
    public string CalvingNumber { get => CowInfo?.CalvingNumber ?? string.Empty; set { if (CowInfo != null) CowInfo.CalvingNumber = value; } }
    public string RegistrationCategory { get => CowInfo?.RegistrationCategory ?? string.Empty; set { if (CowInfo != null) CowInfo.RegistrationCategory = value; } }
    public gValues CopyGValues { get => CowInfo ?? new(); set { if (value != null) CowInfo = value; } }
    public string bodyWeightInColdNum { get => CowInfo?.bodyWeightInColdNum ?? string.Empty; set { if (CowInfo != null) CowInfo.bodyWeightInColdNum = value; } }
    public string Reproduction_Imsin_Sujung_Date { get => CowInfo?.Reproduction_Imsin_Sujung_Date ?? string.Empty; set { if (CowInfo != null) CowInfo.Reproduction_Imsin_Sujung_Date = value; } }
    public string Reproduction_Sujung_KPN { get => CowInfo?.Reproduction_Sujung_KPN ?? string.Empty; set { if (CowInfo != null) CowInfo.Reproduction_Sujung_KPN = value; } }
    public string Is_strＮh_Excellent { get => CowInfo?.AuctionResultStatus == "23" || CowInfo?.Is_Nh_QQuri == "N" || CowInfo?.Is_Nh_QQuri == "X" ? string.Empty : CowInfo?.Is_strＮh_Excellent ?? string.Empty; set { } }
    public string Is_strNh_Excellent { get => Is_strＮh_Excellent; set { } }
    public string Is_Ｎh_Excellent { get => CowInfo?.AuctionResultStatus == "23" ? "N" : CowInfo?.Is_Ｎh_Excellent ?? "N"; set { if (CowInfo != null) CowInfo.Is_Ｎh_Excellent = value; } }
    public string Is_Mother_Ｎh_Excellent { get => CowInfo?.AuctionResultStatus == "23" ? "N" : CowInfo?.Is_Mother_Ｎh_Excellent ?? "N"; set { if (CowInfo != null) CowInfo.Is_Mother_Ｎh_Excellent = value; } }
    public string Is_Nh_QQuri { get => CowInfo?.Is_Nh_QQuri ?? string.Empty; set { if (CowInfo != null) CowInfo.Is_Nh_QQuri = value; } }
    public string Nh_ability_Str { get => CowInfo?.AuctionResultStatus == "23" || CowInfo?.Is_Nh_QQuri == "N" ? string.Empty : CowInfo?.Nh_ability_Str ?? string.Empty; set { if (CowInfo != null) CowInfo.Nh_ability_Str = value; } }
    public string Nh_ability_1_num { get => CowInfo?.AuctionResultStatus == "23" || CowInfo?.Is_Nh_QQuri == "N" ? string.Empty : CowInfo?.Nh_ability_1_num ?? string.Empty; set { if (CowInfo != null) CowInfo.Nh_ability_1_num = value; } }
    public string Nh_ability_1_str { get => CowInfo?.AuctionResultStatus == "23" || CowInfo?.Is_Nh_QQuri == "N" ? string.Empty : CowInfo?.Nh_ability_1_str ?? string.Empty; set { if (CowInfo != null) CowInfo.Nh_ability_1_str = value; } }
    public string bodyWeightInColdString { get => CowInfo?.bodyWeightInColdString ?? "A"; set { if (CowInfo != null) CowInfo.bodyWeightInColdString = value; } }
    public string longestMuscleCrossSectionString { get => CowInfo?.longestMuscleCrossSectionString ?? "A"; set { if (CowInfo != null) CowInfo.longestMuscleCrossSectionString = value; } }
    public string fatThicknessOnBackString { get => CowInfo?.fatThicknessOnBackString ?? "A"; set { if (CowInfo != null) CowInfo.fatThicknessOnBackString = value; } }
    public string intramuscularFatContentString { get => CowInfo?.intramuscularFatContentString ?? "D"; set { if (CowInfo != null) CowInfo.intramuscularFatContentString = value; } }
    public MockPanelViewModel(gValues cowInfo) => CowInfo = cowInfo;
}
