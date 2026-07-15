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
    public class CowCowDist_NumToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? distinction = value as string;
            switch (distinction)
            {
                case "송아지": //송아지
                    return new Style(typeof(TextBlock))
                    {
                        Setters =
                            {
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF00"))),
                                new Setter(TextBlock.MarginProperty, new Thickness(2, 0, 0, 0))

                            }
                    };
                case "비육우": //비육우
                    return new Style(typeof(TextBlock))
                    {
                        Setters =
                            {
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF00"))),
                                new Setter(TextBlock.MarginProperty, new Thickness(2, 0, 0, 0))

                            }
                    };
                case "번식우": //번식우
                    return new Style(typeof(TextBlock))
                    {
                        Setters =
                            {
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF00"))),
                                new Setter(TextBlock.MarginProperty, new Thickness(2, 0, 0, 0))

                            }
                    };
                case "염소":
                    return new Style(typeof(TextBlock))
                    {
                        Setters =
                            {
                                new Setter(TextBlock.FontSizeProperty, 13.0),
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF00"))),
                                new Setter(TextBlock.MarginProperty, new Thickness(2, 0, 0, 0)),
                                new Setter(TextBlock.RenderTransformOriginProperty, new Point(0.05, 0.5))

                            }
                    };
                default:
                    return new Style(typeof(TextBlock))
                    {
                        Setters =
                            {
                                new Setter(TextBlock.FontSizeProperty, 13.0),
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF69B4"))),
                                new Setter(TextBlock.MarginProperty, new Thickness(0, 2, 0, 0)),
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
