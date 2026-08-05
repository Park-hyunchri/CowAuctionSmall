using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        // 📐 마진 재사용
        private static readonly Thickness DefaultMargin = new Thickness(74, 4, 0, 0);
        private static readonly Thickness WideMargin = new Thickness(90, 0, 0, 0);

        // 💾 캐시된 스타일
        private static readonly Dictionary<string, Style> CachedStyles = new();

        // 📋 스타일 설정 목록
        private static readonly Dictionary<string, (double FontSize, Brush Foreground, Thickness Margin)> StyleSettings;

        // 🚀 static 생성자에서 딕셔너리 및 축약 키(Alias) 미리 초기화
        static CowTypeToStyleConverter_V3()
        {
            StyleSettings = new()
            {
                { "암", (28.0, BrushRed, WideMargin) },
                { "수", (28.0, BrushBlue, WideMargin) },
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

            // 💡 <ChangeSexName>Y</ChangeSexName> 옵션 적용 시 들어오는 축약 키(Alias) 사전 등록
            StyleSettings["거"] = StyleSettings["거세"];
            StyleSettings["비거"] = StyleSettings["비거세"];
            StyleSettings["프리"] = StyleSettings["프리마틴"];
            StyleSettings["새"] = StyleSettings["새끼"];
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not gValues cowInfo)
                return CreateDefaultStyle();

            string sex = string.IsNullOrWhiteSpace(cowInfo.Sex) ? "기본" : cowInfo.Sex;

            // 1. 이미 캐시된 Style이 있는지 확인
            if (CachedStyles.TryGetValue(sex, out var cachedStyle))
                return cachedStyle;

            // 2. 딕셔너리에서 조회 (축약 키도 사전 등록되어 있으므로 예외 없이 즉시 조회됨)
            var style = StyleSettings.TryGetValue(sex, out var setting) ? setting : StyleSettings["기본"];

            var textStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                    new Setter(TextBlock.FontSizeProperty, style.FontSize),
                    new Setter(TextBlock.ForegroundProperty, style.Foreground),
                    new Setter(TextBlock.MarginProperty, style.Margin)
                }
            };

            CachedStyles[sex] = textStyle;
            return textStyle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private static Style CreateDefaultStyle() => new(typeof(TextBlock))
        {
            Setters =
            {
                new Setter(TextBlock.FontWeightProperty, FontWeights.Normal),
                new Setter(TextBlock.FontSizeProperty, 12.0),
                new Setter(TextBlock.ForegroundProperty, BrushBlack)
            }
        };
    }
}