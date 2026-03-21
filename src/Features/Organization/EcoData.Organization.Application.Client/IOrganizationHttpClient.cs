using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Errors;
using EcoData.Organization.Contracts.Parameters;
using OneOf;

namespace EcoData.Organization.Application.Client;

public interface IOrganizationHttpClient
{
    IAsyncEnumerable<OrganizationDtoForList> GetOrganizationsAsync(
        OrganizationParameters parameters,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<MyOrganizationDto> GetMyOrganizationsAsync(
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationDtoForDetail, ProblemDetail>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationDtoForCreated, ProblemDetail>> CreateAsync(
        OrganizationDtoForCreate dto,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationDtoForDetail, ProblemDetail>> UpdateAsync(
        Guid id,
        OrganizationDtoForUpdate dto,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, ProblemDetail>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
}
