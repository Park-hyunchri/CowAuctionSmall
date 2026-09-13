using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.Services;
using GAMSTest.Models;
using GAMSTest.ViewModels;
using GAMSTest.Views;

namespace GAMSTest.Services;

public sealed class DisplayTestController
{
    private readonly Dictionary<int, Grid> _hosts = new();
    private readonly Dictionary<int, Dictionary<string, UserControl>> _panelViewCache = new();
    private readonly MockDataFactory _factory;
    private readonly UserInfo _userInfo;
    private readonly DispatcherTimer _pageTimer;
    private readonly string _usersXmlPath;
    private int _stepIndex;

    public DisplayTestController(UserInfo userInfo)
    {
        _userInfo = userInfo;
        _factory = new MockDataFactory(userInfo);
        _usersXmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "users.XML");
        _pageTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _pageTimer.Tick += _pageTimer_Tick;
    }

    public void Attach(StackPanel mainContainer, BoardList boardList)
    {
        mainContainer.Children.Clear();
        _hosts.Clear();
        var board = boardList.MultiBoards?.FirstOrDefault();
        if (board?.Rows == null) return;

        foreach (var row in board.Rows)
        {
            var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Height = 128 };
            mainContainer.Children.Add(rowPanel);
            foreach (var number in row)
            {
                var host = new Grid
                {
                    Name = $"Cow_{number}",
                    Width = 128,
                    Height = 128,
                    MinWidth = 128,
                    MaxWidth = 128,
                    MinHeight = 128,
                    MaxHeight = 128,
                    Background = Brushes.Black,
                    ClipToBounds = true
                };
                rowPanel.Children.Add(host);
                _hosts[number] = host;
            }
        }
    }

    public void StartPageCycle()
    {
        try
        {
            _pageTimer.Stop();
            _stepIndex = 0;
            var modes = GetCycleModes();
            ApplyMode(modes[0]);
            WriteDiagnostics(modes);
            _pageTimer.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Cycle Start Error] {ex}");
            MessageBox.Show($"뷰 페이지 시작 중 오류 발생:\n{ex.Message}", "뷰 페이지 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void ShowNumbers()
    {
        _pageTimer.Stop();
        foreach (var panel in _hosts.Values)
        {
            panel.Background = Brushes.Black;
            panel.Children.Clear();
            var name = panel.Name ?? string.Empty;
            var text = name.StartsWith("Cow_", StringComparison.OrdinalIgnoreCase) ? name[4..] : name;
            int.TryParse(text, out var number);
            panel.Children.Add(new BoardNumberView(number));
        }
    }

    public void Fill(Color color)
    {
        _pageTimer.Stop();
        foreach (var host in _hosts.Values)
        {
            host.Children.Clear();
            host.Background = new SolidColorBrush(color);
        }
    }

    public void ShowState(TesterDisplayState state)
    {
        if (state == TesterDisplayState.BoardNumber)
        {
            ShowNumbers();
            return;
        }

        var mode = state switch
        {
            TesterDisplayState.Running => "RUN1",
            TesterDisplayState.Epd => "RUN2",
            TesterDisplayState.UnSold => "UNSOLD",
            _ => "SOLD"
        };
        ShowSingleMode(mode, false);
    }

    public void ShowSingleMode(string mode, bool isRunning = false)
    {
        _pageTimer.Stop();
        ApplySingleMode(mode, isRunning);
    }

    private string[] GetCycleModes()
    {
        var boardPage = 1;
        try
        {
            if (!string.IsNullOrWhiteSpace(_usersXmlPath) && File.Exists(_usersXmlPath))
            {
                var document = System.Xml.Linq.XDocument.Load(_usersXmlPath);
                var value = document
                    .Descendants("PageSetting")
                    .FirstOrDefault()?.Element("BoardPage")?.Value;
                value ??= document
                    .Descendants("BoardPage")
                    .FirstOrDefault()?.Value;
                if (int.TryParse(value, out var parsed)) boardPage = parsed;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BoardPage Read Error] {ex.Message}");
        }

        return boardPage <= 1
            ? new[] { "RUN1", "UNSOLD", "SOLD" }
            : new[] { "RUN1", "RUN2", "UNSOLD", "SOLD" };
    }

    private void _pageTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            var modes = GetCycleModes();
            _stepIndex = (_stepIndex + 1) % modes.Length;
            ApplyMode(modes[_stepIndex]);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Cycle Tick Error] {ex}");
        }
    }

    private void ApplyMode(string mode)
    {
        foreach (var (boardNumber, host) in _hosts)
        {
            if (!_panelViewCache.TryGetValue(boardNumber, out var views))
            {
                views = new Dictionary<string, UserControl>(StringComparer.OrdinalIgnoreCase);
                _panelViewCache[boardNumber] = views;
            }

            if (!views.TryGetValue(mode, out var targetView))
            {
                targetView = CreatePanelView(boardNumber, mode, mode.Equals("RUN1", StringComparison.OrdinalIgnoreCase));
                views[mode] = targetView;
            }

            if (targetView.Parent is Panel oldParent) oldParent.Children.Remove(targetView);
            host.Background = Brushes.Black;
            host.Children.Clear();
            host.Children.Add(targetView);
        }
    }

    private void ApplySingleMode(string mode, bool isRunning)
    {
        foreach (var (boardNumber, host) in _hosts)
        {
            var targetView = CreatePanelView(boardNumber, mode, isRunning);
            if (targetView.Parent is Panel oldParent) oldParent.Children.Remove(targetView);
            host.Background = Brushes.Black;
            host.Children.Clear();
            host.Children.Add(targetView);
        }
    }

    private UserControl CreatePanelView(int boardNumber, string mode, bool isRunning = false)
    {
        var normalizedMode = mode.ToUpperInvariant();
        var status = normalizedMode switch
        {
            "UNSOLD" => "23",
            "SOLD" => "22",
            _ => "11"
        };
        var data = _factory.Create(boardNumber, status, isRunning);
        var vm = new MockPanelViewModel(data);
        var auction = _userInfo.Auction;
        var nhCode = auction?.AuctionHouseCode ?? string.Empty;
        var qquri = auction?.IsShowQQuri ?? string.Empty;
        var selector = new SetCustomDisplay();
        UserControl? innerView = normalizedMode switch
        {
            "RUN1" => selector.CustomAuctionRunning1_128(nhCode, qquri, data.CowDistinction, data.Is_Ｎh_Excellent, data.Is_Mother_Ｎh_Excellent),
            "RUN2" => selector.CustomAuctionRunning2_128(nhCode, qquri, data.CowDistinction, data.Is_Ｎh_Excellent, data.Is_Mother_Ｎh_Excellent),
            "UNSOLD" => selector.CustomAuctionUnSold_128(nhCode, qquri, data.CowDistinction, data.Nh_ability_1_num),
            "SOLD" => selector.CustomAuctionSold_128(nhCode, auction?.BidderName ?? string.Empty, qquri, data.CowDistinction, data.Nh_ability_1_num, data.LowestPriceTitle),
            _ => selector.CustomAuctionRunning1_128(nhCode, qquri, data.CowDistinction, data.Is_Ｎh_Excellent, data.Is_Mother_Ｎh_Excellent)
        };

        if (innerView == null && normalizedMode == "UNSOLD") innerView = new CowAuctionSmall.Views.Size128_128.QQuriUnSold();
        if (innerView == null && normalizedMode == "SOLD") innerView = new CowAuctionSmall.Views.Size128_128.Standard_non_X_Sold();
        if (innerView == null) throw new InvalidOperationException($"{normalizedMode} 뷰 생성 결과가 null입니다.");

        innerView.DataContext = vm;
        innerView.Width = 128;
        innerView.Height = 128;
        var container = new Grid { Width = 128, Height = 128, Background = Brushes.Black, ClipToBounds = true };
        container.Children.Add(innerView);
        var noteCanvas = new Canvas { Margin = new Thickness(0, 108, 0, 0), Height = 20, ClipToBounds = true, IsHitTestVisible = false };
        var note = new TextBlock { Text = vm.Note ?? string.Empty, Foreground = new SolidColorBrush(Color.FromRgb(246, 28, 45)), FontSize = 12 };
        Canvas.SetLeft(note, 2);
        Canvas.SetTop(note, 2);
        noteCanvas.Children.Add(note);
        container.Children.Add(noteCanvas);
        return new UserControl { Width = 128, Height = 128, Content = container };
    }

    private void WriteDiagnostics(string[] modes)
    {
        try
        {
            var assembly = typeof(SetCustomDisplay).Assembly;
            var asmLoc = assembly.Location ?? string.Empty;
            var version = "Unknown";
            if (!string.IsNullOrWhiteSpace(asmLoc) && File.Exists(asmLoc))
            {
                version = FileVersionInfo.GetVersionInfo(asmLoc).FileVersion ?? "Unknown";
            }

            var xmlPath = !string.IsNullOrWhiteSpace(_usersXmlPath) && File.Exists(_usersXmlPath)
                ? Path.GetFullPath(_usersXmlPath)
                : "Not Found";
            var nhCode = _userInfo?.Auction?.AuctionHouseCode ?? "Unknown";
            Debug.WriteLine($"[GAMSTest Init] XML: {xmlPath}, HouseCode: {nhCode}");
            Debug.WriteLine($"[GAMSTest Init] Core DLL: {asmLoc} ({version})");
            Debug.WriteLine($"[GAMSTest Init] Cycle modes: [{string.Join(", ", modes)}]");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GAMSTest Init Log Fail] {ex.Message}");
        }
    }
}
