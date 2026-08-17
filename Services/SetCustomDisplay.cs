// 화면 제어 컨트롤러
using CowAuctionSmall.Views;
using CowAuctionSmall.Views.SIze_160_64.Running;
using CowAuctionSmall.Views.Size_320_64.Running;
using CowAuctionSmall.Views.Size128_128;
using CowAuctionSmall.Views.Size128_128.CustomAuctionSold;
using CowAuctionSmall.Views.Size128_128.CustomAUctionUnSold;
using CowAuctionSmall.Views.Size128_128.Running;
using CowAuctionSmall.Views.Size128_128.Running.CustomAuctionRunning1;
using CowAuctionSmall.Views.Size128_128.Running.CustomAuctionRunning2;
using System;
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
        public UserControl CustomAuctionRunning1_128(string nhCode, string is_QQuri, string CowDistinction, string is_Ｎh_Excellent, string is_Mother_Ｎh_Excellent)
        {
            if (CowDistinction == "5" || CowDistinction == "6")
            {
                return new Standard_Goat_Run(); // 염소 또는 말
            }

            // 조합별(nhCode) 예외 처리
            if (nhCode == "8808990656953") return new Standard_non_X_Run1_1(); // 정읍
            if (nhCode == "8808990656427") return new Standard_non_QQuri_Run1_1(); // 문경
            if (nhCode == "8808990656106") return new HaenamJindo(); // 해남진도
            if (nhCode == "8808990656915") return new MokpoMuanSinan(); // 목무신
            if (nhCode == "8808990643625") return new YangpyeongRun(); // 양평
            if (nhCode == "8808990656557") return new YecheonRun(); // 예천
            if (nhCode == "8808990656885") return new HoengseongRun(); // 횡성
            if (nhCode == "8808990661315") return new Hwasun(); // 화순

            // if (nhCode == "8808990643625" || nhCode == "8808990657202") return new YangpyeongRun(); // 양평, 무진장

            // Null 방지용 Safe 값 추출
            string qquri = is_QQuri?.ToUpper() ?? "";
            int motherExLen = is_Mother_Ｎh_Excellent?.Length ?? 0;

            if (qquri == "X")
            {
                return new Standard_non_X_Run1();
            }
            if (qquri == "Y" || motherExLen > 2)
            {
                return new StandardQQuri_Run1();
            }

            return new Standard_non_QQuri_Run1();
        }

        /// <summary>
        /// 유전능력 페이지
        /// </summary>
        /// <param name="nhCode"></param>
        /// <returns></returns>
        public UserControl CustomAuctionRunning2_128(string nhCode, string is_QQuri, string CowDistinction, string is_Ｎh_Excellent, string is_Mother_Ｎh_Excellent)
        {
            // 1. 염소(5) 또는 말(6) 예외 처리
            if (CowDistinction == "5" || CowDistinction == "6")
            {
                return new Standard_Goat_Run();
            }

            // 2. 춘천 축협 전용 코드
            if (nhCode == "8808990656229") return new AuctionRunning2();  // 춘천
            if (nhCode == "8808990656960" || nhCode == "8808990656953" || nhCode == "8808990643625") return new Eumseong2(); // 순창, 정읍, 양평
            if (nhCode == "8808990656717" || nhCode == "8808990817675") return new TestRunning2(); // 곡성, 장성
            if (nhCode == "8808990656885" || nhCode == "8808990657202") return new HoengseongRun2(); // 횡성

            // 3. 농협 우수 조건 만족 여부 판단
            // (is_QQuri가 "Y"이고, 둘 중 하나라도 "N"이 아닌 경우)
            bool isNhExcellent = is_QQuri == "Y" && (is_Ｎh_Excellent != "N" || is_Mother_Ｎh_Excellent != "N");

            // 4. 번식우 구분(CowDistinction == "3") 여부에 따른 화면 결정
            bool isBreedingCow = CowDistinction == "3";

            if (isNhExcellent)
            {
                // 농협 우수 유전능력 화면 (번식우 여부에 따라 분기)
                return isBreedingCow ? new AuctionRunning4() : new AuctionRunning3();
            }

            // 그 외 모든 기본 경우 (X, 기본값 등)
            return isBreedingCow ? new AuctionRunning2_3() : new AuctionRunning2();
        }

        /// <summary>
        /// 낙찰 페이지
        /// </summary>
        /// <param name="nhCode"></param>
        /// <returns></returns>
        public UserControl CustomAuctionSold_128(string nhCode, string bidderCode, string is_QQuri, string CowDistinction, string nh_ability_1_num)
        {
            // 염소/말 구분
            if (int.TryParse(CowDistinction, out int result) && result > 4)
            {
                return new GoatSold();
            }

            // 조합별 전용 낙찰 화면 분기
            return nhCode switch
            {
            "8808990656687" => new Standard_non_X_Sold(), // 영천
            "8808990656526" or "8808990684321" => new JecheonDanyangSold(), // 제천단양, 보령
            "8808990837314" or "8808990227207" or "8808990683973" or "8808990659787" or "8808990656427" or "8808990844220" or "8808990227283" or "8808990656458" or "8808990795874" or "8808990657639" or "8808990659268" or "8808990657615" or "8808990679549" or "8808990671086" => new AnseongSold(),
            // 안성, 남원, 음성, 파주연천, 문경, 이천, 익산군산, 고성, 평택, 상주, 논산계룡, 구미칠곡, 포항, 옥천
                "8808990660783" => new QQuriSold_Weight(), // 임실
    
            "8808990643625" => new YangpyeongSold(), // 양평
            "8808990656557" => new YecheonSold(), // 예천
            "8808990656106" => new HaenamJindoSold(), // 해남진도
            "8808990656915" or "8808990656229" or "8808990659701" or "8808990844220" or "8808998656496" or "8808990657196" => new MokpoMuanSinanSold(), // 목무신, 춘천, 거창, 홍천, 수원, 예산
            "8808990656717" or "8808990817675" => new QQuriSold_v3(), // 곡성, 장성
            "8808990656885" => new HoengseongSold(), // 횡성
            "8808990661315" => new HwasunSold(), // 화순

                // "8808990657202" => new AnseongSold(), // 안성, 무진장

                // 뿌리농가 미적용("X") 구분이 필요할 경우 아래 주석 해제 후 사용
                _ => string.Equals(is_QQuri, "X", StringComparison.OrdinalIgnoreCase)
                ? new Standard_non_X_Sold()
                : new QQuriSold()
            };
        }

        /// <summary>
        /// 유찰 페이지
        /// </summary>
        /// <param name="nhCode"></param>
        /// <returns></returns>
        public UserControl CustomAuctionUnSold_128(string nhCode, string is_QQuri, string CowDistinction, string nh_ability_1_num)
        {
            // 1. 염소 또는 말 구분
            if (int.TryParse(CowDistinction, out int result) && result > 4)
            {
                return new GoatUnSold();
            }

            // 2. 특정 지역 조합(nhCode) 분기
            if (nhCode == "8808990656953" || nhCode == "8808990656915") return new NamwonUnSold();          // 정읍, 목무신
            if (nhCode == "8808990837314" || nhCode == "8808990660783") return new QQuriUnSold();         // 안성, 임실
            if (nhCode == "8808990656229" || nhCode == "8808990656106" || nhCode == "8808990656717") return new OutLineUnSold(); // 춘천, 해남진도, 곡성
            if (nhCode == "8808990656885" || nhCode == "8808990657202") return new HoengseongUnSold();      // 횡성
            if (nhCode == "8808990656557") return new YecheonUnSold(); // 예천
            if (nhCode == "8808990684321") return new Standard_non_X_UnSold(); // 보령
            if (nhCode == "8808990661315") return new HwasunUnSold(); // 화순
            if (nhCode == "8808990844220") return new OutLineUnSold_2(); // 홍천

            if (nhCode == "8808990643625") return new YangpyeongUnSold();      // 양평
            
            

            // if (nhCode == "8808990657202") return new AnseongUnSold(); // 무진장

            // 3. 뿌리농가 미적용("X")이면서 유전능력 번호도 없을 때만 Standard_non_X_UnSold
            bool isX = string.Equals(is_QQuri, "X", StringComparison.OrdinalIgnoreCase);
            int abilityLen = nh_ability_1_num?.Length ?? 0; // null 방지

            if (isX && abilityLen <= 2)
            {
                return new Standard_non_X_UnSold();
            }

            // 그 외 모든 경우(Y이거나, X여도 능력치 정보가 길게 있으면) QQuriUnSold 반환
            return new QQuriUnSold();
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
