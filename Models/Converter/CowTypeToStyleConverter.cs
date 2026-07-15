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
using CowAuctionSmall.Models.Structures;

namespace CowAuctionSmall.Models.Converter
{
    public class CowTypeToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            string? cowType = value as string;

            if (cowType != null)
            {
                switch (cowType)
                {
                    case "암":
                        return new Style(typeof(TextBlock))
                        {
                            Setters =
                            {
                                new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                                new Setter(TextBlock.FontSizeProperty, 30.0),
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0064"))),
                                new Setter(TextBlock.MarginProperty, new Thickness(90, 0, 0, 0))

                            }
                        };
                    case "수":
                        return new Style(typeof(TextBlock))
                        {
                            Setters =
                            {
                                new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                                new Setter(TextBlock.FontSizeProperty, 30.0),
                                new Setter(TextBlock.ForegroundProperty, Brushes.DodgerBlue),
                                new Setter(TextBlock.MarginProperty, new Thickness(90, 0, 0, 0))

                            }
                        };
                    case "거세":
                        return new Style(typeof(TextBlock))
                        {
                            Setters =
                            {
                                new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                                new Setter(TextBlock.FontSizeProperty, 24.0),
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D6B700"))),
                                new Setter(TextBlock.MarginProperty, new Thickness(74, 0, 0, 0))
                            }
                        };
                    case "비육":
                        return new Style(typeof(TextBlock))
                        {
                            Setters =
                            {
                                new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                                new Setter(TextBlock.FontSizeProperty, 24.0),
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FED900"))),//#D60053
                                new Setter(TextBlock.MarginProperty, new Thickness(74, 0, 0, 0))
                            }
                        };
                    case "암소":
                        return new Style(typeof(TextBlock))
                        {
                            Setters =
                            {
                                new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                                new Setter(TextBlock.FontSizeProperty, 24.0),
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D60053"))),
                                new Setter(TextBlock.MarginProperty, new Thickness(74, 0, 0, 0))
                            }
                        };
                    case "숫소":
                        return new Style(typeof(TextBlock))
                        {
                            Setters =
                            {
                                new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                                new Setter(TextBlock.FontSizeProperty, 24.0),
                                new Setter(TextBlock.ForegroundProperty, Brushes.DodgerBlue),
                                new Setter(TextBlock.MarginProperty, new Thickness(74, 0, 0, 0))
                            }
                        };
                    case "미경산":
                        return new Style(typeof(TextBlock))
                        {
                            Setters =
                            {
                                new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                                new Setter(TextBlock.FontSizeProperty, 24.0),
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF69B4"))),
                                new Setter(TextBlock.MarginProperty, new Thickness(74, 0, 0, 0))
                            }
                        };
                    case "프리마틴":
                        return new Style(typeof(TextBlock))
                        {
                            Setters =
                            {
                                new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                                new Setter(TextBlock.FontSizeProperty, 24.0),
                                new Setter(TextBlock.ForegroundProperty, Brushes.DodgerBlue),
                                new Setter(TextBlock.MarginProperty, new Thickness(74, 0, 0, 0))
                            }
                        };
                    case "비거세":
                        return new Style(typeof(TextBlock))
                        {
                            Setters =
                            {
                                new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                                new Setter(TextBlock.FontSizeProperty, 24.0),
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D6B700"))),
                                new Setter(TextBlock.MarginProperty, new Thickness(74, 0, 0, 0))
                            }
                        };
                    case "공통":
                        return new Style(typeof(TextBlock))
                        {
                            Setters =
                            {
                                new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                                new Setter(TextBlock.FontSizeProperty, 24.0),
                                new Setter(TextBlock.ForegroundProperty, Brushes.DodgerBlue),
                                new Setter(TextBlock.MarginProperty, new Thickness(74, 0, 0, 0))
                            }
                        };
                    case "새끼":
                        return new Style(typeof(TextBlock))
                        {
                            Setters =
                            {
                                new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                                new Setter(TextBlock.FontSizeProperty, 24.0),
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF69B4"))),
                                new Setter(TextBlock.MarginProperty, new Thickness(74, 0, 0, 0))
                            }
                        };
                    // 추가적인 경우에 따른 스타일 설정
                    default:
                        return new Style(typeof(TextBlock))
                        {
                            Setters =
                            {
                                new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
                                new Setter(TextBlock.FontSizeProperty, 24.0),
                                new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF69B4"))),
                                new Setter(TextBlock.MarginProperty, new Thickness(74, 0, 0, 0))
                            }
                        };
                }
            }

            return new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.FontWeightProperty, FontWeights.Normal),
                    new Setter(TextBlock.FontSizeProperty, 12.0),
                    new Setter(TextBlock.ForegroundProperty, Brushes.Black)
                }
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
