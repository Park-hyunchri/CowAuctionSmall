using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CowAuctionSmall.Domain.Auction
{
    /// <summary>수신 형식에 실제로 포함된 유효한 필드만 가진 부분 갱신이다.</summary>
    public sealed record AuctionItemUpdate(gValues Values, IReadOnlyCollection<string> Fields,
        string AuctionHouseCode, string Round);

    /// <summary>필드 접근자는 한 번만 생성하고 수신마다 리플렉션을 반복하지 않는다.</summary>
    public static class AuctionItemFields
    {
        private sealed record Accessor(Func<gValues, object?> Read, Action<gValues, object?> Write);
        private static readonly IReadOnlyDictionary<string, Accessor> Accessors = CreateAccessors();
        public static IReadOnlyCollection<string> All { get; } = Accessors.Keys.Where(field =>
            field != nameof(gValues.AuctionHouseCode) && field != nameof(gValues.AuctionRound) &&
            field != nameof(gValues.SuppliedFields) && field != nameof(gValues.IsRunning) &&
            field != nameof(gValues.NoteChangeOwner) && field != nameof(gValues.HistoryOwner)).ToArray();

        public static object? Get(gValues value, string field) => Accessors[field].Read(value);
        public static void Set(gValues value, string field, object? content) => Accessors[field].Write(value, content);

        private static IReadOnlyDictionary<string, Accessor> CreateAccessors()
        {
            var result = new Dictionary<string, Accessor>(StringComparer.Ordinal);
            foreach (var property in typeof(gValues).GetProperties().Where(property =>
                property.GetMethod?.IsPublic == true && property.SetMethod?.IsPublic == true &&
                property.GetIndexParameters().Length == 0))
            {
                var target = Expression.Parameter(typeof(gValues), "target");
                var content = Expression.Parameter(typeof(object), "content");
                var member = Expression.Property(target, property);
                result.Add(property.Name, new Accessor(
                    Expression.Lambda<Func<gValues, object?>>(Expression.Convert(member, typeof(object)), target).Compile(),
                    Expression.Lambda<Action<gValues, object?>>(Expression.Assign(member,
                        Expression.Convert(content, property.PropertyType)), target, content).Compile()));
            }
            return result;
        }
    }
}
