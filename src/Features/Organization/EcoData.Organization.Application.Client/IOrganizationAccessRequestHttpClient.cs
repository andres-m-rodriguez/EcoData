using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Parameters;
using EcoData.Organization.Contracts.Requests;
using OneOf;

namespace EcoData.Organization.Application.Client;

public interface IOrganizationAccessRequestHttpClient
{
    IAsyncEnumerable<OrganizationAccessRequestDto> GetByOrganizationAsync(
        Guid organizationId,
        OrganizationAccessRequestParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationAccessRequestDto, ProblemDetail>> GetByIdAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationAccessRequestDto, ProblemDetail>> CreateAsync(
        Guid organizationId,
        CreateOrganizationAccessRequestRequest request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationAccessRequestDto, ProblemDetail>> UpdateStatusAsync(
        Guid organizationId,
        Guid id,
        UpdateOrganizationAccessRequestStatusRequest request,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<OrganizationAccessRequestDto> GetMyRequestsAsync(
        OrganizationAccessRequestParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationAccessRequestDto, ProblemDetail>> CancelMyRequestAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
}
