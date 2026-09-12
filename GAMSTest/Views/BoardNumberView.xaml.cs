using System.Windows.Controls;
namespace GAMSTest.Views;
public partial class BoardNumberView : UserControl
{
    public BoardNumberView(int boardNumber)
    {
        InitializeComponent();
        tbNumber.Text = boardNumber.ToString();
    }
}
