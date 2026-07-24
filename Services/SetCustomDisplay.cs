// 화면 제어 컨트롤러
using CowAuctionSmall.Views;
using CowAuctionSmall.Views.SIze_160_64.Running;
using CowAuctionSmall.Views.Size_320_64.Running;
using CowAuctionSmall.Views.Size128_128;
using CowAuctionSmall.Views.Size128_128.CustomAuctionSold;
using CowAuctionSmall.Views.Size128_128.CustomAUctionUnSold;
using CowAuctionSmall.Views.Size128_128.Running;
using CowAuctionSmall.Views.Size128_128.Running.CustomAuctionRunning1;
using System.Windows.Controls;

namespace CowAuctionSmall.Services
{
    public class SetCustomDisplay
    {
        /// <summary>
        /// 개체정보 페이지 어미 산차 중량 최저가 등.. 표시
        /// </summary>
        /// <param name="nhCode"></param>
        /// <returns></returns>
        public UserControl CustomAuctionRunning1_128(string nhCode ,string is_QQuri, string CowDistinction,string is_Ｎh_Excellent, string is_Mother_Ｎh_Excellent)
        {
            if (CowDistinction == "5" || CowDistinction == "6")
            {
                return new Standard_Goat_Run(); // 염소 또는 말
            }
            // 춘천 축협 전용 코드 분기 추가
            if (nhCode == "8808990656229")
            {
                // 춘천 전용 경매 진행 화면을 반환합니다
                return new ChuncheonRun();
            }

            // ... 기존 다른 조합 분기 코드들 ...
            if (is_QQuri.ToUpper() == "X")
            {
                return new Standard_non_X_Run1(); //구 화면으로 표출 뿌리농가 적용X 랑 차이점은 완전 구버전 화면
            }
            else if (is_QQuri.ToUpper() == "Y" || is_Mother_Ｎh_Excellent.Length>2)
            {
                return new StandardQQuri_Run1(); //뿌리농가 적용 O ==============================================================> 임시 0708
            }
            else
            {
                switch (nhCode)
                {
                    case "8808990657202": // 무진장 낙찰 페이지  
                        return new Yecheon_v2();   //뿌리농가 적용                           
                    default:
                        return new HoengseongRun();
                        //return new Standard_non_QQuri_Run1(); //뿌리농가 적용 X

                }
            }
        }

        /// <summary>
        /// 유전능력 페이지
        /// </summary>
        /// <param name="nhCode"></param>
        /// <returns></returns>
        public UserControl CustomAuctionRunning2_128(string nhCode , string is_QQuri, string CowDistinction, string is_Ｎh_Excellent, string is_Mother_Ｎh_Excellent)
        {
            // ... 기존 염소/말 등의 예외 처리 코드 ...
            if (CowDistinction == "5" || CowDistinction == "6")
            {
                return new Standard_Goat_Run(); // (예시) 염소 화면 반환 [cite: 562]
            }

            // 춘천 축협(nhCode: 8808990656229) 조건 분기 추가! [cite: 562]
            if (nhCode == "8808990656229")
            {
                // 표준 유전능력 전광판 화면(AuctionRunning2)을 반환합니다 [cite: 53]
                return new AuctionRunning2();
            }

            // ... 기존 타 축협 분기 코드 및 기본값 반환 ...
            if (is_QQuri == "X") //뿌리농가 적용 O
            {
                if (CowDistinction != "3")
                {
                    return new AuctionRunning2(); //유전능력
                }
                else
                {
                    return new AuctionRunning2_3(); //번식우 유전능력
                }
            }

            if (is_QQuri == "Y") //뿌리농가 적용 O
            {
                if (is_Ｎh_Excellent == "N" && is_Mother_Ｎh_Excellent == "N")
                {
                    if (CowDistinction != "3")
                    {
                        return new AuctionRunning2(); //유전능력
                    }
                    else
                    {
                        return new AuctionRunning2_3(); //번식우 유전능력
                    }
                }
                else
                {
                    if (CowDistinction != "3")
                    {
                        return new AuctionRunning3(); //농협유전능력
                    }
                    else
                    {
                        return new AuctionRunning4(); //농협유전능력 번식우
                    }

                }
            }
            else //뿌리농가 적용 X
            {
                if (CowDistinction != "3")
                {
                    return new AuctionRunning2(); //유전능력
                }
                else
                {
                    return new AuctionRunning2_3(); //번식우 유전능력
                }
            }

        }

