using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CowAuctionSmall.Models.Converter
{
    /// <summary>
    /// 혈통이 미등록우 같이 2글자 이상이면 2글자로 변환.
    /// </summary>
    public class MotherLevelConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string motherLevel)
            {
                // 해남진도축협 전광판에서는 등록구분 "미등"을 "미등록"으로 전체 표출한다.
                if (string.Equals(parameter as string, "HaenamJindo", StringComparison.Ordinal) && motherLevel == "미등")
                {
                    return "미등록";
                }

                // 문자열 길이가 2자 이상인지 확인
                if (motherLevel.Length > 2)
                {
                    // 앞에서 2글자만 반환
                    return motherLevel.Substring(0, 2);
                }
                else
                {
                    // 2자 이하인 경우 원본 문자열 반환
                    return motherLevel;
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 데이터만 표시하기 때문에 이 메서드는 필요하지 않음
            return value;
        }
    }
}
