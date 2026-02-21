namespace Nomnio.WebAPI.Grains
{
    [GenerateSerializer]
    public class CacheState
    {
        [Id(0)]
        public Dictionary<string, CacheEntry> Entries { get; set; } = new();
    }

    [GenerateSerializer]
    public class CacheEntry
    {
        [Id(0)]
        public string? Details { get; set; }
        [Id(1)]
        public DateTime ExpiresAt { get; set; }
    }
}
