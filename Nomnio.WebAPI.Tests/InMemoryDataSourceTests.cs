using Nomnio.WebAPI.Services;

namespace Nomnio.WebAPI.Tests
{
    public class InMemoryDataSourceTests
    {
        private readonly InMemoryDataSource _sut = new();

        [Fact]
        public async Task FetchAsync_WhenEmailNotFound_ReturnsNull()
        {
            var result = await _sut.FetchAsync("missing@example.com", CancellationToken.None);
            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_ThenFetchAsync_ReturnsStoredDetails()
        {
            await _sut.AddAsync("user@example.com", "breach details", CancellationToken.None);

            var result = await _sut.FetchAsync("user@example.com", CancellationToken.None);
            Assert.Equal("breach details", result);
        }

        [Fact]
        public async Task AddAsync_OverwritesExistingEntry()
        {
            await _sut.AddAsync("user@example.com", "old details", CancellationToken.None);
            await _sut.AddAsync("user@example.com", "new details", CancellationToken.None);

            var result = await _sut.FetchAsync("user@example.com", CancellationToken.None);
            Assert.Equal("new details", result);
        }
    }
}
