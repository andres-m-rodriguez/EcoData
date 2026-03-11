using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Errors;
using OneOf;

namespace EcoData.AquaTrack.Application.Client;

public interface IOrganizationMemberHttpClient
{
    IAsyncEnumerable<OrganizationMemberDto> GetAllAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationMemberDto, NotFoundError>> GetAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationMemberDto, NotFoundError, ConflictError, ApiError>> CreateAsync(
        Guid organizationId,
        AddMemberRequest request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OrganizationMemberDto, NotFoundError, ValidationError, ApiError>> UpdateAsync(
        Guid organizationId,
        Guid userId,
        UpdateMemberRoleRequest request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, NotFoundError, ValidationError, ApiError>> DeleteAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
