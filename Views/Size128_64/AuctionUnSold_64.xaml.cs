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

namespace CowAuctionSmall.Views.Size128_64
{
    /// <summary>
    /// AuctionUnSold_64.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AuctionUnSold_64 : UserControl
    {
        public AuctionUnSold_64()
        {
            InitializeComponent();
            Loaded += AuctionUnSold_64_Loaded;
        }
        private void AuctionUnSold_64_Loaded(object sender, RoutedEventArgs e)
        { 
                Sex.Text = Sex.Text.Length > 0 ? Sex.Text.Substring(0) : "";

        }
    }
}
