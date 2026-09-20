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
            if (value is string strValue && int.TryParse(strValue, out int intValue))
            {
                string? param = parameter as string;
                if (param == "Cow")
                {
                    return (intValue != 5 && (intValue == 1 || intValue == 2 || intValue == 3))
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
                else if (param == "Goat")
                {
                    return (intValue == 5) ? Visibility.Visible : Visibility.Collapsed;
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
