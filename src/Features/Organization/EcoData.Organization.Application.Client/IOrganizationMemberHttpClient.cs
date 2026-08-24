using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Parameters;
using OneOf;
using OneOf.Types;

namespace EcoData.Organization.Application.Client;

public interface IOrganizationMemberHttpClient
{
    IAsyncEnumerable<OrganizationMemberDto> GetListAsync(
        Guid organizationId,
        OrganizationMemberParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationMemberDto, RequestFailed>> GetAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationMemberDto, RequestFailed>> UpdateAsync(
        Guid organizationId,
        Guid userId,
        UpdateMemberRoleRequest request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, RequestFailed>> DeleteAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
