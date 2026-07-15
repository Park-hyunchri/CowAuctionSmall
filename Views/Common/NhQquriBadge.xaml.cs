using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CowAuctionSmall.Views.Common
{
    /// <summary>
    /// NhQquriBadge.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class NhQquriBadge : UserControl
    {
        public NhQquriBadge()
        {
            InitializeComponent();
        }

        // 글자(기본: "뿌리농가")
        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register(
                nameof(LabelText),
                typeof(string),
                typeof(NhQquriBadge),
                new PropertyMetadata("뿌리농가"));

        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        // 글자 크기(기본: 11)
        public static readonly DependencyProperty LabelFontSizeProperty =
            DependencyProperty.Register(
                nameof(LabelFontSize),
                typeof(double),
                typeof(NhQquriBadge),
                new PropertyMetadata(11.0));

        public double LabelFontSize
        {
            get => (double)GetValue(LabelFontSizeProperty);
            set => SetValue(LabelFontSizeProperty, value);
        }

        // 글자 색(기본: Red)
        public static readonly DependencyProperty LabelForegroundProperty =
            DependencyProperty.Register(
                nameof(LabelForeground),
                typeof(Brush),
                typeof(NhQquriBadge),
                new PropertyMetadata(Brushes.Red));

        public Brush LabelForeground
        {
            get => (Brush)GetValue(LabelForegroundProperty);
            set => SetValue(LabelForegroundProperty, value);
        }
    }
}
