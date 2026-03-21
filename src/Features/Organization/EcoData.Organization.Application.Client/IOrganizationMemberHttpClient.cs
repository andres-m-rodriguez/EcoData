using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Errors;
using EcoData.Organization.Contracts.Parameters;
using OneOf;

namespace EcoData.Organization.Application.Client;

public interface IOrganizationMemberHttpClient
{
    IAsyncEnumerable<OrganizationMemberDto> GetAllAsync(
        Guid organizationId,
        OrganizationMemberParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationMemberDto, ProblemDetail>> GetAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationMemberDto, ProblemDetail>> UpdateAsync(
        Guid organizationId,
        Guid userId,
        UpdateMemberRoleRequest request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, ProblemDetail>> DeleteAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
