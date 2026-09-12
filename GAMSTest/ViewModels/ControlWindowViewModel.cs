using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Input;
using GAMSTest.Models;

namespace CowAuctionSmall.Services { public sealed class DisplaySelect { } }
namespace CowAuctionSmall.ViewModels { public sealed class AuctionContPanelViewModel { } }

namespace GAMSTest.ViewModels
{

public sealed class ControlWindowViewModel : INotifyPropertyChanged
{
    private readonly Action<TesterDisplayState> _state;
    private readonly Action _showNumbers;
    private readonly Action _showPages;
    private readonly Action<Color> _color;
    private int _brightness = 255;
    private Color _baseColor = Colors.Black;
    public int Brightness { get => _brightness; set { _brightness = Math.Clamp(value, 0, 255); OnPropertyChanged(); _color(EffectiveColor); } }
    public ICommand Numbers { get; }
    public ICommand Running { get; }
    public ICommand Epd { get; }
    public ICommand Sold { get; }
    public ICommand UnSold { get; }
    public ICommand Pages { get; }
    public ICommand Red { get; }
    public ICommand Green { get; }
    public ICommand Blue { get; }
    public ICommand White { get; }
    public ICommand Black { get; }
    private Color EffectiveColor => Color.FromRgb((byte)(_baseColor.R * Brightness / 255), (byte)(_baseColor.G * Brightness / 255), (byte)(_baseColor.B * Brightness / 255));
    public ControlWindowViewModel(Action onShowNumbers, Action onShowPages, Action<TesterDisplayState> state, Action<Color> color)
    {
        _showNumbers = onShowNumbers; _showPages = onShowPages; _state = state; _color = color;
        Numbers = Command(() => _showNumbers()); Running = Command(() => _state(TesterDisplayState.Running)); Epd = Command(() => _state(TesterDisplayState.Epd)); Sold = Command(() => _state(TesterDisplayState.Sold)); UnSold = Command(() => _state(TesterDisplayState.UnSold));
        Pages = Command(() => _showPages());
        Red = ColorCommand(Colors.Red); Green = ColorCommand(Colors.Green); Blue = ColorCommand(Colors.Blue); White = ColorCommand(Colors.White); Black = ColorCommand(Colors.Black);
    }
    private ICommand ColorCommand(Color color) => Command(() => { _baseColor = color; _color(EffectiveColor); });
    private static ICommand Command(Action action) => new ActionCommand(action);
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    private sealed class ActionCommand(Action action) : ICommand { public event EventHandler? CanExecuteChanged; public bool CanExecute(object? p) => true; public void Execute(object? p) => action(); }
}
}
