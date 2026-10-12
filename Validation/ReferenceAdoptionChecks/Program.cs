internal static class Program
{
    private static int _checks;

    [STAThread]
    private static int Main(string[] args)
    {
        NLog.LogManager.Configuration = new NLog.Config.LoggingConfiguration();
        try
        {
            string stage = args.FirstOrDefault() ?? "all";
            if (stage is "all" or "stage1")
            {
                LogoChecks.Run();
                BoardChecks.Run();
            }
            if (stage is "all" or "stage2") StateChecks.Run();
            if (stage is "all" or "stage3")
            {
                SnapshotChecks.Run();
                IngressChecks.Run().GetAwaiter().GetResult();
                ServiceChecks.Run().GetAwaiter().GetResult();
                ResultChecks.Run().GetAwaiter().GetResult();
                BatchStateChecks.Run().GetAwaiter().GetResult();
                RefreshChecks.Run().GetAwaiter().GetResult();
                SignalChecks.Run().GetAwaiter().GetResult();
                AuctionSectionChecks.Run().GetAwaiter().GetResult();
                NightShutdownChecks.Run().GetAwaiter().GetResult();
                QcnChecks.Run().GetAwaiter().GetResult();
                PollingLifetimeChecks.Run().GetAwaiter().GetResult();
            }
            if (stage is "all" or "history")
            {
                HistoryItemDisplayChecks.Run();
                HistoryBatchChecks.Run().GetAwaiter().GetResult();
                HistoryBackgroundChecks.Run().GetAwaiter().GetResult();
            }
            if (stage is "all" or "history" or "farm-address")
            {
                FarmAddressChecks.RunView();
                FarmAddressChecks.RunAsync().GetAwaiter().GetResult();
            }
            if (stage is "all" or "updates" or "updates-baseline") ItemUpdateChecks.Run(stage == "updates-baseline").GetAwaiter().GetResult();
            if (stage is "all" or "logging") LoggingChecks.Run().GetAwaiter().GetResult();
            if (stage is "all" or "logging" or "log-archive") LogArchiveChecks.Run();
            if (stage is "all" or "entity") EntityNumberChecks.Run();
            if (stage is "all" or "bidder") BidderChecks.Run();
            if (stage is "all" or "binding") BindingChecks.Run();
            if (stage == "note-host") BindingChecks.Run(noteHostsOnly: true);
            if (stage is "all" or "footer")
            {
                AuctionDisplayStateChecks.Run().GetAwaiter().GetResult();
                FooterRotationChecks.Run();
                FooterStatusChecks.Run();
                HistoryProgressUiChecks.Run();
            }
            if (stage is "all" or "stage4")
            {
                DisplayChecks.Run();
                UiChecks.Run();
                DisplayLifetimeChecks.Run();
            }
            if (stage is not ("all" or "stage1" or "stage2" or "stage3" or "stage4" or "history" or "farm-address" or "updates" or "updates-baseline" or "logging" or "log-archive" or "binding" or "entity" or "bidder" or "note-host" or "footer")) throw new ArgumentException("Unknown stage: " + stage);
            Console.WriteLine($"PASS: {_checks} offline reference adoption checks.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL: {ex}");
            return 1;
        }
    }

    internal static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        _checks++;
    }
}
