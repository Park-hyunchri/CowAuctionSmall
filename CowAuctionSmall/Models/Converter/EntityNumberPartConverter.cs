using System;
using System.Globalization;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter
{
    public class EntityNumberPartConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string entityNumber = value as string ?? string.Empty;
            string compactNumber = entityNumber.Replace(" ", string.Empty);
            string part = parameter as string ?? string.Empty;

            if (part.Equals("First", StringComparison.OrdinalIgnoreCase))
            {
                return compactNumber.Length >= 4 ? compactNumber.Substring(0, 4) : compactNumber;
            }

            if (part.Equals("Last", StringComparison.OrdinalIgnoreCase))
            {
                return compactNumber.Length >= 9 ? compactNumber.Substring(8, 1) : string.Empty;
            }

            return entityNumber;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
