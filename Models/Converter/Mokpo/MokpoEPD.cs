using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter.Mokpo
{
    /// <summary>
    /// MultiValueConverter:
    /// values[0] -> SelectShowWeight_EPD (string, expected "Y" or "N")
    /// values[1] -> CowDistinction (int or convertible)
    ///
    /// ConverterParameter forms supported:
    ///  - "Group:5" or "Group:1,3" or "Group:2"           => Visible if cowDistinction in group
    ///  - "ShowWeight"                                     => default behavior: Select == "Y" || cowDistinction == 5
    ///  - "ShowWeight:5" or "ShowWeight:1,3"               => Select == "Y" || cowDistinction in provided group
    ///  - "ShowEPD"                                        => default behavior: Select == "N" && cowDistinction != 5
    ///  - "ShowEPD:1,3"                                    => Select == "N" && cowDistinction in provided group
    ///  - "5" or "1,3"                                     => same as "Group:..."
    /// </summary>
    public class MokpoEPD : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 안전한 기본값 처리
            if (values == null || values.Length < 2) return Visibility.Collapsed;

            // selectValue ("Y"/"N" 등)
            var selectValue = values[0]?.ToString()?.Trim();
            var selectUpper = selectValue?.ToUpperInvariant();

            // cowDistinction 안전 파싱 (IConvertible 등 모든 숫자형에 대해 시도)
            int cowDistinction = 0;
            if (values[1] is IConvertible conv)
            {
                var convText = conv.ToString();
                if (!string.IsNullOrWhiteSpace(convText) && int.TryParse(convText, out var parsed))
                {
                    cowDistinction = parsed;
                }
            }
            else if (int.TryParse(values[1]?.ToString(), out var parsedValue))
            {
                cowDistinction = parsedValue;
            }

            // parameter parsing
            var paramRaw = (parameter ?? string.Empty).ToString().Trim();
            if (string.IsNullOrEmpty(paramRaw))
                return Visibility.Collapsed;

            // 파라미터를 ":"로 분리 (예: "ShowEPD:1,3")
            string cmd = paramRaw;
            string? groupPart = null;
            var colonIndex = paramRaw.IndexOf(':');
            if (colonIndex >= 0)
            {
                cmd = paramRaw.Substring(0, colonIndex).Trim();
                groupPart = paramRaw.Substring(colonIndex + 1).Trim();
            }

            // 만약 파라미터가 숫자 목록만 ("1,3" 또는 "5") 이면 Group 모드로 간주
            if (IsNumericList(cmd) && string.IsNullOrEmpty(groupPart))
            {
                groupPart = cmd;
                cmd = "Group";
            }

            // 그룹 문자열을 정수 집합으로 파싱
            var groupSet = ParseGroupList(groupPart); // null 가능 (파싱 실패 또는 비어있음)

            switch (cmd.ToLowerInvariant())
            {
                case "group":
                    // groupSet 이 없으면 아무 표시 안함
                    if (groupSet == null || groupSet.Count == 0) return Visibility.Collapsed;
                    return groupSet.Contains(cowDistinction) ? Visibility.Visible : Visibility.Collapsed;

                case "showweight":
                    // 기본 group은 {5} (override 가능 via :...)
                    if (groupSet == null || groupSet.Count == 0) groupSet = new HashSet<int> { 5 };
                    if (string.Equals(selectUpper, "Y", StringComparison.OrdinalIgnoreCase)) return Visibility.Visible;
                    return groupSet.Contains(cowDistinction) ? Visibility.Visible : Visibility.Collapsed;

                case "showepd":
                    // 기본: Select == "N" && cowDistinction != 5
                    if (string.Equals(selectUpper, "N", StringComparison.OrdinalIgnoreCase))
                    {
                        if (groupSet == null || groupSet.Count == 0)
                        {
                            // 기본 동작: N 이고 5 가 아닌 경우 보여줌
                            return cowDistinction != 5 ? Visibility.Visible : Visibility.Collapsed;
                        }
                        else
                        {
                            // override: N 이고 group 에 포함되면 보여줌
                            return groupSet.Contains(cowDistinction) ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }
                    return Visibility.Collapsed;

                default:
                    // 알 수 없는 명령어면 Collapsed
                    return Visibility.Collapsed;
            }
        }

        // ConvertBack 미구현 (단방향)
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        // "1,3" 또는 "5" 같은 문자열인지 확인
        private static bool IsNumericList(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (!int.TryParse(p.Trim(), out _)) return false;
            }
            return true;
        }

        // "1,3" => HashSet{1,3}, "5" => {5}, null/"" => null
        private static HashSet<int>? ParseGroupList(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var set = new HashSet<int>();
            var parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out var n))
                    set.Add(n);
            }
            return set.Count > 0 ? set : null;
        }
    }
}
