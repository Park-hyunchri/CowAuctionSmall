using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace GAMSTest.Services;

public static class AuctionApplicationLauncher
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool ShowWindowAsync(IntPtr window, int command);

    public static bool ActivateExisting()
    {
        foreach (var process in Process.GetProcessesByName("CowAuctionSmall"))
        {
            using (process)
            {
                if (process.MainWindowHandle == IntPtr.Zero) continue;
                ShowWindowAsync(process.MainWindowHandle, 9);
                SetForegroundWindow(process.MainWindowHandle);
                return true;
            }
        }
        return false;
    }

    public static string? FindExecutable(string baseDirectory, string workingDirectory)
    {
        var candidates = new List<string> { Path.Combine(baseDirectory, "CowAuctionSmall.exe"), Path.Combine(workingDirectory, "CowAuctionSmall.exe") };
        // Adjacent deployment folder and the standard isolated build layout.
        var parent = Directory.GetParent(baseDirectory.TrimEnd(Path.DirectorySeparatorChar));
        if (parent != null)
            candidates.Add(Path.Combine(parent.FullName, "CowAuctionSmall", "CowAuctionSmall.exe"));
        if (parent?.Name == "GAMSTest" && parent.Parent != null)
            candidates.Add(Path.Combine(parent.Parent.FullName, "CowAuctionSmall", new DirectoryInfo(baseDirectory).Name, "CowAuctionSmall.exe"));
        foreach (string start in new[] { baseDirectory, workingDirectory })
        for (var directory = new DirectoryInfo(start); directory != null; directory = directory.Parent)
        {
            if (!File.Exists(Path.Combine(directory.FullName, "CowAuctionSmall.csproj"))) continue;
            foreach (string configuration in new[] { "Release", "Debug" })
                candidates.Add(Path.Combine(directory.FullName, "bin", configuration, "net10.0-windows", "CowAuctionSmall.exe"));
            break;
        }
        return candidates.FirstOrDefault(File.Exists);
    }

    public static void Launch(string executable)
    {
        string path = Path.GetFullPath(executable);
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = path, WorkingDirectory = Path.GetDirectoryName(path)!, UseShellExecute = true
        });
    }
}
