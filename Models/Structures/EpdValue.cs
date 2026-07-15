using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CowAuctionSmall.Models.Structures
{
    public class EpdValue
    {
        public string SRA_INDV_AMNNO { get; set; } = string.Empty; //개체번호
        public int AUC_PRG_SQ { get; set; } = 0; // 경매번호

        public string EPD_1 { get; set; } = "";
        public string EPD_2 { get; set; } = "";
        public string EPD_3 { get; set; } = "";
        public string EPD_4 { get; set; } = "";

        public string BDHT_VAL { get; set; } = ""; //체고
        public string BDLN_VAL { get; set; } = ""; //체장

        public string BDWI_VAL { get; set; } = ""; //체폭
        public string BDHH_VAL { get; set; } = ""; //십자부고
    }
}
