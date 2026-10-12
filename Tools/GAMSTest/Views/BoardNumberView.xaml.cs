using System.Windows;
using System.Windows.Controls;

namespace GAMSTest.Views;

public partial class BoardNumberView : UserControl
{
    public string Number => tbNumber.Text;

    public BoardNumberView(int boardNumber, double width, double height)
    {
        InitializeComponent();
        Width = width;
        Height = height;
        tbNumber.Text = boardNumber.ToString();
        if (height <= 64)
        {
            title.Margin = new Thickness(0, 2, 0, 0);
            tbNumber.FontSize = 32;
        }
    }
}
