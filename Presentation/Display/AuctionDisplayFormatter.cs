using System.Globalization;

namespace CowAuctionSmall.Presentation.Display
{
    internal static class AuctionDisplayFormatter
    {
        internal static string Number(string? value, CultureInfo culture)
        {
            return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, culture, out var number)
                ? number.ToString("#,##0", culture)
                : value ?? string.Empty;
        }

        internal static string WeightWithUnit(string? value)
        {
            var weight = value?.Trim();
            return string.IsNullOrEmpty(weight) || weight == "0" || weight == "-" ? "-" : weight + "kg";
        }

        internal static string FarmAddress(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "정보 없음";
            string address = value.Trim();
            int separator = 0;
            while (separator < address.Length && !char.IsWhiteSpace(address[separator])) separator++;
            string region = address.Substring(0, separator);
            bool isCityOrProvince = region is "경기도" or "경기" or "강원도" or "강원특별자치도" or "강원"
                or "충청북도" or "충북" or "충청남도" or "충남"
                or "전라북도" or "전북특별자치도" or "전북" or "전라남도" or "전남"
                or "경상북도" or "경북" or "경상남도" or "경남"
                or "제주도" or "제주특별자치도" or "제주"
                or "서울특별시" or "서울시" or "서울" or "부산광역시" or "부산시" or "부산"
                or "대구광역시" or "대구시" or "대구" or "인천광역시" or "인천시" or "인천"
                or "광주광역시" or "광주시" or "광주" or "대전광역시" or "대전시" or "대전"
                or "울산광역시" or "울산시" or "울산" or "세종특별자치시" or "세종시" or "세종";
            isCityOrProvince = isCityOrProvince
                || region.EndsWith("특별시", System.StringComparison.Ordinal)
                || region.EndsWith("광역시", System.StringComparison.Ordinal)
                || region.EndsWith("특별자치시", System.StringComparison.Ordinal)
                || region.EndsWith("특별자치도", System.StringComparison.Ordinal);
            // Keep region-only responses rather than removing their only location.
            return isCityOrProvince && separator < address.Length ? address.Substring(separator).TrimStart() : address;
        }
    }
}
