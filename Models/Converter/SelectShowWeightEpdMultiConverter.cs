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
    public class SelectShowWeightEpdMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return Visibility.Collapsed;

            string? selectValue = values[0]?.ToString(); // CowInfo.SelectShowWeight_EPD (예: "Y"/"N")
            int cowDistinction = 0;

            if (values[1] is int i) cowDistinction = i;
            else if (int.TryParse(values[1]?.ToString(), out var parsed)) cowDistinction = parsed;

            var param = parameter as string;

            if (param == "ShowWeight")
            {
                // ShowWeight 조건: Select == "Y"  ||  CowDistinction == 5
                return (selectValue == "Y" || cowDistinction == 5) ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (param == "ShowEPD")
            {
                // ShowEPD 조건: Select == "N"  &&  CowDistinction != 5
                return (selectValue == "N" && cowDistinction != 5) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
