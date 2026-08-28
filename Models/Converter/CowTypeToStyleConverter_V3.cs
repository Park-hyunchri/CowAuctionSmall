// FILE PATH: Models\Converter\CowTypeToStyleConverter_V3.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CowAuctionSmall.Models.Structures;

namespace CowAuctionSmall.Models.Converter
{
    public class CowTypeToStyleConverter_V3 : IValueConverter
    {
        // 🎨 색상 브러시 (한 번 생성 후 재사용 + Freeze)
        private static SolidColorBrush CreateBrush(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze(); // 성능 최적화
            return brush;
        }
        /*
                { "암", (30.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0064")), new Thickness(90, -3, 0, 0)) },
                { "수", (30.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5cafff")), new Thickness(90, -3, 0, 0)) },
                { "거세", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D6B700")), new Thickness(74, -3, 0, 0)) },
                { "비육", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FED900")), new Thickness(74, -3, 0, 0)) },
                { "암소", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D60053")), new Thickness(74, -3, 0, 0)) },
                { "숫소", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5cafff")), new Thickness(74, -3, 0, 0)) },
                { "미경산", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF69B4")), new Thickness(74, -3, 0, 0)) },
                { "프리마틴", (24.0, new SolidColorBrush((Color) ColorConverter.ConvertFromString("#5cafff")), new Thickness(74, -3, 0, 0)) },
                { "비거세", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D6B700")), new Thickness(74, -3, 0, 0)) },
                { "공통", (24.0, new SolidColorBrush((Color) ColorConverter.ConvertFromString("#5cafff")), new Thickness(74, -3, 0, 0)) },
                { "새끼", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF69B4")), new Thickness(74, -3, 0, 0)) },
                // 기본 스타일
                { "기본", (24.0, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF69B4")), new Thickness(74, -3, 0, 0)) }
         */

        private static readonly SolidColorBrush BrushRed = CreateBrush("#FF0064");
        private static readonly SolidColorBrush BrushBlue = CreateBrush("#5cafff");
        private static readonly SolidColorBrush BrushGold = CreateBrush("#D6B700");
        private static readonly SolidColorBrush BrushYellow = CreateBrush("#FED900");
        private static readonly SolidColorBrush BrushPink = CreateBrush("#FF69B4");
        private static readonly SolidColorBrush BrushDarkRed = CreateBrush("#D60053");
        private static readonly SolidColorBrush BrushBlack = Brushes.Black;

        private static readonly Thickness DefaultMargin = new Thickness(74, 4, 0, 0);
        private static readonly Thickness WideMargin = new Thickness(90, 0, 0, 0);

        private static readonly Dictionary<string, Style> CachedStyles = new();
        private static readonly Style DefaultStyle;

        static CowTypeToStyleConverter_V3()
        {
            var settings = new Dictionary<string, (double FontSize, Brush Foreground, Thickness Margin)>
            {
                { "암", (28.0, BrushRed, WideMargin) },
                { "수", (28.0, BrushBlue, WideMargin) },
                { "프", (28.0, BrushBlue, WideMargin) },
                { "거세", (23.0, BrushGold, DefaultMargin) },
                { "비육", (23.0, BrushYellow, DefaultMargin) },
                { "암소", (23.0, BrushDarkRed, DefaultMargin) },
                { "숫소", (23.0, BrushBlue, DefaultMargin) },
                { "미경산", (23.0, BrushPink, DefaultMargin) },
                { "프리마틴", (23.0, BrushBlue, DefaultMargin) },
                { "비거세", (23.0, BrushGold, DefaultMargin) },
                { "공통", (23.0, BrushBlue, DefaultMargin) },
                { "새끼", (23.0, BrushPink, DefaultMargin) },
                { "기본", (23.0, BrushBlack, DefaultMargin) }
            };

            // 💡 <ChangeSexName>Y</ChangeSexName> 옵션 적용 시 들어오는 축약어 매핑
            settings["거"] = settings["거세"];
            settings["비거"] = settings["비거세"];
            settings["프리"] = settings["프리마틴"];
            settings["새"] = settings["새끼"];

            // 💡 모든 스타일을 정적 생성 시점에 미리 생성하고 .Seal() 호출하여 다중 패널 재사용 예외 방지
            foreach (var kvp in settings)
            {
                var textStyle = new Style(typeof(TextBlock));
                textStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
                textStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, kvp.Value.FontSize));
                textStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, kvp.Value.Foreground));
                textStyle.Setters.Add(new Setter(TextBlock.MarginProperty, kvp.Value.Margin));
                textStyle.Seal(); // 💡 핵심: 다수 패널 재사용을 위한 봉인 처리

                CachedStyles[kvp.Key] = textStyle;
            }

            DefaultStyle = CachedStyles["기본"];
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not gValues cowInfo)
                return DefaultStyle;

            string sex = string.IsNullOrWhiteSpace(cowInfo.Sex) ? "기본" : cowInfo.Sex;

            if (CachedStyles.TryGetValue(sex, out var cachedStyle))
                return cachedStyle;

            return DefaultStyle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
