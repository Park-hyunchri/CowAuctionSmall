using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter
{
    public class NumberFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value가 문자열이라면, 이를 숫자로 변환하고 천 단위 구분 기호를 추가
            if (value is string strValue && double.TryParse(strValue, out double number))
            {
                // 숫자를 "1,000"과 같은 형식으로 변환
                return number.ToString("#,##0");
            }

            return value; // 변환할 수 없으면 원래 값 반환
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
