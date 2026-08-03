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
    /// <summary>
    /// 혈통 정보에 따른 텍스트 스타일 및 색상 변환 컨버터
    /// </summary>
    public class BloodFontStyle : IValueConverter
    {
        // 💡 static 필드로 브러시 및 스타일을 1회만 생성 후 Freeze()
        private static readonly SolidColorBrush RedBrush = CreateFrozenBrush("#f61c2d");
        private static readonly SolidColorBrush WhiteBrush = CreateFrozenBrush("#ffffff");
        private static readonly SolidColorBrush OrangeBrush = CreateFrozenBrush("#ff7f00");

        private static readonly Style BloodStyle;
        private static readonly Style UnregisteredStyle;
        private static readonly Style DefaultStyle;

        static BloodFontStyle()
        {
            BloodStyle = new Style(typeof(TextBlock));
            BloodStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, RedBrush));
            BloodStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(40, 0, 0, 0)));
            BloodStyle.Seal();

            UnregisteredStyle = new Style(typeof(TextBlock));
            UnregisteredStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, WhiteBrush));
            UnregisteredStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(35, 0, 0, 0)));
            UnregisteredStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 11.0));
            UnregisteredStyle.Seal();

            DefaultStyle = new Style(typeof(TextBlock));
            DefaultStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, OrangeBrush));
            DefaultStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(40, 0, 0, 0)));
            DefaultStyle.Seal();
        }

        private static SolidColorBrush CreateFrozenBrush(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? bloodType = value as string;
            return bloodType switch
            {
                "혈통" => BloodStyle,
                "미등록" => UnregisteredStyle,
                _ => DefaultStyle
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}