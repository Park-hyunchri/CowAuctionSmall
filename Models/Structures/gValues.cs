using System;
using System.Text;

namespace CowAuctionSmall.Models.Structures
{
    /// <summary>
    /// 소 객체 하나에 대한 정보 구조
    /// </summary>
    public class gValues
    {
        /*
         * 0:구분자 | 1:조합구분코드 | 2:출품번호 | 3:경매회차 | 4:경매대상구분코드 | 5:축산개체관리번호 | 6:축산축종구분코드 | 7:농가식별번호 | 8:농장관리번호 | 9:농가명 | 10:브랜드명 | 
         * 11:생년월일 | 12:KPN번호 | 13:개체성별코드 | 14:어미소구분코드 | 15:어미소축산개체관리번호 | 16:산차 | 17:임신개월수 | 18:계대 | 19:계체식별번호 | 20:축산개체종축등록번호 | 
         * 21:등록구분번호 | 22:출하생산지역 | 23:친자검사결과여부 | 24:신규여부 | 25:우출하중량 | 26:최초최저낙찰한도금액 | 27:최저낙찰한도금액 | 28:비고내용 | 29:낙유찰결과 | 30:낙찰자 | 
         * 31:낙찰금액 | 32:응찰일시 | 33:마지막출품여부 | 34:계류대번호 | 35:초과출장우여부					

        // Ex>>
        // SV|8808990656656|1|189|1|410002159890848|01|332813|1|남향순||20.11.12(11개월)|KPN1180|null|혈통|410002068315481|6|0|3|하동20-03-9084|231867588|02|고전|1|1|0kg|470|290|친자일치|23||0||N|1|N
        // SV|8808990656656|1|189|1|410002159890848|01|332813|1|남향순||20.11.12(11개월)|KPN1180|null|혈통|410002068315481|6|0|3|하동20-03-9084|231867588|02|고전|1|1|0kg|470|290|친자일치|23||0||N|1|N
        */

        public string SpaceIndex { get; set; } = "-1";              //계류대 번호
        public string DataType { get; set; } = "-1";                   //계대
        public string Blood { get; set; } = "-1";                   //계대
        public string SipNumber { get; set; } = "-1";               //출하 번호
        public string Sex { get; set; } = "-";                      //성별
        public string LowestPrice { get; set; } = "-";              //최저가
        public string Weight { get; set; } = "-";                   //중량
        public string Birth { get; set; } = "-";                    //출생일 + 개월수
        public string BirthMonth { get; set; } = "-";                    //출생일 + 개월수
        public string Pregnant { get; set; } = "-";                 //임신개월수
        public string CalvingNumber { get; set; } = "-";            //어미 산차
        public string RegistrationCategory { get; set; } = "-";     //등록구분
        public string MotherLevel { get; set; } = "-";              //어미구분
        public string KPN { get; set; } = "-";                      //KPN 
        public string OwnerName { get; set; } = "-";                //출하주
        public string CowDistinction { get; set; } = "-";                //출하주
        public string Location { get; set; } = "-";                 //출하 지역
        public int ProcessStatus { get; set; } = 8001;              //경매 진행 상태
        public string Bidder { get; set; } = "-";                   //낙찰자 번호
        //public string BidNumber { get; set; } = "None";           //낙찰번호
        public string BidPrice { get; set; } = "-";                 //낙찰가격
        public string Note { get; set; } = "-";                     //비고
        //public string Sex { get, set};
        public string ModifiedPrice { get; set; } = "-";            //수정 최저가
        public string AuctionResultStatus { get; set; } = "-";

        public string EntityNumber { get; set; } = "";              //개체 번호
        public string EntityNumberShort { get; set; } = "";              //개체 번호

        public string Code { get; set; } = ""; // AS인지 SV인지 구분코드

        public bool IsRunning { get; set; } = false;
        public string UpdateDtm { get; set; } = "";
        public string BloodEntityNumber { get; set; } = "-";        //혈통등록번호 , 축산개체종축등록번호

        //-EPD- 유전능력
        public string bodyWeightInColdNum { get; set; } = ""; //냉도에서의 체중 
        public string bodyWeightInColdString { get; set; } = "-";
        public string longestMuscleCrossSectionNum { get; set; } = ""; //근육 최장 단면적
        public string longestMuscleCrossSectionString { get; set; } = "-";
        public string fatThicknessOnBackNum { get; set; } = ""; //등지방 두께
        public string fatThicknessOnBackString { get; set; } = "-";
        public string intramuscularFatContentNum { get; set; } = ""; //근내지방 함량
        public string intramuscularFatContentString { get; set; } = "-";

        //염소 KG당 가격
        public decimal GoatPricePerKg { get; set; } = 0;

        /// <summary>
        /// 경매번호 리턴
        /// </summary>
        public int getNumericShipNo()
        {
            int n = 0;
            int.TryParse(SipNumber, out n);
            return n;
        }

