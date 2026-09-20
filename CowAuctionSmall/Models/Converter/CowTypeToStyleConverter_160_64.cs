using CowAuctionSmall.Models.Structures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace CowAuctionSmall.Models.Converter
{
    public class CowTypeToStyleConverter_160_64 : IValueConverter
    {
        // 각 'Sex'에 따른 스타일 속성을 미리 정의
        private readonly Dictionary<string, (double FontSize, Brush Foreground, Thickness Margin, ScaleTransform? Transform)> styleSettings =
    new Dictionary<string, (double, Brush, Thickness, ScaleTransform?)>
    {
        { "암", (25.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0064")), new Thickness(15, -3, 0, 0), null) },
        { "수", (25.0, Brushes.DodgerBlue, new Thickness(15, -3, 0, 0), null) },
        { "거세", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D6B700")), new Thickness(7, -3, 0, 0), new ScaleTransform(0.70, 1.0)) },
        { "비육", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FED900")), new Thickness(7, -3, 0, 0), new ScaleTransform(0.70, 1.0)) },
        { "암소", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D60053")), new Thickness(7, -3, 0, 0), new ScaleTransform(0.70, 1.0)) },
        { "숫소", (24.0, Brushes.DodgerBlue, new Thickness(7, -3, 0, 0), new ScaleTransform(0.70, 1.0)) },
        { "미경산", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF69B4")), new Thickness(-10, -3, 0, 0), new ScaleTransform(0.70, 0.8)) },
        { "프리마틴", (24.0, Brushes.DodgerBlue, new Thickness(-10, -3, 0, 0), new ScaleTransform(0.50, 0.8)) },
        { "비거세", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D6B700")), new Thickness(-10, -3, 0, 0), new ScaleTransform(0.70, 0.8)) },
        { "공통", (24.0, Brushes.DodgerBlue, new Thickness(7, -3, 0, 0), new ScaleTransform(0.70, 1.0)) },
        { "새끼", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF69B4")), new Thickness(7, -3, 0, 0), new ScaleTransform(0.70, 1.0)) },
        // 기본 스타일
        { "기본", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF69B4")), new Thickness(10, -3, 0, 0), new ScaleTransform(0.70, 1.0)) }
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
            var settings = styleSettings.ContainsKey(cowinfo.Sex) ? styleSettings[cowinfo.Sex] : styleSettings["기본"];

            var style = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                    new Setter(TextBlock.FontSizeProperty, settings.FontSize),
                    new Setter(TextBlock.ForegroundProperty, settings.Foreground),
                    new Setter(TextBlock.MarginProperty, settings.Margin),
                    new Setter(TextBlock.RenderTransformProperty, settings.Transform)
                }
            };

            // '혈통' 카테고리일 경우 추가 스타일 적용
            return style;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private Style CreateDefaultStyle()
        {
            return new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.FontWeightProperty, FontWeights.Normal),
                    new Setter(TextBlock.FontSizeProperty, 12.0),
                    new Setter(TextBlock.ForegroundProperty, Brushes.Black)
                }
            };
        }
    }
}
