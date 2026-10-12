using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.ViewModels;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using CowAuctionSmall.Domain.Settings;
using Size = CowAuctionSmall.Presentation.Display.DisplaySizeParser.DisplaySize;

internal static class FactoryChecks
{
    internal static void Run()
    {
        var factory = new AuctionViewFactory();
        var original = new SetCustomDisplay();
        foreach (string house in new[] { "TEST", "8808990817675", "8808990643625", "8808990656557", "8808990656106", "8808990661315", "8808990656885", "8808990656458" })
        foreach (string qquri in new[] { "X", "Y", "N" })
        foreach (string animal in new[] { "1", "2", "3", "5", "6" })
        {
            var options = Options(Size.Size128x128, house, qquri, animal);
            Program.Check(factory.CreateRunning(options, 1)?.GetType() == original.CustomAuctionRunning1_128(house, qquri, animal, "N", "N").GetType(), "128 running page 1 selection parity");
            Program.Check(factory.CreateRunning(options, 2)?.GetType() == original.CustomAuctionRunning2_128(house, qquri, animal, "N", "N").GetType(), "128 running page 2 selection parity");
            Program.Check(factory.CreateRunning(options, 3)?.GetType() == original.CustomAuctionRunning3_128(house)?.GetType(), "128 optional history page selection parity");
            Program.Check(factory.CreateSold(options).GetType() == original.CustomAuctionSold_128(house, qquri, animal, "최저가").GetType(), "128 sold selection parity");
            Program.Check(factory.CreateUnSold(options).GetType() == original.CustomAuctionUnSold_128(house, qquri, animal, "123").GetType(), "128 unsold selection parity");
        }

        foreach (string house in new[] { "TEST", "0002" })
        {
            var options = Options(Size.Size128x64, house);
            string[] expected = house == "0002"
                ? new[] { "AuctionRunning1_64", "AuctionRunning3_64", "AuctionRunning4_64", "AuctionRunning2_64" }
                : new[] { "AuctionRunning1_64", "AuctionRunning2_64", "AuctionRunning3_64", "AuctionRunning4_64" };
            Program.Check(AuctionViewFactory.GetRunningPageNumbers(options).SequenceEqual(new[] { 1, 2, 3, 4 }), "128x64 has four pages");
            for (int index = 0; index < expected.Length; index++)
                Program.Check(factory.CreateRunning(options, index + 1)?.GetType().Name == expected[index], "128x64 special/default page order");
            Program.Check(factory.CreateSold(options).GetType().Name == "AuctionSold_64", "128x64 sold type");
            Program.Check(factory.CreateUnSold(options).GetType().Name == "AuctionUnSold_64", "128x64 unsold type");
        }

        foreach (Size size in new[] { Size.Size160x64, Size.Size320x64 })
        {
            var options = Options(size);
            Program.Check(AuctionViewFactory.GetRunningPageNumbers(options).SequenceEqual(new[] { 1, 2 }), "wide display page list");
            string suffix = size == Size.Size160x64 ? "160_64" : "320_64";
            Program.Check(factory.CreateRunning(options, 1)?.GetType().Name == "AuctionRunning1_" + suffix, "wide running 1 type");
            Program.Check(factory.CreateRunning(options, 2)?.GetType().Name == "AuctionRunning2_" + suffix, "wide running 2 type");
            Program.Check(factory.CreateSold(options).GetType().Name == "AuctionSold_" + suffix, "wide sold type");
            Program.Check(factory.CreateUnSold(options).GetType().Name == "AuctionUnSold_" + suffix, "wide unsold type");
            Program.Check(factory.CreateRunning(options, 3) == null, "wide absent page is not a fallback page");
            Program.Check(!AuctionViewFactory.IsPlaceholder(factory.CreateRunning(options, 1)!), "wide running 1 is implemented");
            Program.Check(AuctionViewFactory.IsPlaceholder(factory.CreateRunning(options, 2)!) == (size == Size.Size320x64), "320 running 2 placeholder explicitly identified");
            Program.Check(AuctionViewFactory.IsPlaceholder(factory.CreateSold(options)) == (size == Size.Size320x64), "320 sold placeholder explicitly identified");
            Program.Check(AuctionViewFactory.IsPlaceholder(factory.CreateUnSold(options)) == (size == Size.Size320x64), "320 unsold placeholder explicitly identified");
        }

        var normal = Options(Size.Size128x128);
        Program.Check(AuctionViewFactory.GetRunningPageNumbers(normal).SequenceEqual(new[] { 1, 2 }), "ordinary 128 excludes absent history page");
        Program.Check(AuctionViewFactory.GetRunningPageNumbers(Options(Size.Size128x128, "8808990817675")).SequenceEqual(new[] { 1, 2, 3 }), "history house includes third page");
        Program.Check(AuctionViewFactory.ShouldUseNoteHost(normal, factory.CreateRunning(normal, 1)), "ordinary running uses shared note host");
        var breeding = Options(Size.Size128x128, animal: "3");
        Program.Check(!AuctionViewFactory.ShouldUseNoteHost(breeding, factory.CreateRunning(breeding, 2)), "breeding EPD excludes note host");
        Program.Check(!AuctionViewFactory.ShouldUseNoteHost(Options(Size.Size128x128, animal: "5")), "goat excludes shared note host");
        Program.Check(!AuctionViewFactory.ShouldUseNoteHost(Options(Size.Size128x64)), "64-height excludes shared note host");
        Program.Throws<ArgumentException>(() => factory.CreateRunning(Options(Size.Unknown), 1), "unknown size rejected");

        var source = new gValues { SpaceIndex = "1", OwnerName = "가상긴출하주", Note = "가상 비고", CowDistinction = "1", Sex = "비거세" };
        using var model = new AuctionContPanelViewModel(source, new VirtualizingStackPanel(), 2);
        Program.Check(model.ItemDisplay.SexText == "비거세" && model.ItemDisplay.Snapshot.OwnerName == source.OwnerName, "offline ViewModel uses real display rules");
        source.Note = "외부 변경";
        Program.Check(model.CowInfo.Note == "가상 비고", "offline ViewModel owns its snapshot");
        CheckLegacyUnknownSize();
    }

