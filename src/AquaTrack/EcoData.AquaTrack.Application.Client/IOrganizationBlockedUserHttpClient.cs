using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Errors;
using OneOf;

namespace EcoData.AquaTrack.Application.Client;

public interface IOrganizationBlockedUserHttpClient
{
    IAsyncEnumerable<OrganizationBlockedUserDto> GetBlockedUsersAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationBlockedUserDto, ConflictError, ApiError>> BlockUserAsync(
        Guid organizationId,
        Guid userId,
        string? reason,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, NotFoundError, ApiError>> UnblockUserAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
