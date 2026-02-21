namespace Nomnio.WebAPI.Contracts
{
    public class CacheOptions
    {
        public const string SectionName = "Cache";

        public int TtlMinutes { get; set; } = 5;
    }
}