        /// <summary>
        /// 거치대 번호 리턴
        /// </summary>
        public int getNumericSpaceIndex()
        {
            int n = 0;
            int.TryParse(SpaceIndex, out n);
            return n;
        }

        public string toString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(SpaceIndex);
            sb.Append(DataType);
            sb.Append(Blood);
            sb.Append(SipNumber);
            sb.Append(Sex);
            sb.Append(LowestPrice);
            sb.Append(Weight);
            sb.Append(Birth);
            sb.Append(BirthMonth);
            sb.Append(Pregnant);                   // 2022.01.02 추가  (임-)
            sb.Append(CalvingNumber);
            sb.Append(RegistrationCategory);
            sb.Append(MotherLevel);
            sb.Append(KPN);
            sb.Append(OwnerName);
            sb.Append(CowDistinction);
            sb.Append(Location);
            sb.Append(ProcessStatus);
            sb.Append(Bidder);
            //sb.Append(this.BidNumber);
            sb.Append(BidPrice);
            sb.Append(Note);
            sb.Append(ModifiedPrice);
            sb.Append(AuctionResultStatus);
            sb.Append(EntityNumber);
            sb.Append(EntityNumberShort);
            sb.AppendLine(BloodEntityNumber);
            return sb.ToString();
        }

        public gValues Clone()
        {
            gValues clone = new gValues
            {
                SpaceIndex = SpaceIndex,
                DataType = DataType,
                Blood = Blood,
                SipNumber = SipNumber,
                Sex = Sex,
                LowestPrice = LowestPrice,
                Weight = Weight,
                Birth = Birth,
                BirthMonth = BirthMonth,
                Pregnant = Pregnant,
                CalvingNumber = CalvingNumber,
                RegistrationCategory = RegistrationCategory,
                MotherLevel = MotherLevel,
                KPN = KPN,
                OwnerName = OwnerName,
                CowDistinction = CowDistinction,
                Location = Location,
                ProcessStatus = ProcessStatus,
                Bidder = Bidder,
                BidPrice = BidPrice,
                Note = Note,
                ModifiedPrice = ModifiedPrice,
                AuctionResultStatus = AuctionResultStatus,
                EntityNumber = EntityNumber,
                EntityNumberShort = EntityNumberShort,
                BloodEntityNumber = BloodEntityNumber,
                IsRunning = IsRunning,
                UpdateDtm = UpdateDtm,

                bodyWeightInColdNum = bodyWeightInColdNum,
                bodyWeightInColdString = bodyWeightInColdString,
                longestMuscleCrossSectionNum = longestMuscleCrossSectionNum,
                longestMuscleCrossSectionString = longestMuscleCrossSectionString,
                fatThicknessOnBackNum = fatThicknessOnBackNum,
                fatThicknessOnBackString = fatThicknessOnBackString,
                intramuscularFatContentNum = intramuscularFatContentNum,
                intramuscularFatContentString = intramuscularFatContentString
            };

            return clone;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            gValues other = (gValues)obj;

            return SpaceIndex == other.SpaceIndex &&
                   DataType == other.DataType &&
                   Blood == other.Blood &&
                   SipNumber == other.SipNumber &&
                   Sex == other.Sex &&
                   LowestPrice == other.LowestPrice &&
                   Weight == other.Weight &&
                   Birth == other.Birth &&
                   BirthMonth == other.BirthMonth &&
                   Pregnant == other.Pregnant &&
                   CalvingNumber == other.CalvingNumber &&
                   RegistrationCategory == other.RegistrationCategory &&
                   MotherLevel == other.MotherLevel &&
                   KPN == other.KPN &&
                   OwnerName == other.OwnerName &&
                   CowDistinction == other.CowDistinction &&
                   Location == other.Location &&
                   ProcessStatus == other.ProcessStatus &&
                   Bidder == other.Bidder &&
                   BidPrice == other.BidPrice &&
                   Note == other.Note &&
                   ModifiedPrice == other.ModifiedPrice &&
                   AuctionResultStatus == other.AuctionResultStatus &&
                   EntityNumber == other.EntityNumber &&
                   EntityNumberShort == other.EntityNumberShort &&
                   BloodEntityNumber == other.BloodEntityNumber &&
                   IsRunning == other.IsRunning &&
                   UpdateDtm == other.UpdateDtm &&

                    bodyWeightInColdNum == other.bodyWeightInColdNum &&
                    bodyWeightInColdString == other.bodyWeightInColdString &&
                    longestMuscleCrossSectionNum == other.longestMuscleCrossSectionNum &&
                    longestMuscleCrossSectionString == other.longestMuscleCrossSectionString &&
                    fatThicknessOnBackNum == other.fatThicknessOnBackNum &&
                    fatThicknessOnBackString == other.fatThicknessOnBackString &&
                    intramuscularFatContentNum == other.intramuscularFatContentNum &&
                    intramuscularFatContentString == other.intramuscularFatContentString;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

}
