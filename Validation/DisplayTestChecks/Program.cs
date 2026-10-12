using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;

internal static class Program
{
    private static int _checks;
    internal static string? ScreenshotsDirectory { get; private set; }

    [STAThread]
    private static int Main(string[] args)
    {
        bool measure = true;
        for (int index = 0; index < args.Length; index++)
        {
            if (args[index] == "--skip-measurements") measure = false;
            else if (args[index] == "--screenshots" && index + 1 < args.Length)
            {
                ScreenshotsDirectory = Path.GetFullPath(args[++index]);
                Directory.CreateDirectory(ScreenshotsDirectory);
            }
            else
            {
                Console.Error.WriteLine("Usage: DisplayTestChecks [--screenshots <directory>] [--skip-measurements]");
                return 2;
            }
        }
        NLog.LogManager.Configuration = new NLog.Config.LoggingConfiguration();
        var application = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/CowAuctionSmall;component/Views/Common/DisplayResources.xaml", UriKind.Relative)
        });
        try
        {
            FactoryChecks.Run();
            SokchoChecks.Run();
            ConfigurationChecks.Run();
            ControllerChecks.Run(measure);
            ReferenceUiChecks.Run();
            NamedPipeChecks.Run();
            Console.WriteLine($"PASS: {_checks} offline display test checks. Synthetic data; no product App or operational server.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL: {error}");
            return 1;
        }
        finally
        {
            application.Shutdown();
            NLog.LogManager.Shutdown();
        }
    }

    internal static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        _checks++;
    }

    internal static void Throws<T>(Action action, string label) where T : Exception
    {
        try { action(); }
        catch (T) { Check(true, label); return; }
        throw new InvalidOperationException(label);
    }

    internal static void Drain()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}
