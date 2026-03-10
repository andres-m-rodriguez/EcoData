using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Errors;
using EcoData.AquaTrack.Contracts.Parameters;
using OneOf;

namespace EcoData.AquaTrack.Application.Client;

public interface IOrganizationHttpClient
{
    IAsyncEnumerable<OrganizationDtoForList> GetOrganizationsAsync(
        OrganizationParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationDtoForDetail, NotFoundError>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationDtoForCreated, ValidationError, ApiError>> CreateAsync(
        OrganizationDtoForCreate dto,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationDtoForDetail, NotFoundError, ValidationError, ApiError>> UpdateAsync(
        Guid id,
        OrganizationDtoForUpdate dto,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, NotFoundError, ApiError>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
}
