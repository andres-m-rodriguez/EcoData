using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;

namespace EcoData.AquaTrack.Application.Client;

public interface IOrganizationHttpClient
{
    IAsyncEnumerable<OrganizationDtoForList> GetOrganizationsAsync(
        OrganizationParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationDtoForDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OrganizationDtoForCreated> CreateAsync(
        OrganizationDtoForCreate dto,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationDtoForDetail?> UpdateAsync(
        Guid id,
        OrganizationDtoForUpdate dto,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
