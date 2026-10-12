using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Presentation.Display;
using System.Globalization;

namespace CowAuctionSmall.ViewModels
{
    public enum SexPresentationMode
    {
        Plain,
        Blood,
        Matched
    }

    /// <summary>
    /// 대기·진행·낙찰·유찰에서 공유하는 화면용 스냅샷과 표시 값.
    /// 입력이 바뀌면 소유 ViewModel 또는 View의 DataContext에서 이 객체를 교체한다.
    /// </summary>
    public sealed class AuctionItemDisplayViewModel
    {
        public gValues Snapshot { get; }
        public string LowestPriceText { get; }
        public string BidPriceText { get; }
        public string HistoryOwnerNameText => string.IsNullOrWhiteSpace(Snapshot.HistoryOwner?.Name) ? "정보 없음" : Snapshot.HistoryOwner.Name;
        public string HistoryFarmAddressText => AuctionDisplayFormatter.FarmAddress(Snapshot.HistoryOwner?.FarmAddress);
        // 기존 해남진도 화면의 '결장 → -' 표시만 별도로 보존한다.
        public string LowestPriceOrDashText { get; }
        public string WeightText { get; }
        public string FormattedWeightText { get; }
        public string WeightWithUnitText { get; }
        public bool ShowPregnancy { get; }
        public string SexStyleKey { get; }
        public string SexText { get; }
        public bool IsThreeCharacterSex => SexText.Length == 3;
        public SexPresentationMode SexPresentationMode { get; }
        public string OutlinedSexText { get; }
        public bool IsThreeCharacterOutlinedSex => OutlinedSexText.Length == 3;
        public bool IsSingleCharacterOutlinedSex => OutlinedSexText.Length == 1;

        public AuctionItemDisplayViewModel(gValues source)
        {
            Snapshot = source.Clone();
            var culture = CultureInfo.CurrentCulture;
            LowestPriceText = AuctionDisplayFormatter.Number(Snapshot.LowestPrice, culture);
            BidPriceText = AuctionDisplayFormatter.Number(Snapshot.BidPrice, culture);
            LowestPriceOrDashText = Snapshot.LowestPrice == "결장" ? "-" : LowestPriceText;
            WeightText = Snapshot.Weight ?? string.Empty;
            FormattedWeightText = AuctionDisplayFormatter.Number(Snapshot.Weight, culture);
            WeightWithUnitText = AuctionDisplayFormatter.WeightWithUnit(Snapshot.Weight);
            ShowPregnancy = Snapshot.Pregnant == "X" || Snapshot.Pregnant == "미정" ||
                (int.TryParse(Snapshot.Pregnant, NumberStyles.Integer, culture, out var months) && months > 0);
            SexStyleKey = Snapshot.Sex ?? string.Empty;
            SexText = SexStyleKey == "프리마틴" ? "프리" : SexStyleKey;
            SexPresentationMode = Snapshot.RegistrationCategory switch
            {
                "혈통" => SexPresentationMode.Blood,
                "일치" => SexPresentationMode.Matched,
                _ => SexPresentationMode.Plain
            };
            OutlinedSexText = SexPresentationMode == SexPresentationMode.Plain && SexStyleKey.Length > 2 &&
                (SexStyleKey.Contains("정보없음") || SexStyleKey.Contains("일치")) ? string.Empty : SexText;
        }
    }
}
