using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Errors;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.Contracts.Requests;
using OneOf;

namespace EcoData.AquaTrack.Application.Client;

public interface IOrganizationAccessRequestHttpClient
{
    IAsyncEnumerable<OrganizationAccessRequestDto> GetByOrganizationAsync(Guid organizationId, OrganizationAccessRequestParameters parameters, CancellationToken cancellationToken = default);

    Task<OneOf<OrganizationAccessRequestDto, NotFoundError>> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<OneOf<OrganizationAccessRequestDto, ConflictError, ApiError>> CreateAsync(Guid organizationId, CreateOrganizationAccessRequestRequest request, CancellationToken cancellationToken = default);

    Task<OneOf<OrganizationAccessRequestDto, NotFoundError, ValidationError, ApiError>> UpdateStatusAsync(Guid organizationId, Guid id, UpdateOrganizationAccessRequestStatusRequest request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<OrganizationAccessRequestDto> GetMyRequestsAsync(OrganizationAccessRequestParameters parameters, CancellationToken cancellationToken = default);

    Task<OneOf<Success, NotFoundError, ValidationError, ApiError>> CancelMyRequestAsync(Guid id, CancellationToken cancellationToken = default);
}
