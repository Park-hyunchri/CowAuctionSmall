using CowAuctionSmall.Views.SIze_160_64;
using CowAuctionSmall.Views.Size_320_64;
using CowAuctionSmall.Views.Size_320_64.Running;
using CowAuctionSmall.Views.Size128_128.Running;
using CowAuctionSmall.Views.Size128_64;
using CowAuctionSmall.Views.Size128_64.Running;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace CowAuctionSmall.Presentation.Display
{
    /// <summary>통신이나 운영 설정 조회 없이 화면을 선택하기 위한 입력값.</summary>
    public sealed record AuctionViewOptions
    {
        public DisplaySizeParser.DisplaySize Size { get; init; } = DisplaySizeParser.DisplaySize.Size128x128;
        public string NhCode { get; init; } = string.Empty;
        public string IsQquri { get; init; } = "N";
        public string CowDistinction { get; init; } = "1";
        public string IsNhExcellent { get; init; } = "N";
        public string IsMotherNhExcellent { get; init; } = "N";
        public string IsShowQquri { get; init; } = "N";
        public string LowestPriceTitle { get; init; } = string.Empty;
        public string AbilityNumber { get; init; } = string.Empty;
    }

    /// <summary>운영 화면과 점검 도구가 공유하는 순수 화면 생성 경로. WPF UI 스레드에서 사용한다.</summary>
    public sealed class AuctionViewFactory
    {
        private readonly SetCustomDisplay _customDisplay;

        public AuctionViewFactory(SetCustomDisplay? customDisplay = null)
        {
            _customDisplay = customDisplay ?? new SetCustomDisplay();
        }

        public UserControl? CreateRunning(AuctionViewOptions options, int pageNumber)
        {
            return options.Size switch
            {
                DisplaySizeParser.DisplaySize.Size128x64 => CreateRunning64(options, pageNumber),
                DisplaySizeParser.DisplaySize.Size128x128 => pageNumber switch
                {
                    1 => _customDisplay.CustomAuctionRunning1_128(options.NhCode, options.IsQquri, options.CowDistinction, options.IsNhExcellent, options.IsMotherNhExcellent),
                    2 => _customDisplay.CustomAuctionRunning2_128(options.NhCode, options.IsQquri, options.CowDistinction, options.IsNhExcellent, options.IsMotherNhExcellent),
                    3 => _customDisplay.CustomAuctionRunning3_128(options.NhCode),
                    _ => null
                },
                DisplaySizeParser.DisplaySize.Size160x64 => pageNumber switch
                {
                    1 => _customDisplay.CustomAuctionRunning1_160_64(options.NhCode, options.IsQquri, options.CowDistinction),
                    2 => _customDisplay.CustomAuctionRunning2_160_64(options.NhCode, options.IsQquri, options.CowDistinction),
                    _ => null
                },
                DisplaySizeParser.DisplaySize.Size320x64 => pageNumber switch
                {
                    1 => _customDisplay.CustomAuctionRunning1_320_64(options.NhCode, options.IsQquri, options.CowDistinction),
                    2 => _customDisplay.CustomAuctionRunning2_320_64(options.NhCode, options.IsQquri, options.CowDistinction),
                    _ => null
                },
                _ => throw new ArgumentException("지원하지 않는 전광판 해상도입니다.", nameof(options))
            };
        }

        private static UserControl? CreateRunning64(AuctionViewOptions options, int pageNumber)
        {
            if (options.NhCode == "8808990806426")
            {
                UserControl? sokchoPage = IsSokchoEvent(options)
                    ? pageNumber == 1 ? new SokchoEventRun_64() : null
                    : pageNumber switch { 1 => new SokchoRunning1_64(), 2 => new SokchoRunning2_64(), _ => null };
                if (sokchoPage != null) sokchoPage.Name = IsSokchoEvent(options)
                    ? $"SokchoEventRunPage{pageNumber}" : $"SokchoRunPage{pageNumber}";
                return sokchoPage;
            }
            int viewNumber = options.NhCode == "0002"
                ? pageNumber switch { 2 => 3, 3 => 4, 4 => 2, _ => pageNumber }
                : pageNumber;
            UserControl? page = viewNumber switch
            {
                1 => new AuctionRunning1_64(),
                2 => new AuctionRunning2_64(),
                3 => new AuctionRunning3_64(),
                4 => new AuctionRunning4_64(),
                _ => null
            };
            if (page != null) page.Name = $"RunPage{pageNumber}_64";
            return page;
        }

        public UserControl CreateSold(AuctionViewOptions options)
        {
            return options.Size switch
            {
                DisplaySizeParser.DisplaySize.Size128x128 => _customDisplay.CustomAuctionSold_128(options.NhCode, options.IsShowQquri, options.CowDistinction, options.LowestPriceTitle),
                DisplaySizeParser.DisplaySize.Size128x64 when options.NhCode == "8808990806426" =>
                    IsSokchoEvent(options) ? new SokchoEventResult_64() : new SokchoSold_64(),
                DisplaySizeParser.DisplaySize.Size128x64 => new AuctionSold_64(),
                DisplaySizeParser.DisplaySize.Size160x64 => new AuctionSold_160_64(),
                DisplaySizeParser.DisplaySize.Size320x64 => new AuctionSold_320_64(),
                _ => throw new ArgumentException("지원하지 않는 전광판 해상도입니다.", nameof(options))
            };
        }

        public UserControl CreateUnSold(AuctionViewOptions options)
        {
            return options.Size switch
            {
                DisplaySizeParser.DisplaySize.Size128x128 => _customDisplay.CustomAuctionUnSold_128(options.NhCode, options.IsQquri, options.CowDistinction, options.AbilityNumber),
                DisplaySizeParser.DisplaySize.Size128x64 when options.NhCode == "8808990806426" =>
                    IsSokchoEvent(options) ? new SokchoEventResult_64() : new SokchoUnSold_64(),
                DisplaySizeParser.DisplaySize.Size128x64 => new AuctionUnSold_64(),
                DisplaySizeParser.DisplaySize.Size160x64 => new AuctionUnSold_160_64(),
                DisplaySizeParser.DisplaySize.Size320x64 => new AuctionUnSold_320_64(),
                _ => throw new ArgumentException("지원하지 않는 전광판 해상도입니다.", nameof(options))
            };
        }

        /// <summary>운영에 존재하는 페이지 번호. 미구현 틀도 포함하며 점검 시 IsPlaceholder로 구분한다.</summary>
        public static IReadOnlyList<int> GetRunningPageNumbers(AuctionViewOptions options)
        {
            return options.Size switch
            {
                DisplaySizeParser.DisplaySize.Size128x64 when options.NhCode == "8808990806426" =>
                    IsSokchoEvent(options) ? new[] { 1 } : new[] { 1, 2 },
                DisplaySizeParser.DisplaySize.Size128x64 => new[] { 1, 2, 3, 4 },
                DisplaySizeParser.DisplaySize.Size128x128 when SetCustomDisplay.HasRunning3(options.NhCode) => new[] { 1, 2, 3 },
                DisplaySizeParser.DisplaySize.Size128x128 or DisplaySizeParser.DisplaySize.Size160x64 or DisplaySizeParser.DisplaySize.Size320x64 => new[] { 1, 2 },
                _ => throw new ArgumentException("지원하지 않는 전광판 해상도입니다.", nameof(options))
            };
        }

        public static bool IsPlaceholder(UserControl page) =>
            page is AuctionRunning2_320_64 or AuctionSold_320_64 or AuctionUnSold_320_64;

        public static bool IsSokchoEvent(AuctionViewOptions options) =>
            options.Size == DisplaySizeParser.DisplaySize.Size128x64 && options.NhCode == "8808990806426" &&
            string.Equals(options.LowestPriceTitle?.Trim(), "행사용", StringComparison.Ordinal);

        public static int LimitRunningPageCount(AuctionViewOptions options, int requestedPages)
        {
            int maximum = options.NhCode == "8808990806426" &&
                options.Size is DisplaySizeParser.DisplaySize.Size128x64 or DisplaySizeParser.DisplaySize.Size128x128
                ? IsSokchoEvent(options) ? 1 : 2 : 4;
            return Math.Clamp(requestedPages, 1, maximum);
        }

        public static bool ShouldUseNoteHost(AuctionViewOptions options, UserControl? page = null) =>
            options.Size == DisplaySizeParser.DisplaySize.Size128x128 &&
            options.CowDistinction != "5" && options.CowDistinction != "6" &&
            page is not AuctionRunning2_3 && page is not AuctionRunning4;
    }
}
