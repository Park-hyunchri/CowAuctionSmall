using CowAuctionSmall.Domain.Auction;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CowAuctionSmall.Application.Auction
{
    /// <summary>Accessed only by the auction state consumer. Owns indexes and unconfirmed SV fields.</summary>
    public sealed class AuctionItemStore
    {
        private List<gValues>? _source;
        private readonly Dictionary<string, gValues> _items = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<gValues>> _spaces = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PendingUpdate> _pending = new(StringComparer.Ordinal);
        private sealed record FieldValue(object? Value, long Revision);
        private sealed record PendingUpdate(gValues Identity, Dictionary<string, FieldValue> Fields);
        public long Revision { get; private set; }
        public long LookupCount { get; private set; }

        private static readonly HashSet<string> ResultFields = new(StringComparer.Ordinal)
        {
            nameof(gValues.AuctionResultStatus), nameof(gValues.BidPrice), nameof(gValues.Bidder),
            nameof(gValues.BidderNum), nameof(gValues.BidderString), nameof(gValues.ResultBidderNumber)
        };

        public void Bind(List<gValues>? source)
        {
            if (ReferenceEquals(source, _source)) return;
            _source = source;
            _items.Clear();
            _spaces.Clear();
            foreach (var item in source ?? Enumerable.Empty<gValues>())
            {
                _items[Key(item.SipNumber)] = item;
                AddSpace(item);
            }
        }

        public gValues? Find(string sip)
        {
            LookupCount++;
            return _items.TryGetValue(Key(sip), out var value) ? value : null;
        }

        public static string Key(string sip) => int.TryParse(sip, out int number)
            ? number.ToString(CultureInfo.InvariantCulture) : sip;

        private static bool SameAnimal(gValues left, gValues right) =>
            (string.IsNullOrEmpty(left.TraceNumber) || string.IsNullOrEmpty(right.TraceNumber) || left.TraceNumber == right.TraceNumber) &&
            (string.IsNullOrEmpty(left.AuctionRound) || string.IsNullOrEmpty(right.AuctionRound) || left.AuctionRound == right.AuctionRound) &&
            (string.IsNullOrEmpty(left.AuctionHouseCode) || string.IsNullOrEmpty(right.AuctionHouseCode) || left.AuctionHouseCode == right.AuctionHouseCode);

        public IReadOnlyList<gValues> Apply(AuctionItemUpdate update, bool protectResult)
        {
            string key = Key(update.Values.SipNumber);
            var current = Find(key);
            bool same = current != null && SameAnimal(current, update.Values);
            var fields = update.Fields.Where(name => !protectResult || !ResultFields.Contains(name)).ToArray();
            bool unchanged = same && fields.All(name => Equals(AuctionItemFields.Get(current!, name), AuctionItemFields.Get(update.Values, name)));
            var next = unchanged ? current! : same ? current!.Clone() : new gValues();
            if (!unchanged)
                foreach (string name in fields)
                    AuctionItemFields.Set(next, name, AuctionItemFields.Get(update.Values, name));
            next.SipNumber = key;
            next.AuctionHouseCode = update.AuctionHouseCode;
            next.AuctionRound = update.Round;

            long revision = ++Revision;
            if (!_pending.TryGetValue(key, out var pending) || !SameAnimal(pending.Identity, next))
                _pending[key] = pending = new PendingUpdate(next.Clone(), new Dictionary<string, FieldValue>(StringComparer.Ordinal));
            foreach (string name in fields)
            {
                object? value = AuctionItemFields.Get(next, name);
                // A duplicate field must not erase the revision that protects an earlier real change.
                if (!pending.Fields.TryGetValue(name, out var previous) || !Equals(previous.Value, value))
                    pending.Fields[name] = new FieldValue(value, revision);
            }
            if (_source == null || unchanged) return Array.Empty<gValues>();

            bool changed = current == null || !current.Equals(next);
            string? oldSpace = current?.SpaceIndex;
            if (current != null)
            {
                RemoveSpace(current);
                // Preserve the canonical object's identity for existing state handlers.
                foreach (string name in AuctionItemFields.All)
                    AuctionItemFields.Set(current, name, AuctionItemFields.Get(next, name));
                current.AuctionHouseCode = next.AuctionHouseCode;
                current.AuctionRound = next.AuctionRound;
                if (!same) { current.IsRunning = false; current.NoteChangeOwner = string.Empty; current.HistoryOwner = null; }
                next = current;
            }
            else _source.Add(next);
            _items[key] = next;
            AddSpace(next);
            return changed ? SelectSpaces(new[] { oldSpace, next.SpaceIndex }) : Array.Empty<gValues>();
        }

        public void Protect(gValues item, long requestRevision)
        {
            string key = Key(item.SipNumber);
            if (!_pending.TryGetValue(key, out var pending) || !SameAnimal(item, pending.Identity)) return;
            var supplied = item.SuppliedFields?.ToHashSet(StringComparer.Ordinal);
            foreach (var pair in pending.Fields.ToArray())
            {
                bool present = supplied == null || supplied.Contains(pair.Key);
                if (present && pair.Value.Revision <= requestRevision && Equals(AuctionItemFields.Get(item, pair.Key), pair.Value.Value))
                    pending.Fields.Remove(pair.Key);
                else AuctionItemFields.Set(item, pair.Key, pair.Value.Value);
            }
            if (pending.Fields.Count == 0) _pending.Remove(key);
        }

        public gValues PreserveNewerIdentity(gValues incoming, long requestRevision)
        {
            string key = Key(incoming.SipNumber);
            if (_pending.TryGetValue(key, out var pending) && !SameAnimal(incoming, pending.Identity) &&
                pending.Fields.Values.Any(field => field.Revision > requestRevision))
                return Find(key)?.Clone() ?? incoming;
            return incoming;
        }

        public void MergeAbsentFields(gValues incoming)
        {
            var before = Find(incoming.SipNumber);
            if (before == null || !SameAnimal(before, incoming)) return;
            if (incoming.SuppliedFields != null)
            {
                var supplied = incoming.SuppliedFields.ToHashSet(StringComparer.Ordinal);
                foreach (string name in AuctionItemFields.All)
                    if (!supplied.Contains(name))
                        AuctionItemFields.Set(incoming, name, AuctionItemFields.Get(before, name));
            }
            incoming.NoteChangeOwner = before.NoteChangeOwner;
            incoming.HistoryOwner = before.HistoryOwner;
        }

        public void ProtectMissing(List<gValues> snapshot, long requestRevision)
        {
            var present = snapshot.Select(item => Key(item.SipNumber)).ToHashSet(StringComparer.Ordinal);
            foreach (var pair in _pending.ToArray())
            {
                if (present.Contains(pair.Key)) continue;
                if (pair.Value.Fields.Values.Any(field => field.Revision > requestRevision))
                {
                    var item = Find(pair.Key);
                    if (item != null) snapshot.Add(item.Clone());
                }
                else _pending.Remove(pair.Key);
            }
        }

        public void ClearResult(string sip)
        {
            if (_pending.TryGetValue(Key(sip), out var pending))
                foreach (string name in ResultFields) pending.Fields.Remove(name);
        }

        public void RemovePending(string sip) => _pending.Remove(Key(sip));

        public IReadOnlyList<gValues> SelectSpaces(IEnumerable<string?> spaces)
        {
            var result = new List<gValues>();
            foreach (string? space in spaces.Distinct(StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(space)) continue;
                var selected = _spaces.TryGetValue(space, out var candidates)
                    ? CowDisplaySelector.SelectForSpaceIndex(candidates) : null;
                result.Add(selected?.Clone() ?? new gValues { SpaceIndex = space, SipNumber = "-1", AuctionResultStatus = "00" });
            }
            return result;
        }

        private void AddSpace(gValues item)
        {
            if (!_spaces.TryGetValue(item.SpaceIndex, out var values))
                _spaces[item.SpaceIndex] = values = new HashSet<gValues>(ReferenceEqualityComparer.Instance);
            values.Add(item);
        }

        private void RemoveSpace(gValues item)
        {
            if (_spaces.TryGetValue(item.SpaceIndex, out var values))
            {
                values.Remove(item);
                if (values.Count == 0) _spaces.Remove(item.SpaceIndex);
            }
        }

        public void Reset()
        {
            _pending.Clear();
            _source = null;
            _items.Clear();
            _spaces.Clear();
            Revision++;
        }
    }
}
