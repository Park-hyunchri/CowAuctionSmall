using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Settings;
using CowAuctionSmall.Presentation.Display;
using CowAuctionSmall.ViewModels;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;

internal static class FooterRotationChecks
{
    internal static void Run()
    {
        // Constructor bypass avoids starting UDP sync, API requests or production timers.
        _ = System.Windows.Application.Current ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var selector = (DisplaySelect)RuntimeHelpers.GetUninitializedObject(typeof(DisplaySelect));
        typeof(DisplaySelect).GetField("pageTime", flags)!.SetValue(selector, new List<int> { 60, 60 });
        typeof(DisplaySelect).GetField("_totalRunningPage", flags)!.SetValue(selector, 2);
        typeof(DisplaySelect).GetField("_rotationIndex", flags)!.SetValue(selector, 1);
        typeof(DisplaySelect).GetField("_userInfo", flags)!.SetValue(selector, new UserInfo { Auction = new Auction() });
        typeof(DisplaySelect).GetProperty(nameof(DisplaySelect.TodayAuctionItems))!.SetValue(selector,
            new ObservableCollection<AuctionContPanelViewModel>());
        var timerField = typeof(DisplaySelect).GetField("_timer", flags)!;
        var publish = typeof(DisplaySelect).GetMethod("PublishPageIndicatorState", flags)!;
        var tick = typeof(DisplaySelect).GetMethod("Timer_Elapsed", flags)!;
        var recipient = new object();
        PageIndicatorStateMessage? last = null;
        int runningNumber = ServerGetData._runRunSipNumber;
        using var timer = new System.Timers.Timer(60000) { AutoReset = false };
        WeakReferenceMessenger.Default.Register<PageIndicatorStateMessage>(recipient, (_, message) => last = message);
        try
        {
            ServerGetData._runRunSipNumber = -1;
            publish.Invoke(selector, null);
            Program.Check(last is { CurrentPage: 2, TotalPages: 2, IsFrozen: false, IsTimerRunning: false },
                "unfrozen page state with no timer reports stopped rather than assuming rotation");

            timerField.SetValue(selector, timer);
            timer.Start();
            publish.Invoke(selector, null);
            Program.Check(last is { IsMaster: false, IsTimerRunning: true },
                "published timer execution is independent of the MASTER/SUB role");

            typeof(DisplaySelect).GetField("_isSubFallbackActive", flags)!.SetValue(selector, true);
            timer.Stop();
            publish.Invoke(selector, null);
            Program.Check(last is { IsSubFallbackActive: true, IsTimerRunning: false, IsFrozen: false },
                "fallback mode preserves a stopped timer without fabricating a freeze reason");

            // AutoReset=false is disabled during Elapsed. The final message must include the restart.
            tick.Invoke(selector, new object?[] { null, null });
            Program.Check(timer.Enabled && last is { IsTimerRunning: true, IsSubFallbackActive: true },
                "one-shot page transition publishes the final restarted timer state");
            Program.Check(last!.CurrentPage >= 1 && last.CurrentPage <= last.TotalPages,
                "page transition retains valid current/total page values");

            timer.Stop();
            typeof(DisplaySelect).GetField("_stoppedBySignal", flags)!.SetValue(selector, true);
            publish.Invoke(selector, null);
            Program.Check(last is { IsTimerRunning: false, IsFrozen: true, FreezeReason: "StopTimer" },
                "server stop retains the explicit freeze reason alongside actual timer state");

            var legacy = new PageIndicatorStateMessage(1, 1, true, false, false);
            Program.Check(!legacy.IsTimerRunning && legacy.FreezeReason == string.Empty,
                "legacy page-message construction does not presume a running timer");
            var snapshot = PageRotationSnapshot.Create(2, 3, true, false, false, false, false, true);
            Program.Check(snapshot.IsTimerRunning && !snapshot.IsFrozen && snapshot.CurrentPage == 2,
                "snapshot carries a running timer independently from freeze classification");
        }
        finally
        {
            timer.Stop();
            timerField.SetValue(selector, null);
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
            ServerGetData._runRunSipNumber = runningNumber;
        }
    }
}
