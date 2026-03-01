using EcoData.AquaTrack.Contracts.Dtos;

namespace EcoData.AquaTrack.Application.Client;

public interface IDataSourceHttpClient
{
    Task<IReadOnlyList<DataSourceDtoForList>> GetDataSourcesAsync(CancellationToken cancellationToken = default);
}
