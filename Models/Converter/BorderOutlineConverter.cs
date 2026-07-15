using CowAuctionSmall.Models.Structures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CowAuctionSmall.Models.Converter
{
    public class BorderOutlineConverter : IValueConverter
    {
        //시간이 없어서 복붙한거

        // 각 'Sex'에 따른 스타일 속성을 미리 정의
        private readonly Dictionary<string, (double Width, double Height, Thickness Margin, CornerRadius CornerRadius)> borderSettings =
            new Dictionary<string, (double, double, Thickness, CornerRadius)>
            {
                // 성별별 스타일 정의
                { "암", (32, 32, new Thickness(90, -2, 0, 0), new CornerRadius(8)) },
                { "수", (32, 32, new Thickness(90, -2, 0, 0), new CornerRadius(8)) },
                { "거세", (53, 29, new Thickness(72, -2, 0, 0), new CornerRadius(6)) },
                { "비육", (53, 29, new Thickness(72, -2, 0, 0), new CornerRadius(6)) },
                { "암소", (53, 29, new Thickness(72, -2, 0, 0), new CornerRadius(6)) },
                { "숫소", (53, 29, new Thickness(72, -2, 0, 0), new CornerRadius(6)) },
                { "미경산", (53, 29, new Thickness(72, -2, 0, 0), new CornerRadius(6)) },
                { "프리마틴", (53, 29, new Thickness(72, -2, 0, 0), new CornerRadius(6)) },
                { "비거세", (53, 29, new Thickness(72, -2, 0, 0), new CornerRadius(6)) },
                { "공통", (53, 29, new Thickness(72, -2, 0, 0), new CornerRadius(6)) },
                { "새끼", (53, 29, new Thickness(72, -2, 0, 0), new CornerRadius(6)) },
                // 기본 스타일
                { "기본", (53, 29, new Thickness(72, -2, 0, 0), new CornerRadius(6)) }
            };

        private readonly Dictionary<string, (double Width, double Height, Thickness Margin, CornerRadius CornerRadius)> borderSettingsUnSold = // 임시로 이따구로 했지만 수정 예정
            new Dictionary<string, (double, double, Thickness, CornerRadius)>
            {
                // 성별별 스타일 정의
                { "암", (32, 32, new Thickness(77, 2, 0, 0), new CornerRadius(8)) },
                { "수", (32, 32, new Thickness(77, 2, 0, 0), new CornerRadius(8)) },
                { "거세", (53, 29, new Thickness(75, 0, 0, 0), new CornerRadius(6)) },
                { "비육", (53, 29, new Thickness(75, 0, 0, 0), new CornerRadius(6)) },
                { "암소", (53, 29, new Thickness(75, 0, 0, 0), new CornerRadius(6)) },
                { "숫소", (53, 29, new Thickness(75, 0, 0, 0), new CornerRadius(6)) },
                { "미경산", (53, 29, new Thickness(75, 0, 0, 0), new CornerRadius(6)) },
                { "프리마틴", (53, 29, new Thickness(75, 0, 0, 0), new CornerRadius(6)) },
                { "비거세", (53, 29, new Thickness(75, 0, 0, 0), new CornerRadius(6)) },
                { "공통", (53, 29, new Thickness(75, 0, 0, 0), new CornerRadius(6)) },
                { "새끼", (53, 29, new Thickness(75, 0, 0, 0), new CornerRadius(6)) },
                // 기본 스타일
                { "기본", (53, 29, new Thickness(72, 0, 0, 0), new CornerRadius(6)) }
            };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            gValues? cowinfo = value as gValues;

            if (cowinfo == null)
            {
                // 기본 스타일 반환
                return CreateDefaultStyle();
            }

            // 'Sex'에 해당하는 스타일 정보 가져오기
            var settings = borderSettings.ContainsKey(cowinfo.Sex) ? borderSettings[cowinfo.Sex] : borderSettings["기본"];

            if (cowinfo.AuctionResultStatus.Equals("23"))
            {
                settings = borderSettingsUnSold.ContainsKey(cowinfo.Sex) ? borderSettingsUnSold[cowinfo.Sex] : borderSettingsUnSold["기본"];
            }

            var style = new Style(typeof(Border))
            {
                Setters =
                {
                    new Setter(Border.BorderBrushProperty, Brushes.Red),
                    new Setter(Border.BorderThicknessProperty, new Thickness(1)),

                    new Setter(Border.WidthProperty, settings.Width),
                    new Setter(Border.HeightProperty, settings.Height),
                    new Setter(Border.MarginProperty, settings.Margin),
                }
            };

            // '혈통' 카테고리일 경우 추가 스타일 적용
            return style;
        }



        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private object CreateDefaultStyle()
        {
            return new Style(typeof(Border))
            {
                Setters =
                    {
                        new Setter(Border.BorderBrushProperty, Brushes.Transparent),
                        new Setter(Border.BorderThicknessProperty, new Thickness(2)),
                        new Setter(Border.CornerRadiusProperty, new CornerRadius(4)), // 둥근 모서리 추가
                        new Setter(Border.PaddingProperty, new Thickness(4)) // 내부 여백 추가
                    }
            };
        }

    }
}
