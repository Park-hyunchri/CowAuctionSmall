using CowAuctionSmall.Models.Structures;
using Microsoft.Extensions.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CowAuctionSmall.Models
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
        public AnimalParseData() 
        {
            logger = NLogger.Instance;
        }


        public gValues Parse_PacketApi(string message , UserInfo userInfo, ServerConn conn)
        {
            if (message == null || userInfo== null || conn ==null)
            {
                logger.LogError("Parse_PacketApi \nmessage : " + message + "\nuserInfo : " + userInfo.ToString() + "\nconn : " + conn.ToString()+ "");
                
            }

            gValues gv = new gValues();
            string[] data = message.Split('|');
            //0:구분자 | 1:조합구분코드 | 2:출품번호 | 3:경매회차 | 4:경매대상구분코드 | 5:축산개체관리번호 | 6:축산축종구분코드 |
            //7:농가식별번호 | 8:농장관리번호 | 9:농가명 | 10:브랜드명 | 11:생년월일 | 12:KPN번호 | 13:개체성별코드 | 14:어미소구분코드 |
            //15:어미소축산개체관리번호 | 16:산차 | 17:임신개월수 | 18:계대 | 19:계체식별번호 | 20:축산개체종축등록번호 | 21:등록구분번호 |
            //22:출하생산지역 | 23:친자검사결과여부 | 24:신규여부 | 25:우출하중량 | 26:최초최저낙찰한도금액 | 27:최저낙찰한도금액 | 28:비고내용 |
            //29:낙유찰결과 | 30:낙찰자 | 31:낙찰금액 | 32:응찰일시 | 33:마지막출품여부 | 34:계류대번호 | 35:초과출장우여부

            if (data == null || data.Length < 35)
                return gv;

            int i = -1;
            if (int.TryParse(data[34], out i))
                gv.SpaceIndex = i.ToString();               //계류대 번호

            i = -1;
            if (int.TryParse(data[2], out i))
                gv.SipNumber = i.ToString();                //출품번호 - 기준!

            gv.CowDistinction = data[4];



            if ((data[13] != null || data[13].Length > 0) && (data[4] != null || data[4].Length > 0))
            {
                if (userInfo.Auction.ChangeSexName.ToUpper().Equals("N"))
                {
                    gv.Sex = data[13]; //성별 그대로 표출
                }
                else
                {
                    gv.Sex = changeSex(data[13], data[4]); //성별, 송아지 비육우 번식우 구분인자
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

            gv.Weight = data[25];                           //중량


            if (!gv.CowDistinction.Equals("5")) // 염소가 아닌 경우에만 적용
            {
                gv.DataType = data[0];
                gv.Blood = data[18];                            //계대
                gv.BirthMonth = data[11].Substring(data[11].IndexOf("개월") - 2, 2);                         //월령 230331 KSW
                gv.Pregnant = data[17];                        //임실개월수  020103_KIH

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

                //EPD값 넣기 
                gv = conn.InsertEPDValue(gv, data);
            }
            else
            {
                gv.Blood = "-"; //계대

                //염소 kg 당 가격
                /*decimal lowprice = Convert.ToDecimal(gv.LowestPrice)*1000;
                decimal weight = Convert.ToDecimal(gv.Weight);
                if (lowprice > 0 && weight > 0)
                {
                    gv.GoatPricePerKg = Math.Round(lowprice / weight, 1);
                    gv.Birth = "KG당 : "+gv.GoatPricePerKg.ToString();
                }
                else
                {
                    gv.Birth = "-";
                }*/

            }


            if (userInfo.Auction.IsShowOwnerName.Contains("N"))
            {
                gv.OwnerName = data[9];                         //출하주 => 농가명

                if (!String.IsNullOrEmpty(gv.OwnerName) && gv.OwnerName.Length > 5)
                {
                    //농가 이름 자르기
                    gv.OwnerName = gv.OwnerName.Substring(0, 4);
                    if (userInfo.Auction.IsShowOwnerName.Equals("N"))
                    {
                        gv.OwnerName = gv.OwnerName.Substring(0, 1) + "*" + gv.OwnerName.Substring(2);
                    }

                }

            }
            else
            {
                gv.OwnerName = data[9].Length > 5 ? data[9].Substring(0, 4) : data[9];                         //출하주 => 농가명
            }


            gv.Location = data[22].Length > 3 ? data[22].Substring(0, 2) : data[22];                         //출하 지역
            gv.ProcessStatus = 8001;                        //경매 진행 상태


            string BidderName = userInfo.Auction.BidderName.Trim().ToUpper();
            if (BidderName.Equals("Y"))
            {
                if (data[29].Equals("23") || data[29].Equals("22"))
                {
                    if (string.IsNullOrEmpty(data[30]) || data[30] == "0" || data[30] == "null")
                        gv.Bidder = "-";
                    else
                    {
                        if (data[0].Equals("SV"))
                        {
                            gv.Bidder = data[36];        // 낙찰자 이름
                        }
                        else
                        {
                            gv.Bidder = data[38];        // 낙찰자 이름
                        }
                    }
                }
                else
                {
                    gv.Bidder = "-";        // 낙찰자 참가번호
                }

            }
            else if (BidderName.Equals("B"))
            {
                if (data[29].Equals("23") || data[29].Equals("22"))
                {
                    if (string.IsNullOrEmpty(data[30]) || data[30] == "0" || data[30] == "null")
                        gv.Bidder = "-";
                    else
                    {
                        if (data[0].Equals("SV"))
                        {
                            gv.Bidder = data[36] + "(" + data[30] + ")";        // 낙찰자 참가번호

                        }
                        else
                        {
                            gv.Bidder = data[38] + "(" + data[30] + ")";        // 낙찰자 참가번호
                        }
                    }
                }
                else
                {
                    gv.Bidder = "-";        // 낙찰자 참가번호
                }
            }
            else
            {
                if (data[29].Equals("23") || data[29].Equals("22"))
                {
                    if (string.IsNullOrEmpty(data[30]) || data[30] == "0" || data[30] == "null")
                        gv.Bidder = "-";
                    else
                        gv.Bidder = data[30];        // 낙찰자 참가번호
                }
                else
                {
                    gv.Bidder = "-";        // 낙찰자 참가번호
                }
            }

            //gv.BidNumber = data[30];               //낙찰번호 => 낙찰자
            if (string.IsNullOrEmpty(data[31]) || data[31] == "0" || data[31] == "null")
            {
                if (data[29].Equals("23") || data[29].Equals("22"))
                    gv.BidPrice = "유찰";                         //낙찰가격
                else
                    gv.BidPrice = "-";                         //낙찰가격
            }

            else
                gv.BidPrice = data[31];                         //낙찰가격


            if (!string.IsNullOrEmpty(data[28]))
            {
                gv.Note = UrlDecode(data[28]);                     //비고
            }
            //gv.ModifiedPrice = data[27];                    //수정 최저가

            gv.AuctionResultStatus = data[29];              //경매 결과 (낙유찰)

            gv.IsRunning = false; //진행중 깜박임 제어

            if (!string.IsNullOrEmpty(data[5]))
            {
                string foo = data[5].Substring(6);
                string bar = foo[0].ToString() + foo[1].ToString() + foo[2].ToString() + foo[3].ToString() + " " + foo[4].ToString() + foo[5].ToString() + foo[6].ToString() + foo[7].ToString() + " " + foo[8].ToString();
                gv.EntityNumber = bar;
            }
            else
                gv.EntityNumber = "";

            if (!string.IsNullOrEmpty(data[5]))
            {
                string foo = data[5].Substring(10, 4);
                gv.EntityNumberShort = foo;
            }
            else
                gv.EntityNumberShort = "";



            return gv;
        }

        private string changeSex(string sex, string CowDistinction)
        {
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
                case "5": //염수
                    if (sex.Equals("") || sex.Equals("-"))
                    {
                        sex = "새끼";
                    }
                    break;
                default: break;

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
    }
}
