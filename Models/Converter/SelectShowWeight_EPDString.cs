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
    public class SelectShowWeight_EPDString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                if (parameter as string == "ShowWeight")
                {
                    return strValue.Equals("Y") ? Visibility.Visible : Visibility.Collapsed;
                }
                else if (parameter as string == "ShowEPD")
                {
                    return strValue.Equals("N") ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
