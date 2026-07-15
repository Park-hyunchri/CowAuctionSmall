using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter
{
    public class BidderLeftPositionConverter : IMultiValueConverter
    {
        private const double BaseLeft = 60.0;
        private const double MinLeft = 4.0; // 최소 왼쪽 위치

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double actualWidth = values[1] is double width ? width : 0;

            // 기본 위치에서, 오른쪽으로 넘치는 만큼 왼쪽으로 이동
            double shiftedLeft = BaseLeft - Math.Max(0, actualWidth - 64); // 64는 초기기준 또는 최소 너비

            return Math.Max(shiftedLeft, MinLeft);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
