using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface IOrganizationRepository
{
    IAsyncEnumerable<OrganizationDtoForList> GetOrganizationsAsync(
        OrganizationParameters parameters,
        CancellationToken cancellationToken = default
    );
    Task<OrganizationDtoForDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrganizationDtoForCreated?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<OrganizationDtoForCreated> CreateAsync(OrganizationDtoForCreate dto, CancellationToken cancellationToken = default);
    Task<OrganizationDtoForDetail?> UpdateAsync(Guid id, OrganizationDtoForUpdate dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