    private static void CheckLegacyUnknownSize()
    {
        // Bypass the operational constructor only in the test, as existing boundary checks do.
        foreach (Size size in new[] { Size.Unknown, Size.Size128x64 })
        {
            Type storeType = typeof(DisplaySelect).Assembly.GetType("CowAuctionSmall.Presentation.Display.DisplayPanelStore", true)!;
            using var store = (IDisposable)Activator.CreateInstance(storeType, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { new ObservableCollection<AuctionContPanelViewModel>() }, null)!;
            var display = (DisplaySelect)RuntimeHelpers.GetUninitializedObject(typeof(DisplaySelect));
            void Set(string field, object value) => typeof(DisplaySelect).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(display, value);
            Set("_panelStates", store);
            Set("_displaySize", size);
            Set("_nhCode", "0002");
            Set("_setCustomDisplay", new SetCustomDisplay());
            Set("_userInfo", new UserInfo { Auction = new Auction() });
            Set("_totalRunningPage", 4);
            var panel = new VirtualizingStackPanel { Width = 128, Height = 64 };
            using var model = new AuctionContPanelViewModel(new gValues(), panel, 4);
            display.InitializePages(panel, model, 4, "Y", "N", "N", "1");
            object state = storeType.GetMethod("Get", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(store, new object[] { panel })!;
            var pages = (List<UserControl>)state.GetType().GetField("RunningPages")!.GetValue(state)!;
            string[] expected = size == Size.Unknown
                ? ["AuctionRunning1_64", "AuctionRunning2_64", "AuctionRunning3_64", "AuctionRunning4_64"]
                : ["AuctionRunning1_64", "AuctionRunning3_64", "AuctionRunning4_64", "AuctionRunning2_64"];
            Program.Check(pages.Select(page => page.GetType().Name).SequenceEqual(expected), "legacy unknown-size fallback does not take explicit 128x64 special order");
        }
    }

    internal static AuctionViewOptions Options(Size size, string house = "TEST", string qquri = "Y", string animal = "1") => new()
    {
        Size = size, NhCode = house, IsQquri = qquri, CowDistinction = animal,
        IsNhExcellent = "N", IsMotherNhExcellent = "N", IsShowQquri = qquri,
        LowestPriceTitle = "최저가", AbilityNumber = "123"
    };
}
