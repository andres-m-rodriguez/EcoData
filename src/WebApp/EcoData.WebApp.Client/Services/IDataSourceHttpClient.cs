using EcoData.Organization.Contracts.Dtos;

namespace EcoData.WebApp.Client.Services;

public interface IDataSourceHttpClient
{
    Task<IReadOnlyList<DataSourceDtoForList>> GetDataSourcesAsync(CancellationToken cancellationToken = default);
}
