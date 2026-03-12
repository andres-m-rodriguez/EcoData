using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Parameters;
using EcoData.Organization.Database.Models;

namespace EcoData.Organization.DataAccess.Interfaces;

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

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

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
