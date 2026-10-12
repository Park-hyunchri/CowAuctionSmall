using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Infra.Logging;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Domain.Auction;
// 데이터 파싱 서비스(AnimalParseData) - 서버에서 전달받은 경매 데이터 문자열을 객체로 변환하는 서비스 클래스
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Web;
using static System.Net.Mime.MediaTypeNames;

namespace CowAuctionSmall.Infra.Api
{
    /// <summary>
    /// 경매 데이터 소,염소 등의 String data "asd | 123 | 2"을 처리해서 객체로 전환
    /// 축협마다 원하는 데이터가공이 필요하는 경우가 종종 있다
    /// 예를들면 횡성의 경우 송아지의 경우 9개월 부터는 비육으로 표시 등
    /// 
    /// 하지만 최대한 서버가 보내준 데이터를 그대로 표출하려고 해야한다. 너무 맞춤일 경우 .. 
    /// </summary>
    public class AnimalParseData : IAuctionPacketParser
    {
        private NLogger logger;
        /// <summary>
        /// 파서에서 사용할 로거를 초기화한다.
        /// </summary>
        public AnimalParseData()
        {
            logger = NLogger.Instance;
        }


        /// <summary>
        /// 서버 메시지 문자열을 gValues로 파싱한다.
        /// </summary>
        public gValues Parse_PacketApi(string message, UserInfo userInfo, ServerConn conn)
        {
            if (conn == null) return new gValues();
            return ParsePacket(message, userInfo);
        }

