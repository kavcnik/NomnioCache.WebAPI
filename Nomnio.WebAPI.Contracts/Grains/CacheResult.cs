namespace Nomnio.WebAPI.Contracts.Grains
{
    [GenerateSerializer]
    public class CacheResult
    {
        [Id(0)]
        public required string Email { get; init; }
        [Id(1)]
        public string? Details { get; init; }
        [Id(2)]
        public bool Found { get; init; }
        [Id(3)]
        public bool IsFromCache { get; init; }
        [Id(4)]
        public DateTime ExpiresAt { get; init; }
    }
}
