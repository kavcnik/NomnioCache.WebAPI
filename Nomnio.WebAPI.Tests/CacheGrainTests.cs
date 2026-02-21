using Microsoft.Extensions.DependencyInjection;
using Nomnio.WebAPI.Contracts;
using Nomnio.WebAPI.Contracts.DataSources;
using Nomnio.WebAPI.Services;
using Orleans.TestingHost;

namespace Nomnio.WebAPI.Tests
{
    public class CacheGrainTests : IAsyncLifetime
    {
        private TestCluster _cluster = null!;

        public async Task InitializeAsync()
        {
            var builder = new TestClusterBuilder();
            builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
            _cluster = builder.Build();
            await _cluster.DeployAsync();
        }

        public async Task DisposeAsync()
        {
            await _cluster.StopAllSilosAsync();
        }

        [Fact]
        public async Task GetAsync_WhenNotFound_ReturnsCacheResultWithFoundFalse()
        {
            var grain = _cluster.GrainFactory.GetGrain<ICacheGrain>("example.com");
            var result = await grain.GetAsync("unknown@example.com", CancellationToken.None);

            Assert.False(result.Found);
            Assert.Null(result.Details);
        }

        [Fact]
        public async Task AddAsync_ThenGetAsync_ReturnsCacheResultWithDetails()
        {
            var grain = _cluster.GrainFactory.GetGrain<ICacheGrain>("example.com");

            var added = await grain.AddAsync("add-get@example.com", "leaked in 2024", CancellationToken.None);
            Assert.True(added);

            var result = await grain.GetAsync("add-get@example.com", CancellationToken.None);
            Assert.True(result.Found);
            Assert.Equal("leaked in 2024", result.Details);
        }

        [Fact]
        public async Task AddAsync_DuplicateEmail_ReturnsFalse()
        {
            var grain = _cluster.GrainFactory.GetGrain<ICacheGrain>("example.com");

            var first = await grain.AddAsync("duplicate@example.com", "first breach", CancellationToken.None);
            Assert.True(first);

            var second = await grain.AddAsync("duplicate@example.com", "second breach", CancellationToken.None);
            Assert.False(second);
        }

        [Fact]
        public async Task GetAsync_CacheHit_ReturnsFromCache()
        {
            var grain = _cluster.GrainFactory.GetGrain<ICacheGrain>("example.com");

            await grain.AddAsync("cachehit@example.com", "breach data", CancellationToken.None);

            var result = await grain.GetAsync("cachehit@example.com", CancellationToken.None);
            Assert.True(result.IsFromCache);
        }

        [Fact]
        public async Task MultipleEmails_SameDomain_StoredInSameGrain()
        {
            var grain = _cluster.GrainFactory.GetGrain<ICacheGrain>("multi.com");

            await grain.AddAsync("alice@multi.com", "breach A", CancellationToken.None);
            await grain.AddAsync("bob@multi.com", "breach B", CancellationToken.None);

            var resultA = await grain.GetAsync("alice@multi.com", CancellationToken.None);
            var resultB = await grain.GetAsync("bob@multi.com", CancellationToken.None);

            Assert.True(resultA.Found);
            Assert.Equal("breach A", resultA.Details);
            Assert.True(resultB.Found);
            Assert.Equal("breach B", resultB.Details);
        }

        private class TestSiloConfigurator : ISiloConfigurator
        {
            public void Configure(ISiloBuilder siloBuilder)
            {
                siloBuilder.AddMemoryGrainStorage("cacheStore");
                siloBuilder.Services.AddSingleton<IDataSource, InMemoryDataSource>();
                siloBuilder.Services.Configure<CacheOptions>(opts => opts.TtlMinutes = 5);
            }
        }
    }

    public class CacheGrainStaleOnErrorTests : IAsyncLifetime
    {
        private TestCluster _cluster = null!;
        private static readonly FailingDataSource _dataSource = new();

        public async Task InitializeAsync()
        {
            var builder = new TestClusterBuilder();
            builder.AddSiloBuilderConfigurator<StaleTestSiloConfigurator>();
            _cluster = builder.Build();
            await _cluster.DeployAsync();
        }

        public async Task DisposeAsync()
        {
            await _cluster.StopAllSilosAsync();
        }

        [Fact]
        public async Task GetAsync_DataSourceFails_WithStaleData_ReturnsStaleResult()
        {
            var grain = _cluster.GrainFactory.GetGrain<ICacheGrain>("stale.com");

            // First add succeeds (data source is not failing yet)
            _dataSource.ShouldFail = false;
            await grain.AddAsync("user@stale.com", "breach info", CancellationToken.None);

            // Now make the data source fail — simulate expired cache by using a grain
            // that has stale data (TTL set to 0 so it expires immediately)
            _dataSource.ShouldFail = true;

            var result = await grain.GetAsync("user@stale.com", CancellationToken.None);

            // Should return stale data instead of throwing
            Assert.True(result.Found);
            Assert.Equal("breach info", result.Details);
            Assert.True(result.IsFromCache);
        }

        [Fact]
        public async Task GetAsync_DataSourceFails_NoStaleData_Throws()
        {
            var grain = _cluster.GrainFactory.GetGrain<ICacheGrain>("nostale.com");

            _dataSource.ShouldFail = true;

            await Assert.ThrowsAsync<Exception>(
                () => grain.GetAsync("missing@nostale.com", CancellationToken.None));
        }

        private class StaleTestSiloConfigurator : ISiloConfigurator
        {
            public void Configure(ISiloBuilder siloBuilder)
            {
                siloBuilder.AddMemoryGrainStorage("cacheStore");
                siloBuilder.Services.AddSingleton<IDataSource>(_dataSource);
                // TTL of 0 so cache expires immediately — forces re-fetch on GetAsync
                siloBuilder.Services.Configure<CacheOptions>(opts => opts.TtlMinutes = 0);
            }
        }

        private class FailingDataSource : IDataSource
        {
            public bool ShouldFail { get; set; }

            public Task<string?> FetchAsync(string email, CancellationToken ct)
            {
                if (ShouldFail)
                    throw new Exception("Data source unavailable");

                return Task.FromResult<string?>(null);
            }

            public Task AddAsync(string email, string details, CancellationToken ct)
            {
                if (ShouldFail)
                    throw new Exception("Data source unavailable");

                return Task.CompletedTask;
            }
        }
    }
}
