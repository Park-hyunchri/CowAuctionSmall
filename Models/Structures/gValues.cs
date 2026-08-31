// 가축 데이터의 심장부 구조를 나타내는 클래스입니다.
// 이 클래스는 소 객체 하나에 대한 다양한 속성과 정보를 포함하고 있으며,
// 경매 관련 데이터와 유전능력(EPD) 정보도 포함하고 있습니다.
// 또한, 객체의 복사 및 비교 기능을 제공하여 데이터 관리에 유용합니다.
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text;

namespace CowAuctionSmall.Models.Structures
{
    public class gValues
    {
        public int UpdateSignature()
        {
            var hash = new HashCode();
            hash.Add(SpaceIndex);
            hash.Add(DataType);
            hash.Add(Blood);
            hash.Add(SipNumber);
            hash.Add(Sex);
            hash.Add(LowestPrice);
            hash.Add(Weight);
            hash.Add(Birth);
            hash.Add(BirthMonth);
            hash.Add(Pregnant);
            hash.Add(CalvingNumber);
            hash.Add(RegistrationCategory);
            hash.Add(MotherLevel);
            hash.Add(KPN);
            hash.Add(OwnerName);
            hash.Add(CowDistinction);
            hash.Add(Location);
            hash.Add(PaternityMatch);
            hash.Add(ProcessStatus);
            hash.Add(Bidder);
            hash.Add(BidderNum);
            hash.Add(BidderString);
            hash.Add(BidderName);
            hash.Add(BidPrice);
            hash.Add(Note);
            hash.Add(ModifiedPrice);
            hash.Add(AuctionResultStatus);
            hash.Add(EntityNumber);
            hash.Add(EntityNumberShort);
            hash.Add(MatherEntityNumber);
            hash.Add(Code);
            hash.Add(IsRunning);
            hash.Add(UpdateDtm);
            hash.Add(BloodEntityNumber);
            hash.Add(bodyWeightInColdNum);
            hash.Add(bodyWeightInColdString);
            hash.Add(longestMuscleCrossSectionNum);
            hash.Add(longestMuscleCrossSectionString);
            hash.Add(fatThicknessOnBackNum);
            hash.Add(fatThicknessOnBackString);
            hash.Add(intramuscularFatContentNum);
            hash.Add(intramuscularFatContentString);
            hash.Add(SelectShowWeight_EPD);
            hash.Add(GoatPricePerKg);
            hash.Add(BodyLength);
            hash.Add(BodyHeight);
            hash.Add(BodyWidth);
            hash.Add(CrossSectionalArea);
            hash.Add(brucellosisTestDate);
            hash.Add(footAndMouthDiseaseTestDate);
            hash.Add(tuberculosisTestDate);
            hash.Add(Child_EntityNumber);
            hash.Add(Child_Sex);
            hash.Add(Child_Weight);
            hash.Add(Child_Birth);
            hash.Add(Child_Kpn);
            hash.Add(Nh_ability_1_num);
            hash.Add(Nh_ability_1_str);
            hash.Add(Nh_ability_2_num);
            hash.Add(Nh_ability_2_str);
            hash.Add(Nh_ability_3_num);
            hash.Add(Nh_ability_3_str);
            hash.Add(Nh_ability_4_num);
            hash.Add(Nh_ability_4_str);
            hash.Add(Is_Ｎh_Excellent);
            hash.Add(Is_Mother_Ｎh_Excellent);
            hash.Add(Is_Ｎh_ability);
            hash.Add(Nh_ability_Str);
            hash.Add(Is_Nh_QQuri);
            hash.Add(Reproduction_Imsin_Sujung_Date);
            hash.Add(Reproduction_Sujung_KPN);
            return hash.ToHashCode();
        }
        /*
         * 0:구분자 | 1:조합구분코드 | 2:출품번호 | 3:경매회차 | 4:경매대상구분코드 | 5:축산개체관리번호 | 6:축산축종구분코드 | 7:농가식별번호 | 8:농장관리번호 | 9:농가명 | 10:브랜드명 | 
         * 11:생년월일 | 12:KPN번호 | 13:개체성별코드 | 14:어미소구분코드 | 15:어미소축산개체관리번호 | 16:산차 | 17:임신개월수 | 18:계대 | 19:계체식별번호 | 20:축산개체종축등록번호 | 
         * 21:등록구분번호 | 22:출하생산지역 | 23:친자검사결과여부 | 24:신규여부 | 25:우출하중량 | 26:최초최저낙찰한도금액 | 27:최저낙찰한도금액 | 28:비고내용 | 29:낙유찰결과 | 30:낙찰자 | 
         * 31:낙찰금액 | 32:응찰일시 | 33:마지막출품여부 | 34:계류대번호 | 35:초과출장우여부					

        // Ex>>
        // SV|8808990656656|1|189|1|410002159890848|01|332813|1|남향순||20.11.12(11개월)|KPN1180|null|혈통|410002068315481|6|0|3|하동20-03-9084|231867588|02|고전|1|1|0kg|470|290|친자일치|23||0||N|1|N
        // SV|8808990656656|1|189|1|410002159890848|01|332813|1|남향순||20.11.12(11개월)|KPN1180|null|혈통|410002068315481|6|0|3|하동20-03-9084|231867588|02|고전|1|1|0kg|470|290|친자일치|23||0||N|1|N
        */


