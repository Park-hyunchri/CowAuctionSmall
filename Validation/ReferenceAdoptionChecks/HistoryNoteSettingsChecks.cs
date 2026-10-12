using CowAuctionSmall.Domain.AnimalHistory;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Infra.XmlSettings;
using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views.Size128_128.Running;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Threading;

internal static class HistoryNoteSettingsChecks
{
    internal static void Run()
    {
        const string number = "410000000000001";
        var supplied = new HistoryLookupResult(true, "SYNTHETIC_HISTORY", HistoryLookupOutcome.Cache)
        {
            LatestOwner = new AnimalHistoryOwner("SYNTHETIC_OWNER", "SYNTHETIC_ADDRESS")
        };
        using var batch = new AnimalHistoryCoordinator((_, _, _) => Task.FromResult(supplied),
            _ => Task.FromResult(supplied), () => "fixture", action => action(), _ => { });
        batch.UpdateTargets(new[] { number });
        batch.Completion.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        var running = new ObservableCollection<AuctionContPanelViewModel>();
        var storeType = typeof(DisplaySelect).Assembly.GetType("CowAuctionSmall.Presentation.Display.DisplayPanelStore", true)!;
        using var store = (IDisposable)Activator.CreateInstance(storeType, BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { running }, null)!;
        var display = (DisplaySelect)RuntimeHelpers.GetUninitializedObject(typeof(DisplaySelect));
        Set(display, "_displaySize", DisplaySizeParser.Parse("128,128"));
        Set(display, "_nhCode", "fixture");
        Set(display, "_setCustomDisplay", new SetCustomDisplay());
        Set(display, "_panelStates", store);
        Set(display, "_userInfo", new UserInfo { Auction = new Auction { IsShowQQuri = "N" } });
        var model = (MainWindowViewModel)RuntimeHelpers.GetUninitializedObject(typeof(MainWindowViewModel));
        var panel = new VirtualizingStackPanel { Name = "Cow_1", Width = 128, Height = 128 };
        Set(model, "_displaySelect", display);
        Set(model, "_dispatcher", Dispatcher.CurrentDispatcher);
        Set(model, "_panelsByName", new Dictionary<string, List<VirtualizingStackPanel>> { ["Cow_1"] = new() { panel } });
        Set(model, "_visibleCows", new Dictionary<string, gValues>());
        Set(model, "_displayUpdates", new DisplayUpdateTracker());
        Set(model, "_historyReady", true);
        Set(model, "_historyCoordinator", batch);
        var cow = new gValues { SpaceIndex = "1", CowDistinction = "1", AuctionResultStatus = "22",
            TraceNumber = number, Note = "SYNTHETIC_NOTE", Sex = "수" };
        string path = Path.GetTempFileName();
        try
        {
            foreach (var option in new[] { ("", true), ("Y", true), ("N", false), (" n ", false), ("Y", true) })
            {
                string tag = option.Item1 == "" ? "" : $"<ShowOwnerChangeNote>{option.Item1}</ShowOwnerChangeNote>";
                File.WriteAllText(path, "<User><AnimalTrace><IsAnimalProductsTraceability>Y</IsAnimalProductsTraceability>"
                    + tag + "<ServiceKey>SYNTHETIC_KEY</ServiceKey></AnimalTrace></User>");
                var settings = new UserXmlParser().ParseXml(path).AnimalTrace;
                Program.Check(settings.Enabled && settings.ShowOwnerChangeNote == option.Item2,
                    "XML note visibility is independent of enabled history lookup and defaults to visible");
                Set(model, "_showOwnerChangeNote", settings.ShowOwnerChangeNote);
                model.UpdatePanels(new ObservableCollection<gValues> { cow.Clone() });
                var host = (RunningNoteHost128)panel.Children[0];
                var itemDisplay = host.ItemDisplay ?? throw new InvalidOperationException("Result note host has no display model.");
                var snapshot = itemDisplay.Snapshot;
                Program.Check(snapshot.Note == "SYNTHETIC_NOTE"
                    && snapshot.NoteChangeOwner == (option.Item2 ? supplied.Note : "")
                    && snapshot.DisplayNote == (option.Item2 ? "SYNTHETIC_NOTE / SYNTHETIC_HISTORY" : "SYNTHETIC_NOTE"),
                    "common result note host changes only history visibility including hide after show");
                Program.Check(itemDisplay.HistoryOwnerNameText == "SYNTHETIC_OWNER"
                    && itemDisplay.HistoryFarmAddressText == "SYNTHETIC_ADDRESS",
                    "third-page display properties retain owner and address when notes are hidden");
                Program.Check(batch.TryGetHistory(number, out var retained) && retained == supplied,
                    "display suppression does not discard the committed lookup result");
            }
        }
        finally { File.Delete(path); }
    }

    private static void Set(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
}
