using CowAuctionSmall.Infra.XmlSettings;
using CowAuctionSmall.Application.AnimalHistory;
using CowAuctionSmall.Infra.Api;
using CowAuctionSmall.Domain.AnimalHistory;
using System.IO;

static class LiveChecks
{
    // Explicit opt-in diagnostic: one request, no raw response/identity output or persistence.
    public static async Task Run(string repositoryPath, string traceNumber)
    {
        NLog.LogManager.Configuration = new NLog.Config.LoggingConfiguration();
        if (!AnimalHistoryService.IsValidNumber(traceNumber)) throw new ArgumentException("Invalid test trace number");
        var user = new UserXmlParser().ParseXml(Path.Combine(repositoryPath, "Config", "users.XML"));
        var settings = user.AnimalTrace;
        using var client = new AnimalHistoryClient();
        var result = await client.FetchAnimalHistoryAsync(settings, traceNumber, CancellationToken.None);
        Console.WriteLine($"Live API status: {result.Status}; items: {result.Entries.Count}; owner-change note present: {AnimalHistoryNoteFormatter.FormatAnimalHistoryNote(result).Length > 0}");
        if (result.Status != HistoryStatus.Success) throw new InvalidOperationException("Live API verification failed");
    }
}
