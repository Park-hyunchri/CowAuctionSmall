using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Domain.Auction;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CowAuctionSmall.Infra.Persistence
{
    public sealed class AuctionSnapshotCache
    {
        private readonly DailyCacheService _cache;
        // Explicit allowlist: no HTTP envelope, credential or derived owner-history text.
        private static readonly string[] Fields = ("CowDistinction SpaceIndex DataType Blood SipNumber Sex LowestPrice LowestPriceTitle Weight Birth BirthMonth Pregnant CalvingNumber RegistrationCategory MotherLevel KPN OwnerName CowDistinction Location PaternityMatch FrontNoteWord FrontNoteWordBrush ProcessStatus Bidder BidderNum BidderString BidderName BidPrice TraceNumber Note ModifiedPrice AuctionResultStatus EntityNumber MatherEntityNumber Code IsRunning UpdateDtm BloodEntityNumber bodyWeightInColdNum bodyWeightInColdString longestMuscleCrossSectionNum longestMuscleCrossSectionString fatThicknessOnBackNum fatThicknessOnBackString intramuscularFatContentNum intramuscularFatContentString SelectShowWeight_EPD GoatPricePerKg BodyLength BodyHeight BodyWidth CrossSectionalArea brucellosisTestDate footAndMouthDiseaseTestDate tuberculosisTestDate Child_EntityNumber Child_Sex Child_Weight Child_Birth Child_Kpn Nh_ability_1_num Nh_ability_1_str Nh_ability_2_num Nh_ability_2_str Nh_ability_3_num Nh_ability_3_str Nh_ability_4_num Nh_ability_4_str Is_Ｎh_Excellent Is_Mother_Ｎh_Excellent Is_Ｎh_ability Nh_ability_Str Is_Nh_QQuri Reproduction_Imsin_Sujung_Date Reproduction_Sujung_KPN").Split(' ');
        public AuctionSnapshotCache(DailyCacheService cache) { _cache = cache; }
        public static string Key(UserInfo user, string date)
        {
            string scope = JsonConvert.SerializeObject(new[] { user.Authentication?.Address, user.Authentication?.UserID, user.Auction?.AuctionHouseCode, date });
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scope)));
        }
        public Task<bool> SaveAsync(UserInfo user, string date, IEnumerable<gValues> entries, string epoch)
        {
            var array = new JArray(entries.Select(entry =>
            {
                var filtered = new JObject();
                foreach (var field in Fields)
                {
                    object? value = typeof(gValues).GetProperty(field)?.GetValue(entry);
                    if (value != null) filtered[field] = JToken.FromObject(value);
                }
                return filtered;
            }));
            return _cache.SaveCacheAsync("auction", Key(user, date), new JObject { ["version"] = 1, ["items"] = array }.ToString(Formatting.None), epoch);
        }
        public async Task<List<gValues>?> ReadAsync(UserInfo user, string date)
        {
            string epoch = _cache.Epoch;
            var json = await _cache.GetCacheAsync("auction", Key(user, date));
            if (json == null || epoch != _cache.Epoch) return null;
            try
            {
                var root = JObject.Parse(json);
                if (root["version"]?.Value<int>() != 1 || root["items"] is not JArray items) return null;
                var result = items.ToObject<List<gValues>>();
                if (result == null) return null;
                foreach (var item in result)
                {
                    item.NoteChangeOwner = "";
                    item.IsRunning = false;
                    item.ProcessStatus = 8001;
                }
                return result;
            }
            catch (JsonException) { return null; }
        }
    }
}
