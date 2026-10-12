using CowAuctionSmall.Presentation.Assets;
using CowAuctionSmall.Domain.Settings;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static class LogoChecks
{
    internal static void Run()
    {
        using var fixture = new LogoFixture();
        foreach (string extension in new[] { "bmp", "png", "jpg", "jpeg", "PNG" })
        {
            string path = fixture.WriteImage("static." + extension);
            var first = ImageCacheHelper.CreateLogoImage(path);
            var second = ImageCacheHelper.CreateLogoImage(path);
            Program.Check(first.Source is BitmapSource, "static image decodes: " + extension);
            Program.Check(ReferenceEquals(first.Source, second.Source), "static bitmap is shared: " + extension);
            Program.Check(first.Source.IsFrozen, "static bitmap is frozen: " + extension);
        }

        string selected = fixture.WriteImage("selected.png");
        fixture.WriteGif("selected.gif");
        Program.Check(fixture.Manager("selected.png").GetLogoPath("panel_7") == selected,
            "same-name GIF never takes precedence over static logo");
        Program.Check(fixture.Manager("selected.GIF").GetLogoPath("panel_7") == fixture.DefaultPath,
            "explicit uppercase GIF falls back to default logo");
        Program.Check(fixture.Manager("missing.png").GetLogoPath("panel_7") == fixture.DefaultPath,
            "missing configured logo falls back to default");
        foreach (string name in new[] { "direct.gif", "direct.GIF", "disguised.png", "disguised.bmp" })
        {
            string path = fixture.WriteGif(name);
            Program.Check(ImageCacheHelper.CreateLogoImage(path, fixture.DefaultPath).Source is BitmapSource,
                "unsupported GIF uses fallback: " + name);
            Program.Check(ImageCacheHelper.CreateLogoImage(path, Path.Combine(fixture.ConfigPath, "absent.bmp")).Source == null,
                "GIF content never displays even when disguised: " + name);
        }
        string broken = Path.Combine(fixture.ConfigPath, "broken.png");
        File.WriteAllText(broken, "invalid image");
        foreach (string path in new[] { broken, Path.Combine(fixture.ConfigPath, "missing.png") })
        {
            Program.Check(ImageCacheHelper.CreateLogoImage(path, fixture.DefaultPath).Source is BitmapSource,
                "broken or missing image falls back");
            Program.Check(ImageCacheHelper.CreateLogoImage(path, broken).Source == null,
                "broken default leaves empty image without throwing");
        }
        foreach (string extension in new[] { ".gif", ".GIF", ".webp", ".txt" })
            Program.Check(!ImageCacheHelper.IsSupportedExtension("logo" + extension), "unsupported extension: " + extension);
        ImageCacheHelper.ClearCache();
    }

    internal sealed class LogoFixture : IDisposable
    {
        internal string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "ReferenceAdoptionChecks_" + Guid.NewGuid().ToString("N"));
        internal string ConfigPath => Path.Combine(DirectoryPath, "Config");
        internal string DefaultPath => Path.Combine(ConfigPath, "logo.bmp");

        internal LogoFixture()
        {
            Directory.CreateDirectory(ConfigPath);
            WriteImage("logo.bmp");
        }

        internal string WriteImage(string name)
        {
            BitmapEncoder encoder = Path.GetExtension(name).ToLowerInvariant() switch
            {
                ".bmp" => new BmpBitmapEncoder(),
                ".png" => new PngBitmapEncoder(),
                ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
                _ => throw new ArgumentException("Unsupported fixture extension")
            };
            var pixels = new byte[] { 20, 120, 240, 255 };
            var source = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, pixels, 4);
            encoder.Frames.Add(BitmapFrame.Create(source));
            string path = Path.Combine(ConfigPath, name);
            using var stream = File.Create(path);
            encoder.Save(stream);
            return path;
        }

        internal string WriteGif(string name)
        {
            string path = Path.Combine(ConfigPath, name);
            // Valid one-pixel GIF is solely an unsupported-format rejection fixture.
            File.WriteAllBytes(path, Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"));
            return path;
        }

        internal LogoManager Manager(string fileName) => new(new BoardList
        {
            LogoBoard = new List<Logos>
            {
                new() { Rows = new List<LogoRowIdx> { new() { ID = fileName, Rows = new List<int> { 7 } } } }
            }
        }, DirectoryPath);

        public void Dispose()
        {
            ImageCacheHelper.ClearCache();
            string root = Path.GetFullPath(Path.GetTempPath());
            string target = Path.GetFullPath(DirectoryPath);
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(target).StartsWith("ReferenceAdoptionChecks_", StringComparison.Ordinal))
                throw new InvalidOperationException("Fixture path escaped temporary directory.");
            Directory.Delete(target, true);
        }
    }
}
