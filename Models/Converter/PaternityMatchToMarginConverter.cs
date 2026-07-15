using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter
{
    public class PaternityMatchToMarginConverter : IValueConverter
    {
        private static readonly Thickness MatchedMargin = new Thickness(40, 109, 2, 1);
        private static readonly Thickness UnmatchedMargin = new Thickness(2, 109, 2, 1);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == "일치" ? MatchedMargin : UnmatchedMargin;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
