using CowAuctionSmall.Domain.AnimalHistory;
using CowAuctionSmall.Domain.Settings;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CowAuctionSmall.Application.Abstractions
{
    public interface IAnimalHistoryClient : IDisposable
    {
        Task<AnimalHistoryResult> FetchAnimalHistoryAsync(AnimalTraceSettings settings, string number, CancellationToken cancellationToken);
    }
}
