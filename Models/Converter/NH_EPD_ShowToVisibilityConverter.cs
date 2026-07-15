using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter
{
    public class NH_EPD_ShowToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. 문자열로 변환 및 기본적인 Null/Empty 체크
            string val = value as string;
            bool hasValue = !string.IsNullOrEmpty(val);

            // 2. 주석에 언급하신 '-' 문자열인 경우도 값이 없는 것으로 간주
            if (hasValue && val == "-")
            {
                hasValue = false;
            }

            // 3. 'Invert' 파라미터 처리 (반전 로직)
            if (parameter?.ToString() == "Invert")
            {
                hasValue = !hasValue;
            }

            // 4. 결과 반환
            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
