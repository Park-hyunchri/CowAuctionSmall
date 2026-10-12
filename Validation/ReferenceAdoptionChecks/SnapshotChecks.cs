using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Auction;
using System.Reflection;

internal static class SnapshotChecks
{
    internal static void Run()
    {
        var original = new gValues();
        PropertyInfo[] properties = typeof(gValues).GetProperties()
            .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
            .ToArray();
        foreach (var property in properties)
        {
            object value = property.PropertyType == typeof(string) ? "fixture_" + property.Name
                : property.PropertyType == typeof(bool) ? true
                : property.PropertyType == typeof(decimal) ? (object)123.45m
                : property.PropertyType == typeof(int) ? (object)37
                : property.PropertyType == typeof(IReadOnlyCollection<string>) ? new[] { nameof(gValues.LowestPrice), nameof(gValues.Note) }
                : property.PropertyType == typeof(CowAuctionSmall.Domain.AnimalHistory.AnimalHistoryOwner)
                    ? new CowAuctionSmall.Domain.AnimalHistory.AnimalHistoryOwner("SYNTHETIC_OWNER", "SYNTHETIC_ADDRESS")
                : throw new InvalidOperationException("Add a deep-copy fixture for property: " + property.Name);
            property.SetValue(original, value);
        }
        var clone = original.Clone();
        Program.Check(!ReferenceEquals(original, clone), "clone has independent identity");
        foreach (var property in properties)
            Program.Check(property.Name == nameof(gValues.SuppliedFields)
                ? original.SuppliedFields!.SequenceEqual(clone.SuppliedFields!)
                : Equals(property.GetValue(original), property.GetValue(clone)),
                "clone preserves property: " + property.Name);

        var input = new List<gValues> { original };
        var message = new DataChangedMessage(input);
        string expected = original.Note;
        original.Note = "changed after send";
        original.NoteChangeOwner = "changed history after send";
        input.Clear();
        Program.Check(message.Data.Count == 1 && message.Data[0].Note == expected,
            "message survives producer clearing list and mutating object");
        Program.Check(message.Data[0].NoteChangeOwner == clone.NoteChangeOwner,
            "message owns history display data");

        var raw = new[] { "AF", "fixture", "22" };
        var netty = new DataStringArrMessage(raw);
        var query = new DataToServerGetArrMsg(raw);
        var result = new DataToServerGetAF_SD(raw);
        raw[2] = "24";
        Program.Check(netty.Data[2] == "22" && query.Data[2] == "22" && result.Data[2] == "22",
            "protocol message arrays have independent producer ownership");
    }
}
