using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CowAuctionSmall.Models.Structures;

namespace GAMSTest.Services;

public sealed class MockDataFactory
{
    private readonly List<ScenarioItem> _items;

    public MockDataFactory(UserInfo userInfo)
    {
        _items = LoadScenarioItems(userInfo);
    }

    public gValues Create(int boardNumber, string status, bool running)
    {
        var item = _items[boardNumber % _items.Count];
        return new gValues
        {
            SpaceIndex = boardNumber.ToString(), SipNumber = boardNumber.ToString(),
            AuctionResultStatus = status, IsRunning = running, ProcessStatus = 8001,
            DataType = item.DataType ?? "-1", Blood = item.Blood ?? "-1", Sex = item.Sex ?? "-",
            LowestPrice = item.LowestPrice ?? "-", LowestPriceTitle = item.LowestPriceTitle ?? "최저가",
            Weight = item.Weight ?? "-", Birth = item.Birth ?? "-", BirthMonth = item.BirthMonth ?? "-",
            Pregnant = item.Pregnant ?? "-", CalvingNumber = item.CalvingNumber ?? "-",
            RegistrationCategory = item.RegistrationCategory ?? "-", MotherLevel = item.MotherLevel ?? "-",
            KPN = item.KPN ?? "-", OwnerName = item.OwnerName ?? "-", Location = item.Location ?? "-",
            PaternityMatch = item.PaternityMatch ?? string.Empty, FrontNoteWord = item.FrontNoteWord ?? string.Empty,
            FrontNoteWordBrush = item.FrontNoteWordBrush ?? "Transparent", Bidder = item.Bidder ?? "-",
            BidderNum = item.BidderNum ?? string.Empty, BidderString = item.BidderString ?? string.Empty,
            BidderName = item.BidderName ?? string.Empty, BidPrice = item.BidPrice ?? "-", Note = item.Note ?? "-",
            ModifiedPrice = item.ModifiedPrice ?? "-", EntityNumber = item.EntityNumber ?? string.Empty,
            EntityNumberShort = item.EntityNumberShort ?? string.Empty, CowDistinction = item.CowDistinction ?? "1",
            Code = item.Code ?? string.Empty, BloodEntityNumber = item.BloodEntityNumber ?? "-",
            bodyWeightInColdNum = item.bodyWeightInColdNum ?? string.Empty, bodyWeightInColdString = item.bodyWeightInColdString ?? "-",
            longestMuscleCrossSectionNum = item.longestMuscleCrossSectionNum ?? string.Empty, longestMuscleCrossSectionString = item.longestMuscleCrossSectionString ?? "-",
            fatThicknessOnBackNum = item.fatThicknessOnBackNum ?? string.Empty, fatThicknessOnBackString = item.fatThicknessOnBackString ?? "-",
            intramuscularFatContentNum = item.intramuscularFatContentNum ?? string.Empty, intramuscularFatContentString = item.intramuscularFatContentString ?? "-",
            SelectShowWeight_EPD = item.SelectShowWeight_EPD ?? "Y", Nh_ability_1_num = item.Nh_ability_1_num ?? "-",
            Nh_ability_1_str = item.Nh_ability_1_str ?? "-", Nh_ability_2_num = item.Nh_ability_2_num ?? "-",
            Nh_ability_2_str = item.Nh_ability_2_str ?? "-", Nh_ability_3_num = item.Nh_ability_3_num ?? "-",
            Nh_ability_3_str = item.Nh_ability_3_str ?? "-", Nh_ability_4_num = item.Nh_ability_4_num ?? "-",
            Nh_ability_4_str = item.Nh_ability_4_str ?? "-", Is_Ｎh_Excellent = item.Is_Ｎh_Excellent ?? string.Empty,
            Is_Mother_Ｎh_Excellent = item.Is_Mother_Ｎh_Excellent ?? string.Empty, Is_Ｎh_ability = item.Is_Ｎh_ability ?? string.Empty,
            Nh_ability_Str = item.Nh_ability_Str ?? string.Empty, Is_Nh_QQuri = item.Is_Nh_QQuri ?? string.Empty,
            Reproduction_Imsin_Sujung_Date = item.Reproduction_Imsin_Sujung_Date ?? "-",
            Reproduction_Sujung_KPN = item.Reproduction_Sujung_KPN ?? "-"
        };
    }

    private static List<ScenarioItem> LoadScenarioItems(UserInfo userInfo)
    {
        var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scenarios");
        var code = userInfo.Auction?.AuctionHouseCode;
        var items = ReadScenario(string.IsNullOrWhiteSpace(code) ? string.Empty : Path.Combine(directory, $"{code}.json"));
        if (items.Count == 0) items = ReadScenario(Path.Combine(directory, "default.json"));
        return items.Count == 0 ? new List<ScenarioItem> { new() } : items;
    }

    private static List<ScenarioItem> ReadScenario(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return new List<ScenarioItem>();
        try
        {
            return JsonSerializer.Deserialize<List<ScenarioItem>>(File.ReadAllText(path)) ?? new List<ScenarioItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MockDataFactory] JSON load failed: {ex.Message}");
            return new List<ScenarioItem>();
        }
    }

    public sealed class ScenarioItem
    {
        public string? DataType { get; set; } public string? Blood { get; set; } public string? CowDistinction { get; set; }
        public string? Sex { get; set; } public string? LowestPrice { get; set; } public string? LowestPriceTitle { get; set; }
        public string? Weight { get; set; } public string? Birth { get; set; } public string? BirthMonth { get; set; }
        public string? Pregnant { get; set; } public string? CalvingNumber { get; set; } public string? RegistrationCategory { get; set; }
        public string? MotherLevel { get; set; } public string? KPN { get; set; } public string? OwnerName { get; set; }
        public string? Location { get; set; } public string? PaternityMatch { get; set; } public string? FrontNoteWord { get; set; }
        public string? FrontNoteWordBrush { get; set; } public string? Bidder { get; set; } public string? BidderNum { get; set; }
        public string? BidderString { get; set; } public string? BidderName { get; set; } public string? BidPrice { get; set; }
        public string? Note { get; set; } public string? ModifiedPrice { get; set; } public string? EntityNumber { get; set; }
        public string? EntityNumberShort { get; set; } public string? Code { get; set; } public string? BloodEntityNumber { get; set; }
        public string? bodyWeightInColdNum { get; set; } public string? bodyWeightInColdString { get; set; }
        public string? longestMuscleCrossSectionNum { get; set; } public string? longestMuscleCrossSectionString { get; set; }
        public string? fatThicknessOnBackNum { get; set; } public string? fatThicknessOnBackString { get; set; }
        public string? intramuscularFatContentNum { get; set; } public string? intramuscularFatContentString { get; set; }
        public string? SelectShowWeight_EPD { get; set; } public string? Nh_ability_1_num { get; set; } public string? Nh_ability_1_str { get; set; }
        public string? Nh_ability_2_num { get; set; } public string? Nh_ability_2_str { get; set; } public string? Nh_ability_3_num { get; set; }
        public string? Nh_ability_3_str { get; set; } public string? Nh_ability_4_num { get; set; } public string? Nh_ability_4_str { get; set; }
        public string? Is_Ｎh_Excellent { get; set; } public string? Is_Mother_Ｎh_Excellent { get; set; } public string? Is_Ｎh_ability { get; set; }
        public string? Nh_ability_Str { get; set; } public string? Is_Nh_QQuri { get; set; }
        public string? Reproduction_Imsin_Sujung_Date { get; set; } public string? Reproduction_Sujung_KPN { get; set; }
    }
}
