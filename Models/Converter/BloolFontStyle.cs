using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
namespace CowAuctionSmall.Models.Converter
{
    public class BloolFontStyle : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? bloodType = value as string;
            switch (bloodType)
            {
                case "혈통": //혈통
                    return new Style(typeof(TextBlock))
                    {
                        Setters =
                            {
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f61c2d"))),
                                new Setter(TextBlock.MarginProperty,new Thickness(40,0,0,0))
                            }
                    };
                case "미등록": //
                    return new Style(typeof(TextBlock))
                    {
                        Setters =
                            {
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffffff"))),
                                //Margin 추가
                                new Setter(TextBlock.MarginProperty,new Thickness(35,0,0,0)),
                                new Setter(TextBlock.FontSizeProperty,11.0)
                            }
                    };
                default:
                    return new Style(typeof(TextBlock))
                    {
                        Setters =
                            {
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff7f00"))),
                                new Setter(TextBlock.MarginProperty,new Thickness(40,0,0,0))
                            }
                    };
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
