using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter
{
    //친자확인 결과를 Visibility로 변환하는 컨버터
    // 일치인 경우 Visibility.Visible, 불일치인 경우 Visibility.Collapsed로 변환
    public class PaternityMatchToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == "일치" ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // 필요 없으면 예외 처리
        }
    }
}
