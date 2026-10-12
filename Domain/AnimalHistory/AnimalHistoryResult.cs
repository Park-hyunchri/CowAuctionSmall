using System;
using System.Collections.Generic;

namespace CowAuctionSmall.Domain.AnimalHistory
{
    public enum HistoryStatus { Success, AuthenticationError, QuotaExceeded, TransientError, InvalidResponse, Disabled }

    public sealed class AnimalHistoryEntry
    {
        public string Date { get; set; } = "";
        public string Type { get; set; } = "";
        public string Owner { get; set; } = "";
        public string FarmAddress { get; set; } = "";
    }

    public sealed record AnimalHistoryOwner(string Name, string FarmAddress);

    public sealed class AnimalHistoryResult
    {
        public const int CurrentVersion = 2;
        public int Version { get; set; } = CurrentVersion;
        public HistoryStatus Status { get; set; }
        public List<AnimalHistoryEntry> Entries { get; set; } = new List<AnimalHistoryEntry>();
    }


}
