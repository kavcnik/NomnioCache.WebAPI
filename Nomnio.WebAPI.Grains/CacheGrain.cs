using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nomnio.WebAPI.Contracts;
using Nomnio.WebAPI.Contracts.DataSources;
using Nomnio.WebAPI.Contracts.Grains;

namespace Nomnio.WebAPI.Grains
{
    public class CacheGrain : Grain, ICacheGrain
    {
        private readonly IPersistentState<CacheState> _state;
        private readonly IDataSource _dataSource;
        private readonly ILogger<CacheGrain> _logger;
        private readonly CacheOptions _cacheOptions;

        public CacheGrain(
            [PersistentState("cache", "cacheStore")] IPersistentState<CacheState> state,
            IDataSource source,
            ILogger<CacheGrain> logger,
            IOptions<CacheOptions> cacheOptions)
        {
            _state = state;
            _dataSource = source;
            _logger = logger;
            _cacheOptions = cacheOptions.Value;
        }

        public async Task<CacheResult> GetAsync(string email, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var domain = this.GetPrimaryKeyString();

            if (_state.State.Entries.TryGetValue(email, out var entry) && entry.ExpiresAt > now)
            {
                _logger.LogInformation(
                    "Cache hit for email {Email} in domain {Domain}",
                    email, domain);

                return ToResult(email, entry, isFromCache: true);
            }

            try
            {
                _logger.LogInformation(
                    "Cache miss for email {Email} in domain {Domain}",
                    email, domain);

                var details = await _dataSource.FetchAsync(email, ct);

                var newEntry = new CacheEntry
                {
                    Details = details,
                    ExpiresAt = now.AddMinutes(_cacheOptions.TtlMinutes)
                };
                _state.State.Entries[email] = newEntry;

                await _state.WriteStateAsync();

                return ToResult(email, newEntry, isFromCache: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Data source failure for email {Email} in domain {Domain}",
                    email, domain);

                if (entry != null)
                {
                    return ToResult(email, entry, isFromCache: true);
                }

                throw;
            }
        }

        public async Task<bool> AddAsync(string email, string details, CancellationToken ct)
        {
            var domain = this.GetPrimaryKeyString();

            if (_state.State.Entries.TryGetValue(email, out var existing) && existing.Details != null)
            {
                _logger.LogInformation(
                    "Attempt to add already breached email {Email} in domain {Domain}",
                    email, domain);

                return false;
            }

            await _dataSource.AddAsync(email, details, ct);

            _state.State.Entries[email] = new CacheEntry
            {
                Details = details,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_cacheOptions.TtlMinutes)
            };

            await _state.WriteStateAsync();

            _logger.LogInformation(
                "Breached email {Email} added successfully in domain {Domain}",
                email, domain);

            return true;
        }

        private static CacheResult ToResult(string email, CacheEntry entry, bool isFromCache)
        {
            return new CacheResult
            {
                Email = email,
                Details = entry.Details,
                Found = entry.Details != null,
                IsFromCache = isFromCache,
                ExpiresAt = entry.ExpiresAt
            };
        }
    }
}
