// 데이터 파싱 서비스(AnimalParseData) - 서버에서 전달받은 경매 데이터 문자열을 객체로 변환하는 서비스 클래스
using CowAuctionSmall.Models;
using CowAuctionSmall.Models.Structures;
using System;
using System.Linq;
using System.Reflection.Emit;
using System.Web;

namespace CowAuctionSmall.Services
{
    /// <summary>
    /// 경매 데이터 소,염소 등의 String data "asd | 123 | 2"을 처리해서 객체로 전환
    /// 축협마다 원하는 데이터가공이 필요하는 경우가 종종 있다
    /// 예를들면 횡성의 경우 송아지의 경우 9개월 부터는 비육으로 표시 등
    /// 
    /// 하지만 최대한 서버가 보내준 데이터를 그대로 표출하려고 해야한다. 너무 맞춤일 경우 .. 
    /// </summary>
    public class AnimalParseData
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
            if (message == null || userInfo == null || conn == null)
            {
                logger.LogError("Parse_PacketApi \nmessage : " + (message ?? "null") + "\nuserInfo : " + (userInfo?.ToString() ?? "null") + "\nconn : " + (conn?.ToString() ?? "null"));
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

            // 💡 [선언 위치 이동] 메서드 전체에서 접근 가능하도록 공통 변수를 상단으로 끌어올림
            var code = userInfo.Auction?.AuctionHouseCode;
            var showPaternity = userInfo.Auction?.IsPaternityMatch?.Trim().ToUpper() ?? "Y";

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

                    if (code == "8808990656885") //횡성
                    {
                        // TODO: HoengseongSex 리팩토링 후에는 반환값을 gv.Sex에 반영
                        gv.Sex = HoengseongSex(gv.CowDistinction, SafeGet(data, 39), sexCode);
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
                        break;
                }

                // user.xml 설정값(IsPaternityMatch)이 "N"인 경우 친자일치 값을 강제로 비활성화("-")
                if (showPaternity == "N")
                {
                    gv.PaternityMatch = "-";
                }

                if (userInfo.Auction?.CowBirth?.ToUpper() == "N")
                {
                    //생년월일 대신 월령으로 표기
                    gv.Birth = BirthMonthConverter(data[11], data[39]);
                }

                // 💡 users.xml 설정값(LowestPriceTitle) 전달 (설정값이 없으면 기본 "최저가")
                gv.LowestPriceTitle = userInfo.Auction?.LowestPriceTitle ?? "최저가";


                //EPD값 넣기
                if (data.Length > 41)
                {
                    gv.BirthMonth = BirthMonthConverter(data[11], data[39]);                         //월령

                    // 체장, 체고, 체폭, 십자부고 넣기
                    gv.BodyLength = IsNullorEmpty(data[41]); //체장
                    gv.BodyHeight = IsNullorEmpty(data[42]); //체고
                    gv.BodyWidth = IsNullorEmpty(data[43]); //체폭
                    gv.CrossSectionalArea = IsNullorEmpty(data[44]); //십자부고

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
                    else if (userInfo.Auction?.IsShowQQuri.ToUpper() == "X")
                    {
                        gv.Is_Nh_QQuri = "X";
                    }

                    if (gv.SpaceIndex =="45")
                    {
                        int a = 0;
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

            if (string.Equals(userInfo.Auction?.AuctionHouseCode, "8808990227283", StringComparison.Ordinal)) // 익산 군산의 경우
            {
                if (gv.Location.Contains("익산") || gv.Location.Contains("군산"))
                {
                    gv.Location = "관내";
                }
                else
                {
                    gv.Location = "관외";
                }
                
            }
            else if (string.Equals(userInfo.Auction?.AuctionHouseCode, "8808990657639", StringComparison.Ordinal)) //상주 경우
            {
                int.TryParse(gv.CowDistinction, out int cowDistinctionNum);

                if (cowDistinctionNum < 4) 
                {
                    gv.Location = "축주";
                }
                else                
                {
                    gv.Location = "";
                }
            }

            gv.ProcessStatus = 8001;                        //경매 진행 상태

            string BidderName = (userInfo.Auction?.BidderName ?? string.Empty).Trim().ToUpper();
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
                string bidderSource = data[0].Equals("SV") ? data[36] : data[38];
                bidderSource = UrlDecode(bidderSource);
                switch (BidderName)
                {
                    case "Y": // 낙찰자 이름표시
                        if (userInfo.Auction.AuctionHouseCode.Equals("8808990656106") || userInfo.Auction.AuctionHouseCode.Equals("8808990643625"))//해남진도..
                        {
                            gv.Bidder = bidderSource.Length > 5 ? bidderSource.Substring(0, 5) : bidderSource;
                        }
                        else
                        {
                            gv.Bidder = bidderSource.Length > 3 ? bidderSource.Substring(0, 3) : bidderSource;
                        }
                        if (gv.Is_Nh_QQuri =="Y" || gv.Nh_ability_1_num.Length>2)
                        {
                            gv.Bidder = bidderSource;
                        }
                        
                        break;

                    case "X": // 낙찰자 이름표시 + 마스킹 (김*수)
                        gv.Bidder = bidderSource.Length > 3 ? bidderSource.Substring(0, 3) : bidderSource;
                        if (gv.Bidder.Length > 2)
                            gv.Bidder = gv.Bidder.Substring(0, 1) + "*" + gv.Bidder.Substring(2, 1);
                        else
                            gv.Bidder = gv.Bidder.Substring(0, 1) + "*";
                        break;

                    case "B": // 낙찰자 이름 + 참가번호
                        gv.Bidder =  bidderSource;
                        gv.BidderNum = data[30];
                        gv.BidderString = bidderSource.Length > 7 ? bidderSource.Substring(0,7) : bidderSource;
                        if (gv.Is_Nh_QQuri == "Y" || gv.Nh_ability_1_num.Length > 2)
                        {
                            gv.BidderString = bidderSource;
                        }
                        break;

                    default: // 참가번호만 표시
                        gv.Bidder = data[30];
                        break;
                }
            }
            gv.BidderName = userInfo.Auction.BidderName;


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

            
            if (!string.IsNullOrEmpty(data[5]))
            {
                if (!gv.CowDistinction.Equals("5")) // 소(1~3)인 경우
                {
                    string foo = data[5].Substring(10, 4);
                    gv.EntityNumberShort = foo;
                }
                else // 염소(5)인 경우
                {
                    string foo = data[5].Substring(11);
                    gv.EntityNumberShort = foo;
                }

            }
            else
                gv.EntityNumberShort = "";

            
            if (gv.CowDistinction == "5") //축종 구분이 염소("5")인 경우에 작동
            {
                // 4자리 중 뒤 3자리만 보여주기
                if (!string.IsNullOrEmpty(gv.EntityNumberShort) && gv.EntityNumberShort.Length == 4)
                {
                    gv.EntityNumberShort = gv.EntityNumberShort.Substring(1);
                }
            }

            //users.xml 설정 파일의 SelectShowWeight_EPD 옵션값을 체크
            if (userInfo.Auction.SelectShowWeight_EPD != null && userInfo.Auction.SelectShowWeight_EPD.Equals("N")) // 개체화면에 중량을 보여주고 싶다면 Y OR EPD 알파벳만 보여주고 싶다면 N
            {
                gv.SelectShowWeight_EPD = "N";
            }

            //냉도체중 값의 길이가 1자보다 클 때 디버그 로그를 찍기 위해 작성 - 미사용
            if (gv.bodyWeightInColdNum.Length > 1)
            {
                //Debug.WriteLine("");
            }

            //특정 축협(영천/보령) 번식우 임신개월수 예외 처리
            if ((code == "8808990656687" || code == "8808990683973") && gv.CowDistinction == "3")
            {
                var raw = (data != null && data.Length > 17) ? data[17] : null;

                if (string.IsNullOrEmpty(raw) || raw == "0")
                    gv.Pregnant = "X";
                else if (code == "8808990683973" && int.TryParse(raw, out var w) && w <= 4) //보령축협이면서 임신개월수가 4개월 이하인 경우 $\rightarrow$ "미정" 처리
                    gv.Pregnant = "미정";
                else
                    gv.Pregnant = raw;
            }

            // ===================================================
            // 2. [하단] 익산군산(8808990227283) 비고란 파싱 예외 구간
            // ===================================================
            // 💡 상단에서 끌어올린 showPaternity, code 변수를 사용합니다.
            if (showPaternity != "N" && code == "8808990227283" && !string.IsNullOrEmpty(gv.Note))
            {
                string[] keywords =
                {
                    "친자일치",
                    "친자 일치",
                    "친자확인",
                    "친자 확인"
                };

                if (keywords.Any(k => gv.Note.Contains(k)))
                {
                    gv.PaternityMatch = "친자일치"; // 옵션이 "Y"일 때만 뱃지가 생성됨

                    foreach (var keyword in keywords)
                    {
                        gv.Note = gv.Note.Replace(keyword + ",", "");
                        gv.Note = gv.Note.Replace(keyword, "");
                    }

                    gv.Note = gv.Note.Trim().Trim(',');
                }
            }

            return gv;
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
        /// 횡성용 등록구분, 개월령 , 성별
        /// </summary>
        private string HoengseongSex(string cowDistinction, string birthMonth, string sex)
        {
            // 성별이 비어있으면 처리할 게 없음
            if (string.IsNullOrEmpty(sex))
                return sex;

            // 개월 수 파싱 (실패하면 0개월로 간주)
            int month = 0;
            int.TryParse(birthMonth, out month);

            switch (cowDistinction)
            {
                case "1": // 송아지
                    /*    if (month >= 5 && sex == "수")
                        {
                            sex = "거세";
                        } */   // 송아지: 서버에서 수/암/거세 전달된 값 그대로 유지
                    break;

                case "2": // 비육우
                    if (month >= 9)
                    {
                        sex = "비육";
                    }
                    break;

                case "3": // 번식우
                    if (sex == "암" || month >= 9)
                    {
                        sex = "암소";
                    }
                    break;

                case "5": // 염소
                    if (string.IsNullOrEmpty(sex) || sex == "-")
                    {
                        sex = "새끼";
                    }
                    break;

                default:
                    break;
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