using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace CowAuctionSmall.Models.Converter
{
    public class CowTypeToStyleConverter_OutLine_V3 : IMultiValueConverter
    {
        private static readonly Brush _borderBrush = CreateFrozenBrush("#B0B0B0");

        private static readonly Brush _colorFemale = CreateFrozenBrush("#FF0050");
        private static readonly Brush _colorMale = CreateFrozenBrush("#5cafff");
        private static readonly Brush _colorCastrated = CreateFrozenBrush("#D6B700"); //거세
        private static readonly Brush _colorFattening = CreateFrozenBrush("#FED900"); //비육
        private static readonly Brush _colorFemaleCow = CreateFrozenBrush("#D60053"); //암소
        private static readonly Brush _colorUnbred = CreateFrozenBrush("#FF69B4"); // 미경산
        private static readonly Brush _colorNonCastrated = CreateFrozenBrush("#D6B700"); // 비거세
        private static readonly Brush _colorBaby = CreateFrozenBrush("#FF69B4"); // 새끼
        private static readonly Brush _colorDefault = CreateFrozenBrush("#FF69B4"); // 기본 색상

        private static readonly Thickness DefaultMargin = new Thickness(74, -3, 0, 0);
        private static readonly FontFamily UsedFont = new FontFamily("굴림");



        // 각 'Sex'에 따른 스타일 속성을 미리 정의
        private static readonly Dictionary<string, (double FontSize, Brush Foreground, Thickness Margin)> styleSettings =
            new Dictionary<string, (double, Brush, Thickness)>
            {
                { "암", (24.0, _colorFemale, new Thickness(90, -3, 0, 0)) },
                { "수", (24.0, _colorMale, new Thickness(90, -3, 0, 0)) },
                { "거세", (24.0, _colorCastrated, DefaultMargin) },
                { "비육", (24.0, _colorFattening, DefaultMargin) },
                { "암소", (24.0, _colorFemaleCow, DefaultMargin) },
                { "숫소", (24.0, _colorMale, DefaultMargin) },
                { "미경산", (24.0, _colorUnbred, DefaultMargin) },
                { "프리마틴", (24.0, _colorMale, DefaultMargin) },
                { "비거세", (24.0, _colorNonCastrated, DefaultMargin) },
                { "공통", (24.0, _colorMale, DefaultMargin) },
                { "새끼", (24.0, _colorBaby, DefaultMargin) },
                // 기본 스타일
                { "기본", (24.0, _colorDefault, DefaultMargin) },
                { "없음", (24.0, _colorDefault, DefaultMargin) },
                {"일치", (14, _colorDefault, DefaultMargin) } // "일치" 카테고리 추가
            };



        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not string sex || values[1] is not string category)
                return DependencyProperty.UnsetValue;

            var setting = styleSettings.TryGetValue(sex, out var s) ? s : styleSettings["기본"];

            if (category == "혈통")
            {
                // 위치 정보는 styleSettings에서 꺼냄
                var margin = setting.Margin;

                // 3단계 어둡게
                var brush3 = _borderBrush;

                //sex값이 2글자 이상이면 2글자로 변경
                if (sex.Length > 2)
                {
                    sex = sex.Substring(0, 2);
                }

                int plusWidth = sex.Length == 1 ? 0 : (int)setting.FontSize; // 텍스트 길이에 따라 너비 조정

                var border = new Border
                {
                    Background = Brushes.Transparent,          // 배경 제거
                    BorderBrush = setting.Foreground,          // 테두리 색상 = 텍스트 강조 색상
                    BorderThickness = new Thickness(2),        // 테두리 두께 설정
                    Width = setting.FontSize + plusWidth,             // 텍스트보다 약간 넓게
                    Height = setting.FontSize,
                    CornerRadius = new CornerRadius(4),
                    Margin = margin
                };


                var text = new OutlinedTextBlock
                {
                    Text = sex,
                    Fill = setting.Foreground, //내부 텍스트 색상
                    Stroke = setting.Foreground, // 외곽선 색상
                    StrokeThickness = 1,
                    FontSize = setting.FontSize,
                    FontWeight = FontWeights.ExtraBold,
                    FontFamily = UsedFont,
                    Margin = margin
                };

                // 여러 개의 UIElement를 한 번에 반환해야 하므로 StackPanel이나 Grid로 감싸기
                var container = new Grid();
                container.Children.Add(border);
                container.Children.Add(text);

                return container;
            }
            else if (category.Equals("일치"))
            {
                setting = styleSettings["일치"]; // "일치" 카테고리의 스타일 설정
                // 위치 정보는 styleSettings에서 꺼냄
                //var margin = setting.Margin;

                // 3단계 어둡게
                var brush3 = _borderBrush;

                //sex값이 2글자 이상이면 2글자로 변경
                if (sex.Length > 2)
                {
                    sex = sex.Substring(0, 2);
                }

                int plusWidth = sex.Length == 1 ? 0 : (int)setting.FontSize; // 텍스트 길이에 따라 너비 조정

                var border = new Border
                {
                    Background = Brushes.Transparent,          // 배경 제거
                    BorderBrush = setting.Foreground,          // 테두리 색상 = 텍스트 강조 색상
                    BorderThickness = new Thickness(1),        // 테두리 두께 설정
                    Width = setting.FontSize + plusWidth,             // 텍스트보다 약간 넓게
                    Height = setting.FontSize,
                    CornerRadius = new CornerRadius(3)
                    //Margin = margin
                };


                var text = new OutlinedTextBlock
                {
                    Text = sex,
                    Fill = setting.Foreground, //내부 텍스트 색상
                    Stroke = setting.Foreground, // 외곽선 색상
                    StrokeThickness = 0.2,
                    FontSize = setting.FontSize,
                    FontWeight = FontWeights.Regular,
                    FontFamily = UsedFont
                };

                // 여러 개의 UIElement를 한 번에 반환해야 하므로 StackPanel이나 Grid로 감싸기
                var container = new Grid();
                container.Children.Add(border);
                container.Children.Add(text);

                return container;
            }
            else
            {
                if (sex.Length > 2 && (sex.Contains("정보없음") || sex.Contains("일치"))) // "정보없음" 또는 "일치"가 포함된 경우 
                {
                    //모 불일치, 부 불일치, 완전 불일치, 모부 불일치
                    return new TextBlock
                    {
                        Text = string.Empty
                    };
                }
                else 
                {
                    return new TextBlock
                    {
                        Text = sex,
                        Foreground = setting.Foreground, // ✅ 여기도
                        FontSize = setting.FontSize,
                        FontWeight = FontWeights.ExtraBold,
                        FontFamily = UsedFont,
                        Margin = setting.Margin
                    };
                }
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();


        private static Brush CreateFrozenBrush(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze(); // 성능 최적화
            return brush;
        }

    }
}
