using System;
using System.Windows;
using System.Windows.Media;
using CowAuctionSmall.Models;
using CowAuctionSmall.Models.XMLParser;

namespace CowAuctionSmall.Services
{
    public class DisplayColorResourceService
    {
        private readonly DisplayColorXmlParser _parser;
        private readonly NLogger _logger;

        public DisplayColorResourceService(DisplayColorXmlParser parser)
        {
            _parser = parser;
            _logger = NLogger.Instance;
        }

        public void ApplyFromXml(string xmlFilePath, ResourceDictionary resources)
        {
            var settings = _parser.ParseXml(xmlFilePath);

            ApplyBrush(resources, "EntityNumberForeground", settings.EntityNumberForeground);
            ApplyBrush(resources, "EntityNumberShortForeground", settings.EntityNumberShortForeground);
            ApplyBrush(resources, "EntityNumberShortBackground", settings.EntityNumberShortBackground);

            _logger.LogInfo($"DisplayColorResourceService: 색상 설정 적용 완료 ({xmlFilePath})");
        }

        private void ApplyBrush(ResourceDictionary resources, string resourceKey, string? colorValue)
        {
            if (string.IsNullOrWhiteSpace(colorValue))
            {
                return;
            }

            try
            {
                var converted = ColorConverter.ConvertFromString(colorValue.Trim());
                if (converted is not Color color)
                {
                    _logger.LogWarn($"DisplayColorResourceService: 색상 변환 실패 key={resourceKey}, value={colorValue}");
                    return;
                }

                var brush = new SolidColorBrush(color);
                if (brush.CanFreeze)
                {
                    brush.Freeze();
                }

                resources[resourceKey] = brush;
            }
            catch (Exception ex)
            {
                _logger.LogWarn($"DisplayColorResourceService: 유효하지 않은 색상입니다. key={resourceKey}, value={colorValue}, err={ex.Message}");
            }
        }
    }
}
