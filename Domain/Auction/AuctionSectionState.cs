using System;
using System.Globalization;

namespace CowAuctionSmall.Domain.Auction
{
    public sealed record AuctionSectionRange(int StartNumber, int EndNumber);

    /// <summary>표시 필터와 분리된 수신 구간 정보. AS에는 날짜와 번호 범위가 없다.</summary>
    public sealed record AuctionSectionState
    {
        public string Source { get; private init; } = string.Empty;
        public string HouseCode { get; private init; } = string.Empty;
        public DateOnly? AuctionDate { get; private init; }
        public int? CowType { get; private init; }
        public int? SectionNumber { get; private init; }
        public string? DisplayCommand { get; private init; }
        public AuctionSectionRange? Range { get; private init; }
        public bool IsValid { get; private init; }
        public bool IsRangeConfirmed => IsValid && Range != null;

        public static AuctionSectionState Empty { get; } = new();

        public AuctionSectionState ConfirmRange(DateOnly date, AuctionSectionRange range)
        {
            if (!IsValid || range.StartNumber < 1 || range.EndNumber < range.StartNumber)
                throw new ArgumentException("Invalid auction section range.", nameof(range));
            return this with { AuctionDate = date, Range = range };
        }

        public static AuctionSectionState FromSignal(string[] fields)
        {
            string source = fields.Length > 0 ? fields[0] : string.Empty;
            var invalid = new AuctionSectionState { Source = source is "AS" or "SZ" ? source : string.Empty };
            if (fields.Length < 2 || !IsDigits(fields[1])) return invalid;

            if (source == "AS")
            {
                // 끝에 구분자가 붙어도 위치가 바뀌지 않도록 고정 인덱스를 사용한다.
                if (fields.Length < 14 || !TryNumber(fields[6], out int status) || status < 8001 || status > 8007 ||
                    !TryNumber(fields[12], out int section) || !TryCowType(fields[13], out int cowType)) return invalid;
                return new AuctionSectionState { Source = source, HouseCode = fields[1], CowType = cowType,
                    SectionNumber = section, IsValid = true };
            }

            if (source != "SZ" || fields.Length < 8 ||
                !DateOnly.TryParseExact(fields[2], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
                !TryCowType(fields[3], out int type) || !TryNumber(fields[4], out int number) ||
                !TryNumber(fields[6], out int start) || !TryNumber(fields[7], out int end) || start < 1 || end < start)
                return invalid;
            string command = fields[5]?.ToUpperInvariant() ?? string.Empty;
            if (command is not ("A" or "P" or "Y" or "N" or "H")) return invalid;
            return new AuctionSectionState { Source = source, HouseCode = fields[1], AuctionDate = date,
                CowType = type, SectionNumber = number, DisplayCommand = command,
                Range = new AuctionSectionRange(start, end), IsValid = true };
        }

        private static bool TryCowType(string? text, out int value) => TryNumber(text, out value) && value <= 3;

        private static bool TryNumber(string? text, out int value)
        {
            value = 0;
            return IsDigits(text) && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }

        private static bool IsDigits(string? text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char character in text)
                if (character < '0' || character > '9') return false;
            return true;
        }
    }
}
