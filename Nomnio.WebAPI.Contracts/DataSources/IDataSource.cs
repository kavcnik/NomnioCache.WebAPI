
namespace Nomnio.WebAPI.Contracts.DataSources
{
    public interface IDataSource
    {
        Task<string?> FetchAsync(string email, CancellationToken ct);
        Task AddAsync(string email, string details, CancellationToken ct);
    }
}
