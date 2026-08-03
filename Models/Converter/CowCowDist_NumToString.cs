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
    public class CowCowDist_NumToString : IValueConverter
    {
        private static readonly SolidColorBrush YellowBrush = CreateFrozenBrush("#FFFF00");
        private static readonly SolidColorBrush PinkBrush = CreateFrozenBrush("#FF69B4");

        private static readonly Style CattleStyle;
        private static readonly Style GoatStyle;
        private static readonly Style DefaultStyle;

        static CowCowDist_NumToString()
        {
            CattleStyle = new Style(typeof(TextBlock));
            CattleStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, YellowBrush));
            CattleStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(2, 0, 0, 0)));
            CattleStyle.Seal();

            GoatStyle = new Style(typeof(TextBlock));
            GoatStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 13.0));
            GoatStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, YellowBrush));
            GoatStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(2, 0, 0, 0)));
            GoatStyle.Setters.Add(new Setter(TextBlock.RenderTransformOriginProperty, new Point(0.05, 0.5)));
            GoatStyle.Seal();

            DefaultStyle = new Style(typeof(TextBlock));
            DefaultStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 13.0));
            DefaultStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, PinkBrush));
            DefaultStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(0, 2, 0, 0)));
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
            string? distinction = value as string;
            return distinction switch
            {
                "송아지" or "비육우" or "번식우" => CattleStyle,
                "염소" => GoatStyle,
                _ => DefaultStyle
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}