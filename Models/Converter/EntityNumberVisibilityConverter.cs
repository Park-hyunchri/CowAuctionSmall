using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter
{
    public class EntityNumberVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                int intValue = int.Parse(strValue);
                if (parameter as string == "Cow")
                {
                    return intValue != 5 && intValue == 1 || intValue == 2 || intValue == 3 ? Visibility.Visible : Visibility.Collapsed;
                }
                else if (parameter as string == "Goat")
                {
                    return intValue == 5 ? Visibility.Visible : Visibility.Collapsed;
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
