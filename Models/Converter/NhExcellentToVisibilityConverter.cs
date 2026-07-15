using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter
{
    public class NhExcellentToVisibilityConverter : IMultiValueConverter
    {
        // values[0], values[1], ... 에 각 Binding 값이 들어옵니다.
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return Visibility.Collapsed;

            // 예시: 첫 번째, 두 번째 값을 문자열로 받아서 처리
            string s1 = values.Length > 0 ? values[0]?.ToString()?.Trim() ?? string.Empty : string.Empty;
            string s2 = values.Length > 1 ? values[1]?.ToString()?.Trim() ?? string.Empty : string.Empty;

            // 원하는 로직으로 변경하면 됩니다.
            // 예: 둘 중 하나라도 "A" 또는 "Y" 이면 Visible
            bool v1 = (s1 == "A" || s1 == "Y");
            bool v2 = (s2 == "A" || s2 == "Y");

            if (v1 || v2)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Array.Empty<object>(); // 사용 안 할 거면 빈 배열 반환
        }
    }
}
