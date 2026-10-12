using System.Windows;
using System.Windows.Controls;

namespace GAMSTest.Views;

public partial class DisplayWindow : Window
{
    public Canvas Surface => BoardCanvas;
    public DisplayWindow() => InitializeComponent();
}
