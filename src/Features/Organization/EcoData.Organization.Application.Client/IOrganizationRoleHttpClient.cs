using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Requests;
using OneOf;
using OneOf.Types;

namespace EcoData.Organization.Application.Client;

public interface IOrganizationRoleHttpClient
{
    Task<OneOf<IReadOnlyList<OrganizationRoleDto>, RequestFailed>> GetAllAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationRoleDto, RequestFailed>> CreateAsync(
        Guid organizationId,
        OrganizationRoleRequest request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationRoleDto, RequestFailed>> UpdateAsync(
        Guid organizationId,
        Guid roleId,
        OrganizationRoleRequest request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, RequestFailed>> DeleteAsync(
        Guid organizationId,
        Guid roleId,
        CancellationToken cancellationToken = default
    );
}
