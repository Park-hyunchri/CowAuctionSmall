// 중량이 0이거나 없을 때 "-"로 표시하고, 값이 있으면 "kg"를 붙여 표시하는 변환기
using System;
using System.Globalization;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter
{
    public class ChuncheonWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "-";

            string weight = value.ToString().Trim();

            // 중량 값이 "0", 비어있음, 혹은 기존 "-"인 경우 "-" 반환
            if (string.IsNullOrEmpty(weight) || weight.Equals("0") || weight.Equals("-"))
            {
                return "-";
            }
            else
            {
                // 정상적인 중량인 경우 뒤에 "kg"을 붙여서 표시
                return weight + "kg";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 단방향 바인딩이므로 예외 처리로 둡니다.
            throw new NotImplementedException();
        }
    }
}