        //SC 2025.01.17 정의서 변경으로 인한 추가 필드
        // 0.  구분자                 |  1.  조합구분코드          |  2.  출품번호             |  3.  경매회차          |  4.  경매대상구분코드
        // 5.  축산개체관리번호       |  6.  축산축종구분코드      |  7.  농가식별번호         |  8.  농장관리번호      |  9.  농가명
        // 10. 브랜드명               | 11. 생년월일               | 12. KPN번호               | 13. 개체성별코드       | 14. 어미소구분코드
        // 15. 어미소축산개체관리번호 | 16. 산차                   | 17. 임신개월수            | 18. 계대               | 19. 계체식별번호
        // 20. 축산개체종축등록번호   | 21. 등록구분번호           | 22. 출하생산지역          | 23. 친자검사결과여부   | 24. 신규여부
        // 25. 우출하중량             | 26. 최초최저낙찰한도금액   | 27. 최저낙찰한도금액      | 28. 비고내용           | 29. 낙유찰결과
        // 30. 낙찰자                 | 31. 낙찰금액               | 32. 응찰일시              | 33. 마지막출품여부     | 34. 계류대번호
        // 35. 초과출장우여부         | 36. 낙찰가                 | 37. 최종변경일            | 38.                    | 39. 월령
        // 40. 등록구분 명            | 41. 체장                   | 42. 체고                  | 43. 체폭               | 44. 십자부고
        // 45. 냉도체중 (등급)        | 46. 냉도체중 (값)          | 47. 배최장근단면적 (등급) | 48. 배최장근단면적 (값) | 49. 등지방두께 (등급) | 50. 등지방두께 (값)
        // 51. 근내지방도 (등급)      | 52. 근내지방도 (값)


        public string SpaceIndex { get; set; } = "-1";              //계류대 번호
        public string DataType { get; set; } = "-1";                   //계대
        public string Blood { get; set; } = "-1";                   //계대
        public string SipNumber { get; set; } = "-1";               //경매 번호
        public string Sex { get; set; } = "-";                      //성별
        public string LowestPrice { get; set; } = "-";              //최저가
                                                                    
