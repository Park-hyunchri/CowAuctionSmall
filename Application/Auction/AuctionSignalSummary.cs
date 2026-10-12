using System;
using System.Globalization;

namespace CowAuctionSmall.Application.Auction
{
    /// <summary>고정 코드와 허용 상태만 사용한다. 식별값과 원문은 반환하지 않는다.</summary>
    public static class AuctionSignalSummary
    {
        public static bool DiagnosticsEnabled { get; } = Environment.GetEnvironmentVariable("COWAUCTION_DIAGNOSTICS") == "1";
        public static string Diagnostic(string[] fields)
        {
            string code = fields.Length > 0 ? fields[0] : string.Empty;
            string? value = code switch
            {
                "AT" => Allowed(fields, 2, "10", "20"),
                "AS" => Allowed(fields, 6, "8001", "8002", "8003", "8004", "8005", "8006", "8007"),
                "AF" => Allowed(fields, 3, "22", "23", "24"),
                "SZ" => Allowed(fields, 5, "P", "Y", "N", "A", "H"),
                "SD" => Allowed(fields, 2, "C", "F"),
                _ => null
            };
            if (code is not ("AT" or "AS" or "AF" or "SZ" or "SD" or "AR" or "SS" or "SV")) return "OTHER";
            return value == null ? code : code + " state=" + value;
        }

        public static string? StatusText(string[] fields)
        {
            string summary = Diagnostic(fields);
            if (summary == "SD state=C")
                return fields.Length > 3 && int.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out int count)
                    && count >= 0 && count <= 999 ? $"카운트다운 신호: {count}" : null;
            return summary switch
            {
                "AT state=10" => "일괄경매 방식 수신",
                "AT state=20" => "단일경매 방식 수신",
                "AS state=8001" => "새로고침 요청 수신",
                "AS state=8002" => "경매 대기 신호 수신",
                "AS state=8003" => "시작 준비·가격 변경 신호 수신",
                "AS state=8004" => "경매 진행 신호 수신",
                "AS state=8005" => "경매 상태 변경 신호 수신",
                "AS state=8006" => "진행 종료 신호 수신 · 결과 확인 중",
                "AS state=8007" => "회차 종료 신호 수신",
                "AF state=22" => "낙찰 결과 수신",
                "AF state=23" => "유찰 결과 수신",
                "AF state=24" => "경매 취소 신호 수신",
                "SD state=F" => "정지·종료 신호 수신",
                "SZ state=P" => "경매대상 표시 요청 수신",
                "SZ state=Y" => "미응찰 표시 요청 수신",
                "SZ state=N" => "경매결과 표시 요청 수신",
                "SZ state=A" => "기본영상 표시 요청 수신",
                "SZ state=H" => "시작·재개 표시 요청 수신",
                _ => null
            };
        }

        private static string? Allowed(string[] fields, int index, params string[] allowed)
        {
            if (fields.Length <= index) return null;
            foreach (string value in allowed)
                if (string.Equals(fields[index], value, StringComparison.OrdinalIgnoreCase)) return value;
            return null;
        }
    }
}
