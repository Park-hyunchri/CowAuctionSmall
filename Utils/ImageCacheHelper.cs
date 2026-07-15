using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
namespace CowAuctionSmall.Utils
{
    public static class ImageCacheHelper
    {
        private static readonly Dictionary<string, BitmapImage> _cache = new();

        /// <summary>
        /// 로고 경로를 받아 Image 컨트롤 생성 (Stretch.Fill, 캐싱 포함)
        /// </summary>
        public static Image CreateLogoImage(string imagePath)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("이미지 파일이 존재하지 않습니다", imagePath);

            var image = new Image
            {
                Stretch = Stretch.Fill,
                CacheMode = new BitmapCache()
            };

            var bitmap = GetOrLoad(imagePath);

            if (Path.GetExtension(imagePath).Equals(".gif", StringComparison.OrdinalIgnoreCase))
            {
                ImageBehavior.SetAnimatedSource(image, bitmap);
            }
            else
            {
                image.Source = bitmap;
            }

            return image;
        }

        /// <summary>
        /// 이미지 캐시에서 로드하거나 새로 생성
        /// </summary>
        private static BitmapImage GetOrLoad(string path)
        {
            if (_cache.TryGetValue(path, out var cached))
                return cached;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.EndInit();
            bitmap.Freeze();

            _cache[path] = bitmap;
            return bitmap;
        }

        /// <summary>
        /// 전체 캐시 초기화
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }
    }
}
