using System.Collections.Concurrent;
using Nomnio.WebAPI.Contracts;
using Nomnio.WebAPI.Contracts.DataSources;

namespace Nomnio.WebAPI.Services
{
    public class InMemoryDataSource : IDataSource
    {
        private readonly ConcurrentDictionary<string, string> _store = new();

        public Task<string?> FetchAsync(string email, CancellationToken ct)
        {
            _store.TryGetValue(email, out var value);
            return Task.FromResult(value);
        }

        public Task AddAsync(string email, string details, CancellationToken ct)
        {
            _store[email] = details;
            return Task.CompletedTask;
        }
    }
}