        public AuctionItemUpdate ParseUpdate(string message, UserInfo userInfo, bool isSv, gValues? previous = null)
        {
            if (message == null || userInfo == null) throw new FormatException("Auction item input is missing.");
            var source = message.Split('|');
            if (source.Length < 35 || !int.TryParse(source[2], out int sip) || sip < 0)
                throw new FormatException("Auction item identity or structure is invalid.");
            if (!string.IsNullOrEmpty(source[5]) &&
                (source[5].Length != 15 || source[5].Any(character => character < '0' || character > '9')))
                throw new FormatException("Auction item trace number is invalid.");

            // 파서는 API 배열 위치로 통일한다. 패딩은 필드 존재를 의미하지 않는다.
            var data = Enumerable.Repeat(string.Empty, Math.Max(75, source.Length)).ToArray();
            var present = new bool[data.Length];
            for (int index = 0; index < source.Length; index++)
            {
                int target = isSv && index >= 36 ? index + 2 : index;
                if (isSv && index > 38) break; // 문서로 확인되지 않은 SV 확장 필드는 추측하지 않는다.
                data[target] = source[index];
                present[target] = true;
            }
            data[0] = "SC";
            data[2] = sip.ToString(CultureInfo.InvariantCulture);
            var fields = new HashSet<string>(StringComparer.Ordinal);
            void Include(int index, params string[] names)
            {
                if (present[index]) foreach (var name in names) fields.Add(name);
            }
            bool Numeric(int index, bool integer = false, bool signed = false)
            {
                if (!present[index]) return false;
                string value = data[index];
                if (string.IsNullOrEmpty(value) || value == "null")
                {
                    if (value == "null") data[index] = string.Empty;
                    return true;
                }
                if (signed && value == "-") return true;
                var styles = NumberStyles.AllowDecimalPoint | (signed ? NumberStyles.AllowLeadingSign : NumberStyles.None);
                bool valid = decimal.TryParse(value, styles, CultureInfo.InvariantCulture, out decimal number) &&
                    (signed || number >= 0) && (!integer || number == decimal.Truncate(number));
                if (!valid) { present[index] = false; data[index] = string.Empty; }
                else if (index == 27 || index == 31) data[index] = number.ToString("G29", CultureInfo.InvariantCulture);
                return valid;
            }

            Numeric(27);
            if (isSv && string.IsNullOrEmpty(data[27])) present[27] = false;
            Numeric(31);
            Numeric(30, integer: true);
            Numeric(29, integer: true);
            Numeric(39, integer: true);
            string birthDate = data[11].Split('(')[0].Trim();
            if (!string.IsNullOrEmpty(birthDate) && birthDate != "null" && birthDate != "-" &&
                !DateTime.TryParseExact(birthDate, new[] { "yy.MM.dd", "yyyy.MM.dd", "yyyyMMdd", "yyyy-MM-dd" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                present[11] = false;
            if (!int.TryParse(data[34], out int space) || space < 0) present[34] = false;
            string weight = data[25].EndsWith("kg", StringComparison.OrdinalIgnoreCase)
                ? data[25].Substring(0, data[25].Length - 2).Trim() : data[25];
            if (!string.IsNullOrEmpty(weight) && weight != "-" && weight != "null" &&
                (!decimal.TryParse(weight, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal kilograms) || kilograms < 0))
                present[25] = false;
            for (int index = 41; index <= 44; index++) Numeric(index);
            foreach (int index in new[] { 46, 48, 50, 52, 64, 65, 66, 67 }) Numeric(index, signed: true);
            foreach (int index in new[] { 53, 54, 55 })
                if (present[index] && !string.IsNullOrEmpty(data[index]) &&
                    !DateTime.TryParseExact(data[index], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    present[index] = false;

            // 낙찰자 이름의 표시 길이는 API 전용 우량 정보에도 의존한다.
            if (previous != null)
            {
                if (!present[61]) data[61] = previous.Is_Ｎh_Excellent;
                if (!present[62]) data[62] = previous.Is_Mother_Ｎh_Excellent;
                if (!present[64]) data[64] = previous.Nh_ability_1_num;
                if (!present[72]) data[72] = previous.Is_Nh_QQuri;
            }

            Include(0, nameof(gValues.DataType), nameof(gValues.LowestPriceTitle), nameof(gValues.SelectShowWeight_EPD), nameof(gValues.BidderName));
            Include(2, nameof(gValues.SipNumber));
            Include(4, nameof(gValues.CowDistinction));
            Include(5, nameof(gValues.TraceNumber), nameof(gValues.EntityNumber));
            Include(9, nameof(gValues.OwnerName));
            Include(11, nameof(gValues.Birth));
            Include(12, nameof(gValues.KPN));
            Include(13, nameof(gValues.Sex));
            Include(14, nameof(gValues.MotherLevel));
            Include(15, nameof(gValues.MatherEntityNumber));
            Include(16, nameof(gValues.CalvingNumber));
            Include(17, nameof(gValues.Pregnant));
            Include(18, nameof(gValues.Blood));
            Include(20, nameof(gValues.BloodEntityNumber));
            Include(21, nameof(gValues.RegistrationCategory));
            Include(22, nameof(gValues.Location));
            Include(23, nameof(gValues.PaternityMatch));
            Include(25, nameof(gValues.Weight));
            Include(27, nameof(gValues.LowestPrice));
            Include(28, nameof(gValues.Note), nameof(gValues.FrontNoteWord), nameof(gValues.FrontNoteWordBrush), nameof(gValues.PaternityMatch));
            Include(29, nameof(gValues.AuctionResultStatus));
            Include(30, nameof(gValues.ResultBidderNumber));
            if (present[29] && present[30] && (present[38] ||
                (data[29] != "22" && data[29] != "23") || string.IsNullOrEmpty(data[30]) || data[30] == "0" ||
                !new[] { "Y", "X", "B" }.Contains(userInfo.Auction?.BidderName?.Trim().ToUpperInvariant())))
            {
                fields.Add(nameof(gValues.Bidder));
                fields.Add(nameof(gValues.BidderNum));
                fields.Add(nameof(gValues.BidderString));
            }
            if (present[29]) Include(31, nameof(gValues.BidPrice));
            Include(34, nameof(gValues.SpaceIndex));
            Include(39, nameof(gValues.BirthMonth));
            if (!present[11]) fields.Remove(nameof(gValues.BirthMonth));
            if (!present[39])
            {
                if (userInfo.Auction?.CowBirth?.ToUpperInvariant() == "N") fields.Remove(nameof(gValues.Birth));
                if (userInfo.Auction?.AuctionHouseCode == "8808990656885" && userInfo.Auction?.ChangeSexName?.ToUpperInvariant() == "N")
                    fields.Remove(nameof(gValues.Sex));
            }
            Include(41, nameof(gValues.BodyLength));
            Include(42, nameof(gValues.BodyHeight));
            Include(43, nameof(gValues.BodyWidth));
            Include(44, nameof(gValues.CrossSectionalArea));
            Include(45, nameof(gValues.bodyWeightInColdString));
            Include(46, nameof(gValues.bodyWeightInColdNum));
            Include(47, nameof(gValues.longestMuscleCrossSectionString));
            Include(48, nameof(gValues.longestMuscleCrossSectionNum));
            Include(49, nameof(gValues.fatThicknessOnBackString));
            Include(50, nameof(gValues.fatThicknessOnBackNum));
            Include(51, nameof(gValues.intramuscularFatContentString));
            Include(52, nameof(gValues.intramuscularFatContentNum));
            Include(53, nameof(gValues.brucellosisTestDate));
            Include(54, nameof(gValues.footAndMouthDiseaseTestDate));
            Include(55, nameof(gValues.tuberculosisTestDate));
            Include(56, nameof(gValues.Child_EntityNumber));
            Include(57, nameof(gValues.Child_Sex));
            Include(58, nameof(gValues.Child_Birth));
            Include(59, nameof(gValues.Child_Weight));
            Include(60, nameof(gValues.Child_Kpn));
            Include(61, nameof(gValues.Is_Ｎh_Excellent));
            Include(62, nameof(gValues.Is_Mother_Ｎh_Excellent));
            Include(63, nameof(gValues.Is_Ｎh_ability));
            Include(64, nameof(gValues.Nh_ability_1_num));
            Include(65, nameof(gValues.Nh_ability_2_num));
            Include(66, nameof(gValues.Nh_ability_3_num));
            Include(67, nameof(gValues.Nh_ability_4_num));
            Include(68, nameof(gValues.Nh_ability_1_str));
            Include(69, nameof(gValues.Nh_ability_2_str));
            Include(70, nameof(gValues.Nh_ability_3_str));
            Include(71, nameof(gValues.Nh_ability_4_str));
            Include(72, nameof(gValues.Is_Nh_QQuri), nameof(gValues.Nh_ability_Str));
            Include(73, nameof(gValues.Reproduction_Sujung_KPN));
            Include(74, nameof(gValues.Reproduction_Imsin_Sujung_Date));

            var parsed = ParsePacket(string.Join("|", data), userInfo);
            parsed.DataType = "SC";
            parsed.AuctionHouseCode = source[1];
            parsed.AuctionRound = source[3];
            parsed.SuppliedFields = Array.AsReadOnly(fields.ToArray());
            return new AuctionItemUpdate(parsed, parsed.SuppliedFields, source[1], source[3]);
        }

        public gValues ParsePacket(string message, UserInfo userInfo)
        {
            if (message == null || userInfo == null)
            {
                logger.LogError("ParsePacket: 입력 또는 사용자 설정이 없습니다.");
                return new gValues();
            }

            gValues gv = new gValues();
            string[] data = message.Split('|');

            // 0.  구분자                 |  1.  조합구분코드          |  2.  출품번호             |  3.  경매회차           |  4.  경매대상구분코드
            // 5.  축산개체관리번호       |  6.  축산축종구분코드      |  7.  농가식별번호         |  8.  농장관리번호       |  9.  농가명
            // 10. 브랜드명               | 11. 생년월일               | 12. KPN번호               | 13. 개체성별코드        | 14. 어미소구분코드
            // 15. 어미소축산개체관리번호 | 16. 산차                   | 17. 임신개월수            | 18. 계대                | 19. 계체식별번호
            // 20. 축산개체종축등록번호   | 21. 등록구분번호           | 22. 출하생산지역          | 23. 친자검사결과여부    | 24. 신규여부
            // 25. 우출하중량             | 26. 최초최저낙찰한도금액   | 27. 최저낙찰한도금액      | 28. 비고내용            | 29. 낙유찰결과
            // 30. 낙찰자                 | 31. 낙찰금액               | 32. 응찰일시              | 33. 마지막출품여부      | 34. 계류대번호
            // 35. 초과출장우여부         | 36. 낙찰가                 | 37. 최종변경일            | 38.                     | 39. 월령
            // 40. 등록구분 명            | 41. 체장                   | 42. 체고                  | 43. 체폭                | 44. 십자부고
            // 45. 냉도체중 (등급)        | 46. 냉도체중 (값)          | 47. 배최장근단면적 (등급) | 48. 배최장근단면적 (값) | 49. 등지방두께 (등급) | 50. 등지방두께 (값)
            // 51. 근내지방도 (등급)      | 52. 근내지방도 (값)        | 53. 브루셀라 검사일       | 54. 구제역 예방 접종일  | 55. 우결핵 검사일

            //"SC  |  8808990657202  |  2  |  339  |  1  |  410002164159541  |  01  |  73355  |  1  |  박천경  |    |  21.04.30(44개월 17일)  |  KPN1321  |  수  |  혈통  |  410002116992264  |  3  |  0  |  7  |  진안21-07-5954  |  232187979  |  03  |  진안  |  2  |  1  |  0  |  300  |  300  |  송아지 테스트  AA1-2  |  11  |    |  0  |    |  N  |  2  |  N  |  0  |  20250117081625  |    |  45  |  고등  |  0  |  0  |    |    |    |    |    |    |    |    |    |  "

            if (data == null || data.Length < 35)
                return gv;

            int i = -1;
            if (int.TryParse(data[34], out i))
                gv.SpaceIndex = i.ToString();               //계류대 번호

            i = -1;
            if (int.TryParse(data[2], out i))
                gv.SipNumber = i.ToString();                //출품번호 - 기준!

            gv.CowDistinction = SafeGet(data, 4);

            // 성별 파싱
            var sexCode = SafeGet(data, 13);

            if (!string.IsNullOrEmpty(sexCode) && !string.IsNullOrEmpty(gv.CowDistinction))
            {
                if (userInfo.Auction?.ChangeSexName?.ToUpper().Equals("N") == true)
                {
                    gv.Sex = sexCode; //성별 그대로 표출

                    // 💡 N일 때는 성별 데이터가 "-" 이거나 비어있으면 "새끼"로 보정해 줌
                    if (gv.Sex == "-" || gv.Sex == "")
                    {
                        gv.Sex = "새끼";
                    }

                }
                else
                {
                    // 💡 Y일 때는 changeSex() 함수만 호출함
                    gv.Sex = changeSex(sexCode, gv.CowDistinction); //성별, 송아지 비육우 번식우 구분인자
                }
            }

            if (string.IsNullOrEmpty(data[27]) || data[27] == "0" || data[27] == "null") // 최저가
            {
                gv.LowestPrice = "결장";
            }
            else
            {
                gv.LowestPrice = data[27];
            }

            // 💡 "0"이거나 비어있으면 "-"로 변환하여 대입
            gv.Weight = IsNullorEmpty(data[25]);            //중량

            gv.MatherEntityNumber = data[15].Length > 0 ? MatherEntityNumberConverter(data[15]) : "";                //어미 축산개체관리번호

            if (!gv.CowDistinction.Equals("5")) // 염소가 아닌 경우에만 적용
            {
                gv.DataType = data[0];
                gv.Blood = data[18];                            //계대
                // 💡 "0"이거나 빈 값이면 "-"로 변환하여 대입
                gv.Pregnant = IsNullorEmpty(data[17]);         //임실개월수  020103_KIH


                gv.CalvingNumber = data[16];                    //어미 산차

                // gv.RegistrationCategory = data[21];         //등록구분 01:기초, 02:혈통, 03:고등, 09:미등록우
                if (data[21] == "01")
                    gv.RegistrationCategory = "기초";
                else if (data[21] == "02")
                    gv.RegistrationCategory = "혈통";
                else if (data[21] == "03")
                    gv.RegistrationCategory = "고등";
                else
                    gv.RegistrationCategory = "미등";

                gv.MotherLevel = data[14];                      //어미구분
                gv.BloodEntityNumber = data[20];                //혈통등록번호, 축산개체종축등록번호
                gv.KPN = data[12].Replace("KPN", "");           //KPN ?

                gv.Birth = data[11].Replace(" ", "");

                // if (gv.SpaceIndex.Equals("1"))
                // {
                //Debug.WriteLine("");
                //gv = conn.InsertEPDValue(gv, data);
                // }

                //paternityMatch 
                /*
                 * data[23] = 친자검사결과여부
                    1. 일치
                    2. 완전불일치
                    3. 정보없음
                    4. 부 불일치
                    5. 모 불일치
                    6. 부 모 불일치
                 */
                // paternityMatch 파싱
                switch (data[23])
                {
                    case "1":
                        gv.PaternityMatch = "친자일치";
                        break;
/*                    case "2":
                        gv.PaternityMatch = "완전불일치";
                        break;
                    case "3":
                        gv.PaternityMatch = "정보없음";
                        break;
                    case "4":
                        gv.PaternityMatch = "부 불일치";
                        break;
                    case "5":
                        gv.PaternityMatch = "모 불일치";
                        break;
                    case "6":
                        gv.PaternityMatch = "부 모 불일치";
                        break;*/
                    default:
                        gv.PaternityMatch = "-";
                        gv.FrontNoteWord = string.Empty;
                        gv.FrontNoteWordBrush = "Transparent";
                        break;
                }

               

                if (userInfo.Auction?.CowBirth?.ToUpper() == "N")
                {
                    //생년월일 대신 월령으로 표기
                    gv.Birth = BirthMonthConverter(data[11], SafeGet(data, 39));
                }

                // 💡 users.xml 설정값(LowestPriceTitle) 전달 (설정값이 없으면 기본 "최저가")
                gv.LowestPriceTitle = userInfo.Auction?.LowestPriceTitle ?? "최저가";


                //EPD값 넣기
                if (data.Length > 41)
                {
                    gv.BirthMonth = BirthMonthConverter(data[11], data[39]);                         //월령

                    // 체장, 체고, 체폭, 십자부고 넣기
                    gv.BodyLength = IsNullorEmpty(data[41]); //체장
                    gv.BodyHeight = IsNullorEmpty(SafeGet(data, 42)); //체고
                    gv.BodyWidth = IsNullorEmpty(SafeGet(data, 43)); //체폭
                    gv.CrossSectionalArea = IsNullorEmpty(SafeGet(data, 44)); //십자부고

                    //유전능력값 넣기 ( 냉도체중, 배최장근단면적, 등지방두께, 근내지방도 )

                    gv.bodyWeightInColdString = SafeGet(data, 45); //냉도에서의 체중
                    gv.bodyWeightInColdNum = SafeGet(data, 46);

                    gv.longestMuscleCrossSectionString = SafeGet(data, 47); //근육 최장 단면적
                    gv.longestMuscleCrossSectionNum = SafeGet(data, 48);

                    gv.fatThicknessOnBackString = SafeGet(data, 49); //등지방 두께
                    gv.fatThicknessOnBackNum = SafeGet(data, 50);

                    gv.intramuscularFatContentString = SafeGet(data, 51); //근내지방 함량(등급)
                    gv.intramuscularFatContentNum = SafeGet(data, 52); //근내지방 함량(값)

                    var brucellaRaw = SafeGet(data, 53);
                    gv.brucellosisTestDate = !string.IsNullOrEmpty(brucellaRaw)
                        ? DateConverter(brucellaRaw)
                        : "";

                    var fmdRaw = SafeGet(data, 54);
                    gv.footAndMouthDiseaseTestDate = !string.IsNullOrEmpty(fmdRaw)
                        ? DateConverter(fmdRaw, false)
                        : "";

                    var tbRaw = SafeGet(data, 55);
                    gv.tuberculosisTestDate = !string.IsNullOrEmpty(tbRaw)
                        ? DateConverter(tbRaw)
                        : "";



                    // 20251106 추가
                    // 딸송 정보
                    gv.Child_EntityNumber = SafeGet(data, 56); //딸송 귀표번호
                    gv.Child_Sex = SafeGet(data, 57); //딸송 성별
                    gv.Child_Birth = SafeGet(data, 58); //딸송 생년월일
                    gv.Child_Weight = SafeGet(data, 59); //딸송 중량
                    gv.Child_Kpn = SafeGet(data, 60); //딸송 KPN


                    // 뿌리농가참여여부
                    if (!string.IsNullOrEmpty(userInfo.Auction?.IsShowQQuri) && userInfo.Auction?.IsShowQQuri.ToUpper() == "Y")
                    {
                        gv.Is_Ｎh_Excellent = SafeGet(data, 61); //nh 우량
                        gv.Is_Mother_Ｎh_Excellent = SafeGet(data, 62); //어미 소 nh우량 여부
                        gv.Is_Ｎh_ability = SafeGet(data, 63); //nh 유전체검사여부
                        gv.Is_Nh_QQuri = SafeGet(data, 72).ToUpper(); //SafeGet(data, 72); //뿌리농가참여여부

                        if (gv.Is_Ｎh_Excellent.ToUpper() == "N" && gv.Is_Mother_Ｎh_Excellent.ToUpper() == "N")
                        {
                            // Nh 유전능력
                            gv.Nh_ability_1_num = string.Empty; //우량 냉도체중(값)
                            gv.Nh_ability_2_num = string.Empty; //우량 배최장근단면적(값)
                            gv.Nh_ability_3_num = string.Empty; //우량 등지방두께(값)
                            gv.Nh_ability_4_num = string.Empty; //우량 근내지방도(값)

                            gv.Nh_ability_1_str = string.Empty; //우량 냉도체중(등급)
                            gv.Nh_ability_2_str = string.Empty; //우량 배최장근단면적(등급)
                            gv.Nh_ability_3_str = string.Empty; //우량 등지방두께(등급)
                            gv.Nh_ability_4_str = string.Empty; //우량 근내지방도(등급)
                        }
                        else
                        {
                            // Nh 유전능력
                            gv.Nh_ability_1_num = SafeGet(data, 64); //우량 냉도체중(값)
                            gv.Nh_ability_2_num = SafeGet(data, 65); //우량 배최장근단면적(값)
                            gv.Nh_ability_3_num = SafeGet(data, 66); //우량 등지방두께(값)
                            gv.Nh_ability_4_num = SafeGet(data, 67); //우량 근내지방도(값)

                            gv.Nh_ability_1_str = SafeGet(data, 68); //우량 냉도체중(등급)
                            gv.Nh_ability_2_str = SafeGet(data, 69); //우량 배최장근단면적(등급)
                            gv.Nh_ability_3_str = SafeGet(data, 70); //우량 등지방두께(등급)
                            gv.Nh_ability_4_str = SafeGet(data, 71); //우량 근내지방도(등급)

                        }
                    }
                    else if (string.Equals(userInfo.Auction?.IsShowQQuri, "X", StringComparison.OrdinalIgnoreCase))
                    {
                        gv.Is_Nh_QQuri = "X";
                    }



                    if (data.Length > 73)
                    {
                        gv.Reproduction_Sujung_KPN = SafeGet(data, 73); //재생산 수정 KPN
                        gv.Reproduction_Imsin_Sujung_Date = SafeGet(data, 74); //재생산 임신 수정일
                    }


                    //테스트
                    // 기존 로직 유지: 현재는 상수 Y 사용 (필요하면 나중에 data[72]로 변경)
                    /*int numResult = 0;
                    int.TryParse(gv.SipNumber, out numResult);
                    gv.Is_Nh_QQuri = numResult % 2 == 0 ? "Y" : "";// SafeGet(data, 72); ; //SafeGet(data, 72); //뿌리농가참여여부
                    if (gv.Is_Nh_QQuri == "Y")
                    {
                        gv.Nh_ability_1_num = "19.243"; //우량 냉도체중(값)
                        gv.Nh_ability_1_str = "A"; //우량 냉도체중(등급)

                        gv.Nh_ability_2_num = "3.389"; //우량 배최장근단면적(값)
                        gv.Nh_ability_2_str = "B"; //우량 배최장근단면적(등급)

                        gv.Nh_ability_3_num = "-0.688"; //우량 등지방두께(값)
                        gv.Nh_ability_3_str = "C"; //우량 등지방두께(등급)

                        gv.Nh_ability_4_num = "0.505"; //우량 근내지방도(값)
                        gv.Nh_ability_4_str = "D"; //우량 근내지방도(등급)

                        gv.Is_Ｎh_Excellent = "Y"; //nh 우량



                        gv.Is_Ｎh_ability = "Y"; //nh 유전체검사여부
                        gv.Is_Mother_Ｎh_Excellent = "Y";

                        // 우량 유전능력 등급 문자열
                        gv.Nh_ability_Str = $"{gv.Nh_ability_1_str},{gv.Nh_ability_2_str},{gv.Nh_ability_3_str},{gv.Nh_ability_4_str}";
                    }
                    else
                    {
                        gv.bodyWeightInColdString = "A";//냉도에서의 체중
                        gv.bodyWeightInColdNum = SafeGet(data, 46);

                        gv.longestMuscleCrossSectionString = "B"; //근육 최장 단면적
                        gv.longestMuscleCrossSectionNum = SafeGet(data, 48);

                        gv.fatThicknessOnBackString = "C"; //등지방 두께
                        gv.fatThicknessOnBackNum = SafeGet(data, 50);

                        gv.intramuscularFatContentString = "D"; //근내지방 함량(등급)
                        gv.intramuscularFatContentNum = SafeGet(data, 52); //근내지방 함량(값)
                    }*/



                    // 값이 전부 비어있으면 "-"로 표시
                    if (gv.Nh_ability_Str.Replace(",", "").Trim().Length == 0)
                    {
                        gv.Nh_ability_Str = "-";
                    }

                }
            }
            else
            {
                gv.Blood = "-"; //계대
            }

            if (userInfo.Auction?.IsShowOwnerName?.Contains("N") == true)
            {
                gv.OwnerName = data[9]; //출하주 => 농가명

                if (!string.IsNullOrEmpty(gv.OwnerName))
                {
                    if (string.Equals(userInfo.Auction?.IsShowOwnerName, "N", StringComparison.OrdinalIgnoreCase))
                    {
                        if (gv.OwnerName.Length > 2)
                        {
                            gv.OwnerName = gv.OwnerName.Substring(0, 1) + "*" + gv.OwnerName.Substring(2, 1);
                        }
                        else
                        {
                            gv.OwnerName = gv.OwnerName.Substring(0, 1) + "*";
                        }
                    }
                }
            }
            else
            {
                gv.OwnerName = data[9].Length > 3 ? data[9].Substring(0, 3) : data[9]; //출하주 => 농가명
            }

            gv.Location = data[22].Length > 3 ? data[22].Substring(0, 2) : data[22];                         //출하 지역


            gv.ProcessStatus = 8001;                        //경매 진행 상태

            string BidderName = (userInfo.Auction?.BidderName ?? string.Empty).Trim().ToUpper();
            gv.ResultBidderNumber = data[30];
            bool isValidData = data[29].Equals("23") || data[29].Equals("22");
            bool isInvalidData30 = string.IsNullOrEmpty(data[30]) || data[30] == "0" || data[30] == "null";

            if (!isValidData)
            {
                gv.Bidder = "-";
            }
            else if (isInvalidData30)
            {
                gv.Bidder = "-";
            }
            else
            {
                string bidderSource = SafeGet(data, data[0].Equals("SV") ? 36 : 38);
                bidderSource = UrlDecode(bidderSource);
                switch (BidderName)
                {
                    case "Y": // 낙찰자 이름표시
                        gv.Bidder = bidderSource;
                        break;

                    case "X": // 낙찰자 이름표시 + 마스킹 (김*수)
                        gv.Bidder = bidderSource.Length > 3 ? bidderSource.Substring(0, 3) : bidderSource;
                        if (gv.Bidder.Length > 2)
                            gv.Bidder = gv.Bidder.Substring(0, 1) + "*" + gv.Bidder.Substring(2, 1);
                        else if (gv.Bidder.Length > 0)
                            gv.Bidder = gv.Bidder.Substring(0, 1) + "*";
                        break;

                    case "B": // 낙찰자 이름 + 참가번호
                        gv.Bidder = bidderSource;
                        gv.BidderString = bidderSource;
                        gv.BidderNum = data[30];
                        break;

                    default: // 참가번호만 표시
                        gv.Bidder = data[30];
                        break;
                }
            }
            gv.BidderName = userInfo.Auction?.BidderName ?? string.Empty;


            //gv.BidNumber = data[30];               //낙찰번호 => 낙찰자
            if (string.IsNullOrEmpty(data[31]) || data[31] == "0" || data[31] == "null")
            {
                if (data[29].Equals("23") || data[29].Equals("22"))
                    gv.BidPrice = "유찰";                         //낙찰가격
                else
                    gv.BidPrice = "-";                         //낙찰가격
            }

            else
                gv.BidPrice = data[31];                         //낙찰가격 ","표시


            if (!string.IsNullOrEmpty(data[28]))
            {
                gv.Note = UrlDecode(data[28]);                     //비고
            }
            //gv.ModifiedPrice = data[27];                    //수정 최저가

            gv.TraceNumber = data[5]?.Trim() ?? "";
            gv.AuctionResultStatus = data[29];              //경매 결과 (낙유찰)

            gv.IsRunning = false; //경매 진행 중을 나타내는 테두리 깜박임 플래그를 기본값인 false로 초기화

            //전체 개체관리번호 띄어쓰기 포맷팅(9자리를 4자리 - 4자리 - 1자리 형태로 보기 쉽게)
            if (!string.IsNullOrEmpty(data[5])) //15자리 개체관리번호
            {
                string foo = data[5].Substring(6); //9자리 개체관리번호
                string bar = foo[0].ToString() + foo[1].ToString() + foo[2].ToString() + foo[3].ToString() + " " + foo[4].ToString() + foo[5].ToString() + foo[6].ToString() + foo[7].ToString() + " " + foo[8].ToString();
                gv.EntityNumber = bar; //9자리를 4자리 - 4자리 - 1자리 형태로
            }
            else
                gv.EntityNumber = "";

            
            //users.xml 설정 파일의 SelectShowWeight_EPD 옵션값을 체크
            if (userInfo.Auction?.SelectShowWeight_EPD == "N") // 개체화면에 중량을 보여주고 싶다면 Y OR EPD 알파벳만 보여주고 싶다면 N
            {
                gv.SelectShowWeight_EPD = "N";
            }

            //냉도체중 값의 길이가 1자보다 클 때 디버그 로그를 찍기 위해 작성 - 미사용
            if (gv.bodyWeightInColdNum.Length > 1)
            {
                //Debug.WriteLine("");
            }

            // 축협별 요구사항
            ApplyAuctionHouseOverrides(gv, data, userInfo);

            return gv;
        }

        /// <summary>
        /// 공통 파싱이 끝난 값에 축협별 표시 규칙을 적용한다.
        /// </summary>
        private void ApplyAuctionHouseOverrides(gValues gv, string[] data, UserInfo userInfo)
        {
            string? code = userInfo.Auction?.AuctionHouseCode;
            bool hasCustomFrontNote = false;

            switch (code)
            {
                case "8808990656885": // 횡성
                    var sexCode = SafeGet(data, 13);
                    if (userInfo.Auction?.ChangeSexName?.ToUpperInvariant() == "N" &&
                        !string.IsNullOrEmpty(sexCode) && !string.IsNullOrEmpty(gv.CowDistinction))
                    {
                        int birthMonth = 0;

                        string birthText = SafeGet(data, 11); // "26.01.15(8개월 27일)"

                        int start = birthText.IndexOf('(');
                        int end = birthText.IndexOf("개월");

                        if (start >= 0 && end > start)
                        {
                            string monthText = birthText.Substring(start + 1, end - start - 1);
                            int.TryParse(monthText, out birthMonth);
                        }

                        gv.Sex = HoengseongSex(gv.CowDistinction, sexCode, birthMonth);
                    }
                    break;

                case "8808990227283": // 익산·군산
                    string location = SafeGet(data, 22);
                    gv.Location = location.Contains("익산", StringComparison.Ordinal) ||
                                  location.Contains("군산", StringComparison.Ordinal)
                        ? "관내"
                        : "관외";

                    if (!string.Equals(userInfo.Auction?.IsPaternityMatch?.Trim(), "N", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] paternityKeywords = { "친자일치", "친자 일치", "친자확인", "친자 확인" };
                        var paternityMatch = paternityKeywords
                            .Select(keyword => new
                            {
                                Keyword = keyword,
                                Index = gv.Note.IndexOf(keyword, StringComparison.Ordinal)
                            })
                            .Where(match => match.Index >= 0)
                            .OrderBy(match => match.Index)
                            .FirstOrDefault();

                        if (paternityMatch != null)
                        {
                            gv.PaternityMatch = "친자일치";
                            gv.Note = gv.Note
                                .Remove(paternityMatch.Index, paternityMatch.Keyword.Length)
                                .Trim(' ', ',');
                        }
                    }
                    break;

                case "8808990657639": // 상주
                    int.TryParse(gv.CowDistinction, out int cowDistinction);
                    gv.Location = cowDistinction < 4 ? "축주" : string.Empty;
                    break;

                case "8808990656687": // 영천
                case "8808990683973": // 음성
                    if (gv.CowDistinction == "3")
                    {
                        string raw = SafeGet(data, 17);
                        if (string.IsNullOrEmpty(raw) || raw == "0")
                            gv.Pregnant = "X";
                        else if (code == "8808990683973" && int.TryParse(raw, out int month) && month <= 3)
                            gv.Pregnant = "미정";
                        else
                            gv.Pregnant = raw;
                    }
                    break;

                case "8808990817675": // 장성
                    hasCustomFrontNote = true;
                    string[] frontKeywords = { "친자", "으뜸", "혈통" };
                    var firstMatch = frontKeywords
                        .Select(keyword => new
                        {
                            Keyword = keyword,
                            Index = gv.Note.IndexOf(keyword, StringComparison.Ordinal)
                        })
                        .Where(match => match.Index >= 0)
                        .OrderBy(match => match.Index)
                        .FirstOrDefault();

                    switch (firstMatch?.Keyword)
                    {
                        case "친자":
                            gv.FrontNoteWord = "친자";
                            gv.PaternityMatch = "친자일치";
                            gv.FrontNoteWordBrush = "Lime";
                            break;
                        case "으뜸":
                            gv.FrontNoteWord = "으뜸";
                            gv.PaternityMatch = "친자일치";
                            gv.FrontNoteWordBrush = "Aqua";
                            break;
                        case "혈통":
                            gv.FrontNoteWord = "혈통";
                            gv.PaternityMatch = "친자일치";
                            gv.FrontNoteWordBrush = "Yellow";
                            break;
                        default:
                            gv.FrontNoteWord = string.Empty;
                            gv.PaternityMatch = string.Empty;
                            break;
                    }

                    if (firstMatch != null)
                    {
                        gv.Note = gv.Note
                            .Remove(firstMatch.Index, firstMatch.Keyword.Length)
                            .Trim(' ', ',');
                    }
                    break;
            }

            if (!hasCustomFrontNote && gv.PaternityMatch == "친자일치")
            {
                gv.FrontNoteWord = "친자";
                gv.FrontNoteWordBrush = "Yellow";
            }
            else if (!hasCustomFrontNote)
            {
                gv.FrontNoteWord = string.Empty;
            }

            if (userInfo.Auction?.IsPaternityMatch?.Trim().ToUpperInvariant() == "N")
            {
                gv.PaternityMatch = "-";
                gv.FrontNoteWord = string.Empty;
                gv.FrontNoteWordBrush = "Transparent";
            }
        }

        /// <summary>
        /// 축종 구분에 맞춰 성별 표기를 변환한다.
        /// </summary>
        private string changeSex(string sex, string CowDistinction)
        {
            // 💡 성별 값이 없거나 "-"인 경우 축종에 관계없이 "새끼"로 기본 설정[cite: 1]
            if (string.IsNullOrEmpty(sex) || sex == "-")
            {
                return "새끼";
            }
            switch (CowDistinction)
            {
                case "1": //송아지
                    break;
                case "2": //비육우
                    if (sex.Equals("암") || sex.Equals("수"))
                    {
                        sex = "비육";
                    }
                    break;
                case "3": //번식우
                    if (sex.Equals("암"))
                    {
                        sex = "암소";
                    }
                    else if (sex.Equals("수"))
                    {
                        sex = "숫소";
                    }
                    break;
                case "5": //염소
                    break;
                default: break;

            }
            return sex;
        }

        /// <summary>
        /// 빈 값이나 0을 기본 표시값으로 변환한다.
        /// </summary>
        private string IsNullorEmpty(string data)
        {
            if (string.IsNullOrEmpty(data) || data=="0")
            {
                return "-";
            }
            else
            {
                return data;
            }
        }


        /// <summary>
        /// 횡성용 등록구분, 성별, 개월령
        /// 횡성에서 온 성별 관련 문자내용
        /// 9개월 이하 암
        /// 10개월 이상 암소
        /// 
        /// 9개월 이하 거세
        /// 10개월 이상 비육
        /// 
        /// 5개월 이하 수
        /// 
        /// 그런데 사실 미만으로 계산하는것이 맞음
        /// </summary>
        private string HoengseongSex(string cowDistinction, string sex, int month)
        {
            // 성별이 비어있으면 처리할 게 없음
            if (string.IsNullOrEmpty(sex))
                return sex;

            // 횡성은 거세 및 비육우를 비육으로 표출
            if (sex == "거세" || cowDistinction == "2")
            {
                return month < 9 ? "거세" : "비육";
            }

            // 암은 송아지에서 1글자, 성우/번식우에서 2글자로 표출
            if (sex == "암")
                return month < 9 ? "암" : "암소";
                //return cowDistinction == "1" && month < 9 ? "암" : "암소";

            // 수는 월령이나 축종 구분에 의해 암소로 변환하지 않음
            if (sex == "수")
                return "수";

            if (sex == "프리마틴" || sex == "프리")
            {
                sex = "프";
            }

            return sex;
        }


        /// <summary>
        /// 비고 데이터 중 특수 기호 처리
        /// </summary>
        private string UrlDecode(string text)
        {
            return HttpUtility.HtmlDecode(text);
        }

        private string DateConverter(string data, bool includeElapsed = true)
        {
            // ex: "20220502" → "22.05.02" or "22.05.02(경과일)"
            if (string.IsNullOrEmpty(data) || data.Length < 8)
                return "";

            // yyyyMMdd 형식 가정
            string yearPart = data.Substring(0, 4);
            string monthPart = data.Substring(4, 2);
            string dayPart = data.Substring(6, 2);

            // 숫자 변환 실패 시 빈 값 반환
            if (!int.TryParse(yearPart, out int year) ||
                !int.TryParse(monthPart, out int month) ||
                !int.TryParse(dayPart, out int day))
            {
                return "";
            }

            // 표시용 날짜 (YY.MM.DD)
            string formattedDate = yearPart.Substring(2, 2) + "." + monthPart + "." + dayPart;

            if (!includeElapsed)
            {
                // 예: 20220502 → 22.05.02
                return formattedDate;
            }

            // 실제 DateTime 구성 (잘못된 날짜는 실패 처리)
            DateTime date;
            if (!DateTime.TryParse($"{year:D4}-{month:D2}-{day:D2}", out date))
            {
                return formattedDate;
            }

            int daysElapsed = (DateTime.Now.Date - date.Date).Days;
            if (daysElapsed < 0) daysElapsed = 0; // 미래 날짜가 들어오면 0일로 처리

            string daysDisplay = daysElapsed > 100 ? $"{daysElapsed}" : $"{daysElapsed}일";

            // 예: 20220502 → 22.05.02(30일) 또는 22.05.02(120)
            return $"{formattedDate}({daysDisplay})";
        }


        /// <summary>
        /// 월령으로 변환 ex 22.05.02(8개월 17일) → 22.05.02(9개월령)
        /// </summary>
        /// <param name="birth">예: 22.05.02(8개월 17일)</param>
        /// <param name="brithMonth">예: 9</param>
        /// <returns>예: 22.05.02(9개월령)</returns>
        private string BirthMonthConverter(string birth, string brithMonth)
        {
            if (string.IsNullOrEmpty(birth) || string.IsNullOrEmpty(brithMonth))
                return "";

            // 괄호 있는 경우: 앞부분만 추출
            int bracketIndex = birth.IndexOf('(');
            string dateOnly = bracketIndex >= 0 ? birth.Substring(0, bracketIndex) : birth;

            return $"{dateOnly.Trim()}({brithMonth}개월령)";
        }




        private string MatherEntityNumberConverter(string data)
        {
            // 예: 410002129837506 → 002 1298 3750 6
            if (string.IsNullOrEmpty(data) || data.Length < 15)
                return data;

            data = data.Substring(3); // 12자리: 002129837506

            string part1 = data.Substring(0, 3);
            string part2 = data.Substring(3, 4);
            string part3 = data.Substring(7, 4);
            string part4 = data.Substring(11, 1);

            return $"{part2} {part3} {part4}";
        }

        /// <summary>
        /// 배열 범위를 확인한 뒤 안전하게 값을 반환한다.
        /// </summary>
        private static string SafeGet(string[] data, int index)
        {
            if (data == null)
                return "";

            return (index >= 0 && index < data.Length) ? data[index] : "";
        }


    }
}
