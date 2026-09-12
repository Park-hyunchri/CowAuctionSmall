using System;
using System.Windows;
using System.Windows.Controls;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.Models.XMLParser;
using GAMSTest.Services;

namespace GAMSTest.Views;

public partial class DisplayWindow : Window
{
    public DisplayTestController Controller { get; }
    public DisplayWindow(UserInfo userInfo)
    {
        InitializeComponent(); Controller = new DisplayTestController(userInfo);
        var board = new BoardXmlParser().ParseXml(System.IO.Path.Combine(AppContext.BaseDirectory, "Config", "Board.XML")); Controller.Attach(MainContainer, board);
        var position = userInfo.Auction?.StartPosition?.Split(',', StringSplitOptions.TrimEntries);
        if (position?.Length == 2 && double.TryParse(position[0], out var left) && double.TryParse(position[1], out var top)) { Left = left; Top = top; }
    }
}
