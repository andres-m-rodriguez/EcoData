using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.Database.Models;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface IOrganizationAccessRequestRepository
{
    IAsyncEnumerable<OrganizationAccessRequestDto> GetByOrganizationAsync(
        Guid organizationId,
        OrganizationAccessRequestParameters parameters,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<OrganizationAccessRequestDto> GetByUserAsync(
        Guid userId,
        OrganizationAccessRequestParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationAccessRequestDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationAccessRequestDto> CreateAsync(
        Guid userId,
        Guid organizationId,
        string? requestMessage,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationAccessRequestDto?> UpdateStatusAsync(
        Guid id,
        OrganizationAccessRequestStatus status,
        string? reviewNotes,
        Guid reviewedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OrganizationAccessRequestDto?> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<bool> ExistsPendingAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}
