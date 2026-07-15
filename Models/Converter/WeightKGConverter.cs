using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter
{
    public class WeightKGConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string weight = value as string ?? "-";
            if (weight.Equals("0") || weight.Equals("-"))
            {
                return "-";
            }
            else
            {
                return weight + "kg";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
