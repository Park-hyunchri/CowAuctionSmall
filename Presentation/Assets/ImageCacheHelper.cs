using CowAuctionSmall.Infra.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace CowAuctionSmall.Presentation.Assets
{
    public static class ImageCacheHelper
    {
        private static readonly Dictionary<string, BitmapSource?> _cache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _sync = new();

        /// <summary>
        /// 로고 경로를 받아 Image 컨트롤 생성 (Stretch.Fill, 캐싱 포함)
        /// </summary>
        public static Image CreateLogoImage(string imagePath, string? fallbackPath = null)
        {
            var image = new Image
            {
                Stretch = Stretch.Fill,
                CacheMode = new BitmapCache()
            };

            fallbackPath ??= Path.Combine(Path.GetDirectoryName(imagePath) ?? AppContext.BaseDirectory, "logo.bmp");
            image.Source = GetOrLoad(imagePath) ?? GetOrLoad(fallbackPath);

            return image;
        }

        /// <summary>
        /// 이미지 캐시에서 로드하거나 새로 생성
        /// </summary>
        public static bool IsSupportedExtension(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() is ".bmp" or ".png" or ".jpg" or ".jpeg";
        }

        private static BitmapSource? GetOrLoad(string path)
        {
            lock (_sync)
            {
                if (_cache.TryGetValue(path, out var cached))
                    return cached;
                BitmapSource? bitmap = null;
                try
                {
                    if (!IsSupportedExtension(path))
                        throw new NotSupportedException();
                    using var stream = File.OpenRead(path);
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                    if (decoder is not BmpBitmapDecoder and not PngBitmapDecoder and not JpegBitmapDecoder)
                        throw new NotSupportedException();
                    bitmap = decoder.Frames[0];
                    bitmap.Freeze();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.IO.FileFormatException or ArgumentException)
                {
                    NLogger.Instance.LogWarn("정적 로고를 읽을 수 없어 기본 로고 또는 빈 영역을 표시합니다.");
                }
                _cache[path] = bitmap;
                return bitmap;
            }
        }

        /// <summary>
        /// 전체 캐시 초기화
        /// </summary>
        public static void ClearCache()
        {
            lock (_sync) _cache.Clear();
        }
    }
}
