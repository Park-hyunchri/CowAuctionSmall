using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.Services;
using GAMSTest.Models;
using GAMSTest.Views;

namespace GAMSTest.Services;

public sealed class DisplayTestController
{
    private readonly Dictionary<int, Grid> _hosts = new();
    private readonly MockDataFactory _factory;
    private readonly UserInfo _userInfo;
    private readonly Brush _black = new SolidColorBrush(Colors.Black);
    private readonly DispatcherTimer _pageTimer;
    private int _pageIndex;

    public DisplayTestController(UserInfo userInfo) { _userInfo = userInfo; _factory = new MockDataFactory(userInfo); _pageTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) }; _pageTimer.Tick += (_, _) => { _pageIndex = (_pageIndex + 1) % 4; ShowState(_pageIndex switch { 0 => TesterDisplayState.Running, 1 => TesterDisplayState.Epd, 2 => TesterDisplayState.UnSold, _ => TesterDisplayState.Sold }); }; }
    public void StartPageCycle() { _pageTimer.Stop(); _pageIndex = 0; ShowState(TesterDisplayState.Running); _pageTimer.Start(); }

    public void Attach(StackPanel mainContainer, BoardList boardList)
    {
        mainContainer.Children.Clear();
        var board = boardList.MultiBoards?.FirstOrDefault();
        if (board?.Rows == null) return;
        foreach (var (row, r) in board.Rows.Select((value, index) => (value, index)))
        {
            var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Height = 128 };
            mainContainer.Children.Add(rowPanel);
            foreach (var (number, c) in row.Select((value, index) => (value, index)))
            {
                var host = new Grid { Name = $"Cow_{number}", Width = 128, Height = 128, MinWidth = 128, MaxWidth = 128, MinHeight = 128, MaxHeight = 128, Background = _black, ClipToBounds = true };
                rowPanel.Children.Add(host); _hosts[number] = host;
            }
        }
    }

    public void ShowNumbers()
    {
        _pageTimer.Stop();
        foreach (var panel in _hosts.Values)
        {
            panel.Background = Brushes.Black;
            panel.Children.Clear();

            var boardNumber = 0;
            if (!string.IsNullOrWhiteSpace(panel.Name))
            {
                var numberText = panel.Name.StartsWith("Cow_", StringComparison.OrdinalIgnoreCase) ? panel.Name[4..] : panel.Name;
                int.TryParse(numberText, out boardNumber);
            }

            panel.Children.Add(new BoardNumberView(boardNumber));
        }
    }
    public void Fill(Color color) { _pageTimer.Stop(); foreach (var host in _hosts.Values) { host.Children.Clear(); host.Background = new SolidColorBrush(color); } }
    public void ShowState(TesterDisplayState state)
    {
        if (state == TesterDisplayState.BoardNumber)
        {
            ShowNumbers();
            return;
        }

        foreach (var pair in _hosts)
        {
            pair.Value.Children.Clear();
            pair.Value.Background = Brushes.Black;
            var data = _factory.Create(pair.Key, state == TesterDisplayState.Sold ? "22" : state == TesterDisplayState.UnSold ? "23" : "11", state is TesterDisplayState.Running or TesterDisplayState.Epd);
            var auction = _userInfo.Auction;
            var selector = new SetCustomDisplay();
            UserControl view = state switch
            {
                TesterDisplayState.Running => selector.CustomAuctionRunning1_128(auction?.AuctionHouseCode ?? "", auction?.IsShowQQuri ?? "", data.CowDistinction, data.Is_Ｎh_Excellent, data.Is_Mother_Ｎh_Excellent),
                TesterDisplayState.Epd => selector.CustomAuctionRunning2_128(auction?.AuctionHouseCode ?? "", auction?.IsShowQQuri ?? "", data.CowDistinction, data.Is_Ｎh_Excellent, data.Is_Mother_Ｎh_Excellent),
                TesterDisplayState.UnSold => selector.CustomAuctionUnSold_128(auction?.AuctionHouseCode ?? "", auction?.IsShowQQuri ?? "", data.CowDistinction, data.Nh_ability_1_num),
                _ => selector.CustomAuctionSold_128(auction?.AuctionHouseCode ?? "", auction?.BidderName ?? "", auction?.IsShowQQuri ?? "", data.CowDistinction, data.Nh_ability_1_num, data.LowestPriceTitle)
            };
            view.Width = 128; view.Height = 128; view.DataContext = data; pair.Value.Children.Add(view);
        }
    }
}
