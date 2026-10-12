using System;
using System.Text.RegularExpressions;

namespace CowAuctionSmall.Infra.Api
{
    public static class AuctionApiAddressResolver
    {
        private const string HouseCodePlaceholder = "{AuctionHouseCode}";

        public static string Resolve(string template, string authenticatedHouseCode)
        {
            if (string.IsNullOrWhiteSpace(authenticatedHouseCode) ||
                !Regex.IsMatch(authenticatedHouseCode, @"\A[0-9]+\z"))
            {
                throw new ArgumentException("로그인 사업장 코드가 없거나 올바르지 않습니다.", nameof(authenticatedHouseCode));
            }

            if (string.IsNullOrWhiteSpace(template))
            {
                throw new ArgumentException("API 주소 설정이 없습니다.", nameof(template));
            }

            string resolved;
            if (template.Contains(HouseCodePlaceholder))
            {
                resolved = template.Replace(HouseCodePlaceholder, authenticatedHouseCode);
            }
            else
            {
                var legacyAddress = Regex.Match(template,
                    @"\Ahttps?://[^/?#]+/api/v1/(?:auction/(?<code>[0-9]+)(?=/entry/board(?:\?|#|\z))|epd/info/(?<code>[0-9]+)(?=/date/))",
                    RegexOptions.IgnoreCase);
                if (!legacyAddress.Success)
                {
                    throw new ArgumentException("API 주소에 사업장 코드 위치가 없습니다.", nameof(template));
                }

                var code = legacyAddress.Groups["code"];
                resolved = template.Remove(code.Index, code.Length).Insert(code.Index, authenticatedHouseCode);
            }

            if (!Uri.TryCreate(resolved, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                resolved.Contains('{') || resolved.Contains('}'))
            {
                throw new ArgumentException("API 주소 형식이 올바르지 않습니다.", nameof(template));
            }

            return resolved;
        }
    }
}
