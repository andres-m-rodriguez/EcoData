using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Parameters;
using OneOf;
using OneOf.Types;

namespace EcoData.Organization.Application.Client;

public interface IOrganizationHttpClient
{
    IAsyncEnumerable<OrganizationDtoForList> GetOrganizationsAsync(
        OrganizationParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<int, RequestFailed>> GetOrganizationCountAsync(
        OrganizationParameters parameters,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<MyOrganizationDto> GetMyOrganizationsAsync(
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationDtoForDetail, RequestFailed>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationDtoForDetail, RequestFailed>> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationDtoForCreated, RequestFailed>> CreateAsync(
        OrganizationDtoForCreate dto,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationDtoForDetail, RequestFailed>> UpdateAsync(
        Guid id,
        OrganizationDtoForUpdate dto,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, RequestFailed>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
}
