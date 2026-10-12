using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using GAMSTest.Models;
using GAMSTest.Services;
using GAMSTest.ViewModels;
using GAMSTest.Views;

internal static class NamedPipeChecks
{
    internal static void Run()
    {
        Program.Check(NamedPipeCommandServer.PipeName == "GAMSTest.DisplayCommand", "external controller pipe name remains compatible");
        string pipeName = "GAMSTest.Checks." + Guid.NewGuid().ToString("N");
        var received = new ConcurrentQueue<string>();
        var server = new NamedPipeCommandServer(received.Enqueue, pipeName);
        try
        {
            Send(pipeName, "  NUMBER  ");
            Until(() => received.Count == 1);
            Program.Check(received.TryDequeue(out var command) && command == "NUMBER", "pipe reads and trims one line");
            Send(pipeName, "\t");
            Send(pipeName, "BLUE");
            Until(() => received.Count == 1);
            Program.Check(received.TryDequeue(out command) && command == "BLUE", "blank command ignored and next connection accepted");
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous))
            {
                client.Connect(5000);
            }
            Send(pipeName, "RED");
            Until(() => received.Count == 1);
            Program.Check(received.TryDequeue(out command) && command == "RED", "disconnected client does not stop listening");
        }
        finally { server.Dispose(); }
        Until(() => server.Completion.IsCompleted);
        server.Completion.GetAwaiter().GetResult();
        server.Dispose();
        Program.Check(server.Completion.IsCompletedSuccessfully, "idle pipe cancellation and repeated dispose complete cleanly");

        pipeName = "GAMSTest.Checks." + Guid.NewGuid().ToString("N");
        int launches = 0;
        var window = new ControlWindow(new TestSettings { Rows = [[7, 8]] }, () => launches++, pipeName);
        var vm = (ControlWindowViewModel)window.DataContext;
        var display = (DisplayWindow)Field(window, "_display")!;
        var controller = (DisplayTestController)Field(vm, "_controller")!;
        var commands = (NamedPipeCommandServer)Field(window, "_commandServer")!;
        bool controlClosed = false, displayClosed = false;
        window.Closed += (_, _) => controlClosed = true;
        display.Closed += (_, _) => displayClosed = true;
        var slots = display.Surface.Children.Cast<Border>().ToArray();
        try
        {
            foreach (var item in new[] { (Command: "red", Color: Colors.Red), (Command: " GREEN ", Color: Colors.Green), (Command: "Blue", Color: Colors.Blue) })
            {
                vm.StartCycleCommand.Execute(null);
                Send(pipeName, item.Command);
                Color expected = Color.FromRgb((byte)(item.Color.R * 170 / 255), (byte)(item.Color.G * 170 / 255), (byte)(item.Color.B * 170 / 255));
                Until(() => slots.All(slot => slot.Child == null && ((SolidColorBrush)slot.Background).Color == expected));
                Program.Check(!controller.IsCycling && slots.All(slot => slot.Child == null && ((SolidColorBrush)slot.Background).Color == expected), "external RGB stops cycle and fills every slot at current brightness: " + item.Command);
            }
            vm.Brightness = 255;
            Program.Check(slots.All(slot => ((SolidColorBrush)slot.Background).Color == Colors.Blue), "external color updates brightness slider's remembered color");
            Send(pipeName, "number");
            Until(() => slots.All(slot => slot.Child is BoardNumberView));
            Program.Check(slots.Select(slot => ((BoardNumberView)slot.Child).Number).SequenceEqual(new[] { "7", "8" }), "external NUMBER restores configured board numbers");
            Send(pipeName, "UNKNOWN");
            Send(pipeName, "RED");
            Until(() => slots.All(slot => slot.Child == null && ((SolidColorBrush)slot.Background).Color == Colors.Red));
            Program.Check(!controlClosed && launches == 0, "unknown command ignored and external colors do not launch operational app");
            vm.StartCycleCommand.Execute(null);
            Send(pipeName, "sToP");
            Until(() => controlClosed && displayClosed && commands.Completion.IsCompleted);
            commands.Completion.GetAwaiter().GetResult();
            Program.Check(controller.IsDisposed && !controller.IsCycling && controller.SlotCount == 0 && launches == 0,
                "external STOP closes both windows, releases cycle and pipe, without launching operational app");
        }
        finally { if (!controlClosed) window.Close(); Program.Drain(); }

        pipeName = "GAMSTest.Checks." + Guid.NewGuid().ToString("N");
        server = new NamedPipeCommandServer(received.Enqueue, pipeName);
        using var idleClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
        idleClient.Connect(5000);
        server.Dispose();
        Until(() => server.Completion.IsCompleted);
        server.Completion.GetAwaiter().GetResult();
        Program.Check(server.Completion.IsCompletedSuccessfully, "pipe shutdown cancels a connected client without a newline");
    }

    private static object? Field(object target, string name) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(target);

    private static void Send(string pipeName, string command)
    {
        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
        client.Connect(5000);
        using var writer = new StreamWriter(client) { AutoFlush = true };
        writer.WriteLine(command);
    }

    private static void Until(Func<bool> ready)
    {
        var watch = Stopwatch.StartNew();
        while (!ready())
        {
            if (watch.Elapsed > TimeSpan.FromSeconds(5)) throw new TimeoutException("Isolated named pipe check timed out.");
            Program.Drain();
            Thread.Sleep(1);
        }
    }
}
