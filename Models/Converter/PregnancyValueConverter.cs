using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter
{
    //임신 개월수가 0보다 클때 보여주기
    public class PregnancyValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value를 string으로 가정하고 필요에 따라 형변환하여 사용합니다.
            string? pregnantValueString = value as string;
            if (pregnantValueString != null && int.TryParse(pregnantValueString, out int pregnantValue))
            {
                return pregnantValue > 0; // 0보다 크면 true를 반환하여 캔버스를 보이게 합니다.
            }
            return false; // 예외 상황 처리
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
