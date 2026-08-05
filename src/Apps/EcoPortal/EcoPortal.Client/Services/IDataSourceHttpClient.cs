using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using OneOf;

namespace EcoPortal.Client.Services;

public interface IDataSourceHttpClient
{
    Task<OneOf<IReadOnlyList<DataSourceDtoForList>, RequestFailed>> GetDataSourcesAsync(
        CancellationToken cancellationToken = default
    );
}
