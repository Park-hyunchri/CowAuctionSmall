using CowAuctionSmall.Views.Size128_128.Running.CustomAuctionRunning1;
using CowAuctionSmall.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using CowAuctionSmall.Views.Size128_128.Running.CustomAuctionRunning2;
using CowAuctionSmall.Views.Size128_128;
using CowAuctionSmall.Views.Size128_128.CustomAUctionUnSold;
using CowAuctionSmall.Views.Size128_128.CustomAuctionSold;

namespace CowAuctionSmall.Models
{
    public class SetCustomDisplay
    {
        /// <summary>
        /// 개체정보 페이지 어미 산차 중량 최저가 등.. 표시
        /// </summary>
        /// <param name="nhCode"></param>
        /// <returns></returns>
        public UserControl CustomAuctionRunning1_128(string nhCode)
        {
            switch (nhCode)
            {

                case "8808990656953": // 정읍 8808990656953 중량란 대신에 유전능력 알파벳으로 표시
                    return new Jeongeup();
                case "8808990656960": // 순창 8808990656960 중량란 대신에 유전능력 알파벳으로 표시
                    return new Jeongeup();
                case "8808990657639": // 상주 지역명 대신에 "축주"라고 표시
                    return new Sangju();
                default:
                    return new AuctionRunning1();
            }
        }

        /// <summary>
        /// 유전능력 페이지
        /// </summary>
        /// <param name="nhCode"></param>
        /// <returns></returns>
        public UserControl CustomAuctionRunning2_128(string nhCode)
        {
            switch (nhCode)
            {

                case "8808990656885": // 횡성 유전능력 목차를 다른 말로 표시
                    return new Hoengseong();
                default:
                    return new AuctionRunning2();
            }
        }

        /// <summary>
        /// 낙찰 페이지
        /// </summary>
        /// <param name="nhCode"></param>
        /// <returns></returns>
        public UserControl CustomAuctionSold_128(string nhCode)
        {
            switch (nhCode)
            {

                case "8808990657639": // 상주 지역명 대신에 "축주"라고 표시
                    return new SangjuSold();
                case "8808990656885": // 횡성 원하는 표출화면
                    return new HoengseongSold();
                default:
                    return new AuctionSold();
            }
        }

        /// <summary>
        /// 유찰 페이지
        /// </summary>
        /// <param name="nhCode"></param>
        /// <returns></returns>
        public UserControl CustomAuctionUnSold_128(string nhCode)
        {
            switch (nhCode)
            {

                case "8808990657639": // 상주 지역명 대신에 "축주"라고 표시
                    return new SanjuUnSold();
                default:
                    return new AuctionUnSold();
            }
        }
    }
}
