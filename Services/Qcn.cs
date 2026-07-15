using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace CowAuctionSmall.Services
{
    public class Qcn
    {
        [JsonProperty("TT_SCR")]
        public int? TtScr { get; set; }

        [JsonProperty("TMS_YN")]
        public string? TmsYn { get; set; }

        [JsonProperty("CUT_AM")]
        public int? CutAm { get; set; }

        [JsonProperty("DDL_YN")]
        public string? DdlYn { get; set; }

        [JsonProperty("AUC_OBJ_DSC")]
        public string? AucObjDsc { get; set; }

        [JsonProperty("LS_CMENO")]
        public string? LsCmeno { get; set; }

        [JsonProperty("FEMALE_KG")]
        public decimal? FemaleKg { get; set; }

        [JsonProperty("DEL_YN")]
        public string? DelYn { get; set; }

        [JsonProperty("MALE_KG")]
        public decimal? MaleKg { get; set; }

        [JsonProperty("FSRGMN_ENO")]
        public string? FsrgmnEno { get; set; }

        // "20251106" 형태 → 원문 보존
        [JsonProperty("AUC_DT")]
        public string? AucDt { get; set; }

        // "2025-11-05T22:59:49.000+00:00" 형태
        [JsonProperty("FSRG_DTM")]
        public DateTimeOffset? FsrgDtm { get; set; }

        [JsonProperty("NA_BZPLC")]
        public string? NaBzplc { get; set; }

        [JsonProperty("SGNO_PRC_DSC")]
        public string? SgnoPrcDsc { get; set; }

        [JsonProperty("LSCHG_DTM")]
        public DateTimeOffset? LschgDtm { get; set; }

        [JsonProperty("QCN")]
        public int QcnValue { get; set; }

        // -------- 편의용 파생 속성 --------
        [Newtonsoft.Json.JsonIgnore]
        public bool? TmsYnBool => TmsYn is null ? null : TmsYn == "1";

        [Newtonsoft.Json.JsonIgnore]
        public bool? DdlYnBool => DdlYn is null ? null : DdlYn == "1";

        [Newtonsoft.Json.JsonIgnore]
        public bool? DelYnBool => DelYn is null ? null : DelYn == "1";

        // "yyyyMMdd" → DateTime? 파싱 (실패 시 null)
        [Newtonsoft.Json.JsonIgnore]
        public DateTime? AucDate
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AucDt)) return null;
                return DateTime.TryParseExact(
                    AucDt, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt)
                    ? dt
                    : (DateTime?)null;
            }
        }
    }
}
/*
"TT_SCR": null,
            "TMS_YN": "0",
            "CUT_AM": 1,
            "DDL_YN": "0",
            "AUC_OBJ_DSC": "0",
            "LS_CMENO": "TEST0000",
            "FEMALE_KG": null,
            "DEL_YN": "0",
            "MALE_KG": null,
            "FSRGMN_ENO": "TEST0000",
            "AUC_DT": "20251106",
            "FSRG_DTM": "2025-11-05T22:59:49.000+00:00",
            "NA_BZPLC": "8808990657202",
            "SGNO_PRC_DSC": "1",
            "LSCHG_DTM": "2025-11-05T22:59:49.000+00:00",
            "QCN": 381
*/