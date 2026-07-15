using CowAuctionSmall.Models.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CowAuctionSmall.Models
{

     public class LogoManager
     {
         private readonly string _baseDirectory;
         private readonly BoardList _boardInfo;
         private readonly Dictionary<string, string> _logoPathCache; // 캐시 저장소

         public LogoManager(BoardList boardInfo, string? baseDirectory = null)
         {
             _boardInfo = boardInfo;
             _baseDirectory = baseDirectory ?? Environment.CurrentDirectory;
             _logoPathCache = new Dictionary<string, string>();
         }

         public string GetLogoPath(string panelName)
         {
             // 캐시 확인
             if (_logoPathCache.TryGetValue(panelName, out var cachedPath))
             {
                 return cachedPath;
             }

             // 캐시에 없으면 계산
             if (string.IsNullOrEmpty(panelName) || !panelName.Contains("_"))
             {
                 return CacheAndReturn(panelName, DefaultLogoPath());
             }

             string? panelId = panelName.Split('_').ElementAtOrDefault(1);
             if (string.IsNullOrEmpty(panelId) || !int.TryParse(panelId, out int id))
             {
                 return CacheAndReturn(panelName, DefaultLogoPath());
             }

              string logoFileName = GetLogoFileName(id);
              string fullPath = Path.Combine(_baseDirectory, "Config", logoFileName);
              if (!File.Exists(fullPath))
              {
                  fullPath = DefaultLogoPath();
              }
              return CacheAndReturn(panelName, fullPath);
          }

          public string GetDefaultLogoPath()
          {
              return DefaultLogoPath();
          }

         private string CacheAndReturn(string key, string value)
         {
             _logoPathCache[key] = value;
             return value;
         }

         private string DefaultLogoPath()
         {
             return Path.Combine(_baseDirectory, "Config", "logo.bmp");
         }

         private string GetLogoFileName(int panelId)
         {
             var logoRows = _boardInfo?.LogoBoard?[0]?.Rows ?? new List<LogoRowIdx>();
             var matchingRow = logoRows.FirstOrDefault(row => row.Rows?.Contains(panelId) == true);

             string fileName = matchingRow?.ID ?? "logo.bmp";

             // GIF가 있으면 우선적으로 반환
             string gifPath = Path.Combine(_baseDirectory, "Config", Path.ChangeExtension(fileName, ".gif"));
             if (File.Exists(gifPath)) return Path.GetFileName(gifPath);

             return fileName;
         }

     }

}
