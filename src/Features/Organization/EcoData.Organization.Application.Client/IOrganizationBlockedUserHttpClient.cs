using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Errors;
using OneOf;

namespace EcoData.Organization.Application.Client;

public interface IOrganizationBlockedUserHttpClient
{
    IAsyncEnumerable<OrganizationBlockedUserDto> GetBlockedUsersAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationBlockedUserDto, ProblemDetail>> BlockUserAsync(
        Guid organizationId,
        Guid userId,
        string? reason,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, ProblemDetail>> UnblockUserAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