        /// <summary>
        /// 낙찰 페이지
        /// </summary>
        /// <param name="nhCode"></param>
        /// <returns></returns>
        public UserControl CustomAuctionSold_128(string nhCode,string bidderCode ,string is_QQuri, string CowDistinction, string nh_ability_1_num)
        {
            int.TryParse(CowDistinction, out int result);
            if (result > 4) // 염소 또는 말
            {
                return new GoatSold();
            }

            switch (nhCode)
            {
                case "8808990656687": // 영천낙찰 페이지
                    return new Standard_non_X_Sold();
                case "8808990656526": // 제천단양 낙찰 페이지
                    return new JecheonDanyangSold();
                case "8808990684321": // 보령 낙찰 페이지
                    return new JecheonDanyangSold();
                case "8808990656229": // 춘천 낙찰 페이지
                    return new ChuncheonSold();
                //return new GoatSold();
                case "8808990657202": // 무진장 낙찰 페이지
                    return new YecheonSold();  

                default:

                    return new QQuriSold();

            }
        }

        /// <summary>
        /// 유찰 페이지
        /// </summary>
        /// <param name="nhCode"></param>
        /// <returns></returns>
        public UserControl CustomAuctionUnSold_128(string nhCode, string is_QQuri, string CowDistinction, string nh_ability_1_num)
        {
            int.TryParse(CowDistinction, out int result);
            if (result > 4) // 염소 또는 말
            {
                return new GoatUnSold();
            }

            if (nhCode == "8808990656953") // 정읍    
                {
                return new NamwonUnSold();
            }
            if (nhCode == "8808990684321") // 보령
            {
                return new Standard_non_X_UnSold();
            }
            if (nhCode == "8808990656229") // 춘천
            {
                return new ChuncheonUnSold();
            }
            if (is_QQuri.ToUpper() == "Y" || nh_ability_1_num.Length > 2)
            {
                return new QQuriUnSold();
            }
            else if (is_QQuri.ToUpper() == "X")
            {
                return new Standard_non_X_UnSold();
            }
            else
            {
                //return new Non_QQuriUnsold();//
                return new QQuriUnSold();
            }

                
        }

        //-----------------------------------------------
        /// <summary>
        /// 무진장의 경우 160,64 320,64 화면이 각각 있음 (라인형의 문제)
        /// </summary>

        /// <summary>
        /// 160x64 진행 페이지(1)를 축협 조건에 맞게 선택한다.
        /// </summary>
        public UserControl CustomAuctionRunning1_160_64(string nhCode, string is_QQuri, string CowDistinction)
        {
            switch (nhCode)
            { // 테스트 8808990657202
                case "8808990657202": // 무진장 P3 160_64 독자적 화면
                    return new AuctionRunning1_160_64();
                default:
                    return new AuctionRunning1_160_64();
            }
        }
        /// <summary>
        /// 160x64 진행 페이지(2)를 축협 조건에 맞게 선택한다.
        /// </summary>
        public UserControl CustomAuctionRunning2_160_64(string nhCode, string is_QQuri, string CowDistinction)
        {
            switch (nhCode)
            { // 테스트 8808990657202
                case "8808990657202":  // 무진장 P3 160_64 독자적 화면
                    return new AuctionRunning2_160_64();
                default:
                    return new AuctionRunning2_160_64();
            }
        }

        /// <summary>
        /// 320x64 진행 페이지(1)를 축협 조건에 맞게 선택한다.
        /// </summary>
        public UserControl CustomAuctionRunning1_320_64(string nhCode, string is_QQuri, string CowDistinction)
        {
            switch (nhCode)
            { // 테스트 8808990657202
                case "8808990657202": // 하동 P3 320_64 독자적 화면
                    return new AuctionRunning1_320_64();
                default:
                    return new AuctionRunning1_320_64();
            }
        }
        /// <summary>
        /// 320x64 진행 페이지(2)를 축협 조건에 맞게 선택한다.
        /// </summary>
        public UserControl CustomAuctionRunning2_320_64(string nhCode, string is_QQuri, string CowDistinction)
        {
            switch (nhCode)
            { // 테스트 8808990657202
                case "8808990657202":  // 하동 P3 320_64 독자적 화면
                    return new AuctionRunning2_320_64();
                default:
                    return new AuctionRunning2_320_64();
            }
        }
    }
}
