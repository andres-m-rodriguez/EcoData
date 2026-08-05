using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using OneOf;
using OneOf.Types;

namespace EcoData.Organization.Application.Client;

public interface IOrganizationBlockedUserHttpClient
{
    IAsyncEnumerable<OrganizationBlockedUserDto> GetBlockedUsersAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationBlockedUserDto, RequestFailed>> BlockUserAsync(
        Guid organizationId,
        Guid userId,
        string? reason,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, RequestFailed>> UnblockUserAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
