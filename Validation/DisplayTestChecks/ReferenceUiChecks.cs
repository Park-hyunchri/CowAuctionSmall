using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GAMSTest.Models;
using GAMSTest.Services;
using GAMSTest.ViewModels;
using GAMSTest.Views;
using Size = CowAuctionSmall.Presentation.Display.DisplaySizeParser.DisplaySize;

internal static class ReferenceUiChecks
{
    internal static void Run()
    {
        var canvas = new Canvas();
        using var controller = new DisplayTestController(canvas);
        var errors = new List<string>();
        int launches = 0;
        var settings = new TestSettings { Rows = [[1]], PageCount = 1 };
        string? selectedBoard = null;
        using var vm = new ControlWindowViewModel(controller, settings, () => launches++, errors.Add, () => selectedBoard);
        Program.Check(vm.Brightness == 170 && ((Border)canvas.Children[0]).Child is BoardNumberView, "reference brightness and initial number screen");
        Program.Check(vm.ShowRun2Command.CanExecute(null), "manual genetics stays available even when BoardPage is one");
        vm.ShowRun2Command.Execute(null);
        Program.Check(controller.CurrentMode == "진행 2페이지" && !controller.IsCycling, "one-click genetics command");
        vm.ShowRun1Command.Execute(null);
        Program.Check(controller.CurrentMode == "진행 1페이지", "one-click running command");
        vm.ShowUnsoldCommand.Execute(null);
        Program.Check(controller.CurrentMode == "유찰", "one-click unsold command");
        vm.ShowSoldCommand.Execute(null);
        Program.Check(controller.CurrentMode == "낙찰", "one-click sold command");
        vm.StartCycleCommand.Execute(null);
        Program.Check(controller.IsCycling && controller.CurrentMode == "진행 1페이지", "reference cycle begins with running rather than extra WAIT mode");
        // Invoke the existing single timer's callback deterministically, without adding another timer.
        var tick = typeof(DisplayTestController).GetMethod("OnTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        tick.Invoke(controller, [null, EventArgs.Empty]);
        Program.Check(controller.CurrentMode == "유찰", "one-page cycle advances to unsold");
        tick.Invoke(controller, [null, EventArgs.Empty]);
        Program.Check(controller.CurrentMode == "낙찰", "reference cycle advances from unsold to sold");
        vm.Green.Execute(null);
        Program.Check(!controller.IsCycling && ((SolidColorBrush)((Border)canvas.Children[0]).Background).Color == Color.FromRgb(0, 85, 0), "reference Green and 170 brightness retained");
        vm.ShowBoardNumberCommand.Execute(null);
        vm.Brightness = 255;
        Program.Check(((Border)canvas.Children[0]).Child == null && ((SolidColorBrush)((Border)canvas.Children[0]).Background).Color == Colors.Green, "brightness returns to previous color as reference UI does");
        foreach (Size size in new[] { Size.Size128x64, Size.Size128x128, Size.Size160x64, Size.Size320x64 })
        {
            var sizeValue = TestSettings.GetDimensions(size);
            selectedBoard = Path.Combine(Path.GetTempPath(), "Board-" + Guid.NewGuid().ToString("N") + ".xml");
            string xml = $"<Boards><Size>{sizeValue.Width},{sizeValue.Height}</Size><MultiBoard><RowIdx>1</RowIdx></MultiBoard></Boards>";
            try
            {
                File.WriteAllText(selectedBoard, xml);
                vm.OpenBoardCommand.Execute(null);
                Program.Check(vm.BoardPathToolTip == selectedBoard && File.ReadAllText(selectedBoard) == xml, "chosen board path shown and source XML unchanged");
            }
            finally { File.Delete(selectedBoard); }
            vm.ShowBoardNumberCommand.Execute(null);
            var number = (BoardNumberView)((Border)canvas.Children[0]).Child;
            var dimensions = TestSettings.GetDimensions(size);
            Program.Check(number.Width == dimensions.Width && number.Height == dimensions.Height && number.Number == "1", "selected XML size applies immediately to reference number view");
            Program.Check(((TextBlock)number.FindName("title")).Text == "경매번호" && ((Border)number.Content).BorderThickness.Left == 2, "reference title and blue border preserved");
        }
        Program.Check(settings.Resolution == Size.Size128x128, "board import leaves original settings snapshot unchanged");
        Program.Check(!vm.ShowRun2Command.CanExecute(null) && !vm.ShowUnsoldCommand.CanExecute(null) && !vm.ShowSoldCommand.CanExecute(null), "320 empty views disabled on reference buttons");
        vm.LaunchAuctionCommand.Execute(null);
        Program.Check(launches == 1 && errors.Count == 0, "AMS button dispatches once to injected action without executing operational application");
        var display = new DisplayWindow();
        Program.Check(display.WindowStyle == WindowStyle.None && display.ResizeMode == ResizeMode.NoResize && display.SizeToContent == SizeToContent.WidthAndHeight && display.Content is Canvas, "reference borderless display has no preview scrollbar or scaling");
        display.Close();
        CheckStartup();
        CheckBoardSelection();
    }

    private static void CheckBoardSelection()
    {
        string path = Path.Combine(Path.GetTempPath(), "Board-" + Guid.NewGuid().ToString("N") + ".xml");
        string? selected = path;
        var canvas = new Canvas();
        using var controller = new DisplayTestController(canvas);
        var errors = new List<string>();
        var original = new TestSettings { Rows = [[1]], BidderName = "Y", DisplayLeft = -128, PageCount = 1 };
        using var vm = new ControlWindowViewModel(controller, original, () => { }, errors.Add, () => selected);
        try
        {
            File.WriteAllText(path, "<Boards><Size>160,64</Size><MultiBoard><RowIdx>9,3</RowIdx><RowIdx>8,9</RowIdx></MultiBoard><MultiBoard><RowIdx>99</RowIdx></MultiBoard></Boards>");
            vm.StartCycleCommand.Execute(null);
            vm.OpenBoardCommand.Execute(null);
            Program.Check(!controller.IsCycling && controller.CurrentMode == "계류대 번호" && controller.CachedViewCount == 0, "successful board replacement stops cycle and releases cached views");
            Program.Check(canvas.Width == 320 && canvas.Height == 128 && canvas.Children.Cast<Border>().Select(host => ((BoardNumberView)host.Child).Number).SequenceEqual(new[] { "9", "3", "8", "9" }), "XML first layout, row order and duplicate numbers applied");
            Program.Check(vm.Settings.BidderName == "Y" && vm.Settings.DisplayLeft == -128 && vm.Settings.PageCount == 1, "opening board preserves existing users settings and display location");
            string boardXml = File.ReadAllText(path);
            foreach (var choice in vm.Resolutions)
            {
                vm.SelectedResolution = choice.Value;
                var size = TestSettings.GetDimensions(choice.Value);
                Program.Check(canvas.Width == size.Width * 2 && canvas.Height == size.Height * 2 && vm.Settings.BidderName == "Y" && vm.Settings.Rows[1][1] == 9, "manual size choice preserves XML arrangement and user settings");
                Program.Check(File.ReadAllText(path) == boardXml && vm.BoardPathToolTip == path, "manual size choice leaves XML and selected path intact");
            }
            vm.OpenBoardCommand.Execute(null);
            Program.Check(vm.SelectedResolution == Size.Size160x64 && canvas.Width == 320 && canvas.Height == 128, "opening XML again restores its size after manual override");
            var previous = vm.Settings;
            vm.StartCycleCommand.Execute(null);
            var child = ((Border)canvas.Children[0]).Child;
            selected = null;
            vm.OpenBoardCommand.Execute(null);
            Program.Check(errors.Count == 0 && ReferenceEquals(previous, vm.Settings) && controller.IsCycling && ReferenceEquals(child, ((Border)canvas.Children[0]).Child), "cancel preserves running display and imported path");
            selected = path;
            foreach (string invalid in new[]
            {
                "<Boards>",
                "<Boards><Size>7,7</Size><MultiBoard><RowIdx>1</RowIdx></MultiBoard></Boards>",
                "<Boards><Size>128,64</Size><MultiBoard><RowIdx>x</RowIdx></MultiBoard></Boards>",
                "<Boards><Size>128,64</Size><MultiBoard><RowIdx>" + string.Join(",", Enumerable.Range(1, 401)) + "</RowIdx></MultiBoard></Boards>"
            })
            {
                File.WriteAllText(path, invalid);
                int count = errors.Count;
                vm.OpenBoardCommand.Execute(null);
                Program.Check(errors.Count == count + 1 && ReferenceEquals(previous, vm.Settings) && controller.IsCycling && ReferenceEquals(child, ((Border)canvas.Children[0]).Child), "invalid XML or oversized layout preserves current screen, cycle and settings");
            }
            File.Delete(path);
            int before = errors.Count;
            vm.OpenBoardCommand.Execute(null);
            Program.Check(errors.Count == before + 1 && ReferenceEquals(previous, vm.Settings) && controller.IsCycling, "missing selected file preserves current state");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void CheckStartup()
    {
        string directory = Path.Combine(Path.GetTempPath(), "GAMSTestUiChecks", Guid.NewGuid().ToString("N"));
        string local = Path.Combine(directory, "tool");
        string working = Path.Combine(directory, "work");
        Directory.CreateDirectory(Path.Combine(local, "Config"));
        Directory.CreateDirectory(Path.Combine(working, "Config"));
        string board = Path.Combine(local, "Config", "Board.XML");
        string users = Path.Combine(local, "Config", "users.XML");
        File.WriteAllText(board, "<Boards><Size>160,64</Size><MultiBoard><RowIdx>9,9</RowIdx></MultiBoard></Boards>");
        File.WriteAllText(users, "<Users><Auction><StartPosition>-128,64</StartPosition></Auction></Users>");
        File.WriteAllText(Path.Combine(working, "Config", "Board.XML"), "<INVALID>");
        var loaded = TestSettingsLoader.LoadStartup(local, working);
        Program.Check(loaded.Resolution == Size.Size160x64 && loaded.Rows[0].SequenceEqual(new[] { 9, 9 }), "startup prefers executable-local configuration over working directory");
        Program.Check(loaded.BoardXmlPath == board, "startup retains imported board path for tooltip");
        Program.Check(loaded.DisplayLeft == -128 && loaded.DisplayTop == 64, "startup reads display coordinates including negative monitor position");
        foreach (string coordinates in new[] { "NaN,0", "0,Infinity", "1", "x,2" })
        {
            File.WriteAllText(users, "<Users><Auction><StartPosition>" + coordinates + "</StartPosition></Auction></Users>");
            Program.Throws<FormatException>(() => TestSettingsLoader.LoadStartup(local, working), "invalid display coordinates rejected");
        }
        File.Delete(board); File.Delete(users);
        File.Delete(Path.Combine(working, "Config", "Board.XML"));
        Directory.CreateDirectory(Path.Combine(local, "Examples"));
        string example = Path.Combine(local, "Examples", "Board.XML");
        File.WriteAllText(example, "<Boards><Size>128,64</Size><MultiBoard><RowIdx>1</RowIdx></MultiBoard></Boards>");
        Program.Check(TestSettingsLoader.LoadStartup(local, working).Resolution == Size.Size128x64, "standalone fallback uses synthetic examples");
        File.Delete(example);
        var embedded = TestSettingsLoader.LoadStartup(local, working);
        Program.Check(embedded.Rows.Count == 2 && embedded.Rows.Sum(row => row.Count) == 8 && embedded.Resolution == Size.Size128x128, "no external configuration uses embedded eight-board default");
        Program.Check(embedded.BoardXmlPath == null && embedded.IsShowOwnerName == "NN" && embedded.Colors["LocationForeground"] == "Lime", "embedded users settings and colors load without filesystem paths");
        Program.Check(!File.Exists(example), "loading embedded defaults does not create loose XML files");
        string fakeExe = Path.Combine(local, "CowAuctionSmall.exe");
        File.WriteAllText(fakeExe, "fake executable, never launched");
        Program.Check(AuctionApplicationLauncher.FindExecutable(local, working) == fakeExe, "deployment-local AMS executable selected without launching it");
        File.Delete(fakeExe);
        Program.Check(AuctionApplicationLauncher.FindExecutable(local, working) == null, "missing AMS delegates to executable picker");
        foreach (string path in new[] { Path.Combine(local, "Config"), Path.Combine(local, "Examples"), Path.Combine(working, "Config"), local, working, directory })
            Directory.Delete(path);
    }
}
