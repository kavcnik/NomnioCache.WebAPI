using Nomnio.WebAPI.Contracts.Grains;

namespace Nomnio.WebAPI.Contracts
{
    public interface ICacheGrain : IGrainWithStringKey
    {
        Task<CacheResult> GetAsync(string email, CancellationToken ct);
        Task<bool> AddAsync(string email, string details, CancellationToken ct);
    }
}
