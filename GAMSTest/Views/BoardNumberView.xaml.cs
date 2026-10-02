using System.Windows.Controls;
namespace GAMSTest.Views;
public partial class BoardNumberView : UserControl
{
    public BoardNumberView(int boardNumber, double width = 128, double height = 128)
    {
        InitializeComponent();
        Width = width;
        Height = height;
        tbTitle.FontSize = height <= 64 ? 9 : 13;
        tbTitle.Margin = new System.Windows.Thickness(0, height <= 64 ? 2 : 10, 0, 0);
        tbNumber.Text = boardNumber.ToString();
        tbNumber.FontSize = height <= 64 ? 27 : 54;
        tbNumber.Margin = new System.Windows.Thickness(0, height <= 64 ? 7 : 15, 0, 0);
    }
}