        public string LowestPriceTitle { get; set; } = "최저가"; // 💡 users.xml의 <LowestPriceTitle> 옵션 값을 담을 프로퍼티 추가
        // 💡 진행 화면(HoengseongRun) 전용: '가'를 떼고 ':' 추가 ("시중:", "내정:", "최저:")
        public string LowestPriceTitleShort
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(LowestPriceTitle)) return "내정:";
                    string title = LowestPriceTitle.Trim().Replace(":", "");
                    if (title == "분양가") return "분양가:";
                    if (title.EndsWith("가") && title.Length > 1)
                    {
                        title = title.Substring(0, title.Length - 1);
                    }
                    return title + ":";
                }
                catch
                {
                    return "내정:";
                }
            }
        }

        // 💡 낙찰 화면(HoengseongSold) 전용: '가'와 ':' 붙이기 ("시중가:", "내정가:", "최저가:")
        public string LowestPriceTitleFull
        {
            get
            {
                if (string.IsNullOrWhiteSpace(LowestPriceTitle)) return "내정가:";
                string title = LowestPriceTitle.Trim().Replace(":", "");
                if (title == "분양가") return "분양가:";
                if (!title.EndsWith("가"))
                {
                    title += "가";
                }
                return title + ":";
            }
        }

        public string SuccessfulBidPriceTitle
        {
            get
            {
                return string.Equals(LowestPriceTitle?.Trim().Replace(":", ""), "분양가", StringComparison.Ordinal)
                    ? "분양가:"
                    : "낙찰가:";
            }
        }

        public string SuccessfulBidderTitle
        {
            get
            {
                return string.Equals(LowestPriceTitle?.Trim().Replace(":", ""), "분양가", StringComparison.Ordinal)
                    ? "당첨자:"
                    : "낙찰자:";
            }
        }

        public string Weight { get; set; } = "-";                   //중량
        public string Birth { get; set; } = "-";                    //출생일 + 개월수
        public string BirthMonth { get; set; } = "-";                    //월령
        public string Pregnant { get; set; } = "-";                 //임신개월수
        public string CalvingNumber { get; set; } = "-";            //어미 산차
        public string RegistrationCategory { get; set; } = "-";     //등록구분 혈통
        public string MotherLevel { get; set; } = "-";              //어미구분 혈통
        public string KPN { get; set; } = "-";                      //KPN 
        public string OwnerName { get; set; } = "-";                //출하주
        //public string CowDistinction { get; set; } = "-";                //범주, 1,송아지 ,2 비육우 ,3번식우 ,5 염소


        private string cowDistinction = "-";                //범주, 1,송아지 ,2 비육우 ,3번식우 ,5 염소, 6 말
        private string strCowDistinction = "-";
        public string CowDistinction//범주, 1,송아지 ,2 비육우 ,3번식우 ,5 염소}
        {
            get { return cowDistinction; }
            set 
            { 
                cowDistinction = value;
                switch (cowDistinction)
                {
                    case "1":
                        strCowDistinction = "송아지";
                        break;
                    case "2":
                        strCowDistinction = "비육우";
                        break;
                    case "3":
                        strCowDistinction = "번식우";
                        break;
                    case "5":
                        strCowDistinction = "염소";
                        break;
                    case "6":
                        strCowDistinction = "말";
                        break;
                    default:
                        strCowDistinction = "-";
                    break;
                }
              
            }
        }



        // 화면 폭(MaxWidth=200) 기준으로 적당히 길이로 판단
        public bool IsTwoLineName
        {
            get
            {
                if (BidderName != "Y" || string.IsNullOrEmpty(Bidder))
                    return false;

                // 대충 글자 수 기준으로 줄 수를 가늠 (숫자는 네가 조정해도 됨)
                return Bidder.Length > 3;   // 예시: 6글자 이상이면 2줄일 가능성이 높다
            }
        }

        public string StrCowDistinction { get { return strCowDistinction; } }

        public string Location { get; set; } = "-";                 //출하 지역

        public string PaternityMatch { get; set; } = string.Empty; //친자검사결과여부
        public bool HasPaternityMatch => !string.IsNullOrWhiteSpace(PaternityMatch) && PaternityMatch != "-";
        public int ProcessStatus { get; set; } = 8001;              //경매 진행 상태
        public string Bidder { get; set; } = "-";                   //낙찰자 번호
        public string BidderNum { get; set; } = "";           //낙찰번호
        public string BidderString { get; set; } = "";           //낙찰번호

        public string BidderName { get; set; } = string.Empty; // 낙찰자 이름 표시 여부 Y,N,B

        public string BidPrice { get; set; } = "-";                 //낙찰가격
        public string Note { get; set; } = "-";                     //비고
        public string ModifiedPrice { get; set; } = "-";            //수정 최저가
        public string AuctionResultStatus { get; set; } = "-";

        public string EntityNumber { get; set; } = "";              //개체 번호
        public string EntityNumberShort { get; set; } = "";              //개체 번호

        public string MatherEntityNumber { get; set; } = "";              //개체 번호

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
        public string SelectShowWeight_EPD { get; set; } = "Y"; //기본값은 Y 중량을 보여주고 N 일 경우 중량대신 EPD 알파벳을 보여준다.

        //염소 KG당 가격
        public decimal GoatPricePerKg { get; set; } = 0;

        public string BodyLength { get; set; } = "-"; // 체장
        public string BodyHeight { get; set; } = "-"; // 체고
        public string BodyWidth { get; set; } = "-"; // 체폭
        public string CrossSectionalArea { get; set; } = "-"; // 십자부고

        public string brucellosisTestDate { get; set; } = "-"; // 브루셀라 검사일
        public string footAndMouthDiseaseTestDate { get; set; } = "-"; // 구제역 검사일
        public string tuberculosisTestDate { get; set; } = "-"; // 우결핵 검사일

        //20251106 추가
        public string Child_EntityNumber  { get; set; } =  "-";          // 딸린송아지 귀표번호
        public string Child_Sex  { get; set; } = "-";                   // 딸린송아지 성별
        public string Child_Weight  { get; set; } = "-";                // 딸린송아주 중량
        public string Child_Birth  { get; set; } = "-";                 // 딸린송아주 생년월일
        public string Child_Kpn  { get; set; } = "-";                   // 딸린송아주 kpn

        public string Nh_ability_1_num  { get; set; }=  "-";            // 우량 냉도체중(값)
        public string Nh_ability_1_str  { get; set; }=  "-";            // 우량 냉도체중(등급)
        public string Nh_ability_2_num  { get; set; }=  "-";            // 우량 배최장근단면적(값)
        public string Nh_ability_2_str  { get; set; }=  "-";            // 우량 배최장근단면적(등급)
        public string Nh_ability_3_num  { get; set; }=  "-";            // 우량 등지방두께(값)
        public string Nh_ability_3_str  { get; set; }=  "-";            // 우량 등지방두께(등급)
        public string Nh_ability_4_num  { get; set; }=  "-";            // 우량 근내지방도(값)
        public string Nh_ability_4_str  { get; set; }=  "-";            // 우량 근내지방도(등급)

        public string Is_Ｎh_Excellent  { get; set; }=  "";               // 우량 여부
        public string Is_Mother_Ｎh_Excellent { get; set; } = "";               // 어미 우량 여부
        public string Is_Ｎh_ability  { get; set; }=  "";               // 유전체분석 여부
        public string Nh_ability_Str  { get; set; }=  "";                  // 유전체분석 알파벳만
        public string Is_Nh_QQuri  { get; set; }=  "";                  // 뿌리농가 참여여부

        //번식우 임신수정일
        public string Reproduction_Imsin_Sujung_Date { get; set; } = "-"; // 번식우 임신수정일
        //번식우 수정kpn
        public string Reproduction_Sujung_KPN { get; set; } = "-"; // 번식우 수정kpn


        public string Is_strＮh_Excellent
        {
            get
            {
                if (Is_Ｎh_Excellent == "Y" || Is_Mother_Ｎh_Excellent == "Y")
                {
                    return "농협우량";
                }
                else if (Is_Mother_Ｎh_Excellent == "Y" || Is_Mother_Ｎh_Excellent == "A")
                {
                    return "농협우량";
                }
                else
                {
                    return "";
                }
            }
        }

        public gValues CopyGValues
        {
            get
            {
                return Clone();  // Clone()을 바로 호출하여 반환
            }
        }

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
            sb.Append(PaternityMatch);
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
            sb.AppendLine(BodyLength);
            sb.AppendLine(BodyHeight);
            sb.AppendLine(BodyWidth);
            sb.AppendLine(CrossSectionalArea);
            sb.AppendLine(MatherEntityNumber);
            sb.AppendLine(bodyWeightInColdNum);
            sb.AppendLine(bodyWeightInColdString);
            sb.AppendLine(longestMuscleCrossSectionNum);
            sb.AppendLine(longestMuscleCrossSectionString);
            sb.AppendLine(fatThicknessOnBackNum);
            sb.AppendLine(fatThicknessOnBackString);
            sb.AppendLine(intramuscularFatContentNum);
            sb.AppendLine(intramuscularFatContentString);
            sb.AppendLine(brucellosisTestDate);
            sb.AppendLine(footAndMouthDiseaseTestDate);
            sb.AppendLine(tuberculosisTestDate);

            // ▼ 여기부터 새로 추가된 필드들
            sb.AppendLine(Child_EntityNumber);
            sb.AppendLine(Child_Sex);
            sb.AppendLine(Child_Weight);
            sb.AppendLine(Child_Birth);
            sb.AppendLine(Child_Kpn);

            sb.AppendLine(Nh_ability_1_num);
            sb.AppendLine(Nh_ability_1_str);
            sb.AppendLine(Nh_ability_2_num);
            sb.AppendLine(Nh_ability_2_str);
            sb.AppendLine(Nh_ability_3_num);
            sb.AppendLine(Nh_ability_3_str);
            sb.AppendLine(Nh_ability_4_num);
            sb.AppendLine(Nh_ability_4_str);

            sb.AppendLine(Is_Ｎh_Excellent);
            sb.AppendLine(Is_Mother_Ｎh_Excellent);
            sb.AppendLine(Is_Ｎh_ability);
            sb.AppendLine(Nh_ability_Str);
            sb.AppendLine(Is_Nh_QQuri);

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
                PaternityMatch = PaternityMatch,
                ProcessStatus = ProcessStatus,
                Bidder = Bidder,
                BidderNum = BidderNum,
                BidderString = BidderString,
                BidderName = BidderName,
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
                intramuscularFatContentString = intramuscularFatContentString,
                SelectShowWeight_EPD = SelectShowWeight_EPD,
                BodyLength = BodyLength,
                BodyHeight = BodyHeight,
                BodyWidth = BodyWidth,
                CrossSectionalArea = CrossSectionalArea,
                MatherEntityNumber = MatherEntityNumber,

                brucellosisTestDate = brucellosisTestDate,
                footAndMouthDiseaseTestDate = footAndMouthDiseaseTestDate,
                tuberculosisTestDate = tuberculosisTestDate,

                // ▼ 새 필드들 복사
                Child_EntityNumber = Child_EntityNumber,
                Child_Sex = Child_Sex,
                Child_Weight = Child_Weight,
                Child_Birth = Child_Birth,
                Child_Kpn = Child_Kpn,

                Nh_ability_1_num = Nh_ability_1_num,
                Nh_ability_1_str = Nh_ability_1_str,
                Nh_ability_2_num = Nh_ability_2_num,
                Nh_ability_2_str = Nh_ability_2_str,
                Nh_ability_3_num = Nh_ability_3_num,
                Nh_ability_3_str = Nh_ability_3_str,
                Nh_ability_4_num = Nh_ability_4_num,
                Nh_ability_4_str = Nh_ability_4_str,

                Is_Ｎh_Excellent = Is_Ｎh_Excellent,
                Is_Mother_Ｎh_Excellent = Is_Mother_Ｎh_Excellent,
                Is_Ｎh_ability = Is_Ｎh_ability,
                Nh_ability_Str = Nh_ability_Str,
                Is_Nh_QQuri = Is_Nh_QQuri
            };

            return clone;
        }


        public override bool Equals(object? obj)
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
                PaternityMatch == other.PaternityMatch &&
                ProcessStatus == other.ProcessStatus &&
                Bidder == other.Bidder &&
                BidderNum == other.BidderNum &&
                BidderString == other.BidderString &&
                BidderName == other.BidderName &&
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
                intramuscularFatContentString == other.intramuscularFatContentString &&
                SelectShowWeight_EPD == other.SelectShowWeight_EPD &&
                GoatPricePerKg == other.GoatPricePerKg &&
                BodyHeight == other.BodyHeight &&
                BodyLength == other.BodyLength &&
                BodyWidth == other.BodyWidth &&
                CrossSectionalArea == other.CrossSectionalArea &&
                MatherEntityNumber == other.MatherEntityNumber &&
                Code == other.Code &&

                brucellosisTestDate == other.brucellosisTestDate &&
                footAndMouthDiseaseTestDate == other.footAndMouthDiseaseTestDate &&
                tuberculosisTestDate == other.tuberculosisTestDate &&
                Child_EntityNumber == other.Child_EntityNumber &&
                Child_Sex == other.Child_Sex &&
                Child_Weight == other.Child_Weight &&
                Child_Birth == other.Child_Birth &&
                Child_Kpn == other.Child_Kpn &&
                Nh_ability_1_num == other.Nh_ability_1_num &&
                Nh_ability_1_str == other.Nh_ability_1_str &&
                Nh_ability_2_num == other.Nh_ability_2_num &&
                Nh_ability_2_str == other.Nh_ability_2_str &&
                Nh_ability_3_num == other.Nh_ability_3_num &&
                Nh_ability_3_str == other.Nh_ability_3_str &&
                Nh_ability_4_num == other.Nh_ability_4_num &&
                Nh_ability_4_str == other.Nh_ability_4_str &&
                Is_Ｎh_Excellent == other.Is_Ｎh_Excellent &&
                Is_Mother_Ｎh_Excellent == other.Is_Mother_Ｎh_Excellent &&
                Is_Ｎh_ability == other.Is_Ｎh_ability &&
                Nh_ability_Str == other.Nh_ability_Str &&
                Is_Nh_QQuri == other.Is_Nh_QQuri &&
                Reproduction_Imsin_Sujung_Date == other.Reproduction_Imsin_Sujung_Date &&
                Reproduction_Sujung_KPN == other.Reproduction_Sujung_KPN;
        }


        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(SpaceIndex);
            hash.Add(DataType);
            hash.Add(Blood);
            hash.Add(SipNumber);
            hash.Add(Sex);
            hash.Add(LowestPrice);
            hash.Add(Weight);
            hash.Add(Birth);
            hash.Add(BirthMonth);
            hash.Add(Pregnant);
            hash.Add(CalvingNumber);
            hash.Add(RegistrationCategory);
            hash.Add(MotherLevel);
            hash.Add(KPN);
            hash.Add(OwnerName);
            hash.Add(CowDistinction);
            hash.Add(Location);
            hash.Add(PaternityMatch);
            hash.Add(ProcessStatus);
            hash.Add(Bidder);
            hash.Add(BidderNum);
            hash.Add(BidderString);
            hash.Add(BidderName);
            hash.Add(BidPrice);
            hash.Add(Note);
            hash.Add(ModifiedPrice);
            hash.Add(AuctionResultStatus);
            hash.Add(EntityNumber);
            hash.Add(EntityNumberShort);
            hash.Add(BloodEntityNumber);
            hash.Add(IsRunning);
            hash.Add(UpdateDtm);
            hash.Add(bodyWeightInColdNum);
            hash.Add(bodyWeightInColdString);
            hash.Add(longestMuscleCrossSectionNum);
            hash.Add(longestMuscleCrossSectionString);
            hash.Add(fatThicknessOnBackNum);
            hash.Add(fatThicknessOnBackString);
            hash.Add(intramuscularFatContentNum);
            hash.Add(intramuscularFatContentString);
            hash.Add(SelectShowWeight_EPD);
            hash.Add(GoatPricePerKg);
            hash.Add(BodyHeight);
            hash.Add(BodyLength);
            hash.Add(BodyWidth);
            hash.Add(CrossSectionalArea);
            hash.Add(MatherEntityNumber);
            hash.Add(Code);
            hash.Add(brucellosisTestDate);
            hash.Add(footAndMouthDiseaseTestDate);
            hash.Add(tuberculosisTestDate);
            hash.Add(Child_EntityNumber);
            hash.Add(Child_Sex);
            hash.Add(Child_Weight);
            hash.Add(Child_Birth);
            hash.Add(Child_Kpn);
            hash.Add(Nh_ability_1_num);
            hash.Add(Nh_ability_1_str);
            hash.Add(Nh_ability_2_num);
            hash.Add(Nh_ability_2_str);
            hash.Add(Nh_ability_3_num);
            hash.Add(Nh_ability_3_str);
            hash.Add(Nh_ability_4_num);
            hash.Add(Nh_ability_4_str);
            hash.Add(Is_Ｎh_Excellent);
            hash.Add(Is_Mother_Ｎh_Excellent);
            hash.Add(Is_Ｎh_ability);
            hash.Add(Nh_ability_Str);
            hash.Add(Is_Nh_QQuri);
            hash.Add(Reproduction_Imsin_Sujung_Date);
            hash.Add(Reproduction_Sujung_KPN);
            return hash.ToHashCode();
        }
    }

}
