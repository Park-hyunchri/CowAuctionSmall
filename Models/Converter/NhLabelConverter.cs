using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter
{
    public class NhLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dist = value as string;

            // CowDistinction == "3" 이면 "NH:", 그 외는 "NH유전:"
            if (dist == "3")
                return "농협:";
            else
                return "농협육종:";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 역변환 안 쓸 거면 DoNothing 또는 null
            return Binding.DoNothing;
        }
    }
}
