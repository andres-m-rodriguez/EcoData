using EcoData.Organization.Contracts.Dtos;

namespace EcoPortal.Client.Services;

public interface IDataSourceHttpClient
{
    Task<IReadOnlyList<DataSourceDtoForList>> GetDataSourcesAsync(CancellationToken cancellationToken = default);
}
