using EcoData.Wildlife.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using OneOf;
using OneOf.Types;

namespace EcoData.Wildlife.DataAccess.Interfaces;

public interface ISightingRepository
{
    Task<OneOf<SightingDto, NotFound>> CreateAsync(
        Guid organizationId,
        Guid reporterUserId,
        string reporterDisplayName,
        SightingDtoForCreate dto,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SightingDto> GetMineAsync(
        Guid reporterUserId,
        SightingParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SightingNoteDto, NotFound>> AddNoteAsync(
        Guid sightingId,
        Guid authorUserId,
        string authorDisplayName,
        SightingNoteDtoForCreate dto,
        CancellationToken cancellationToken = default
    );

    Task<(Guid OrganizationId, Guid ReporterUserId)?> GetOwnerAsync(
        Guid sightingId,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SightingDto> GetByOrganizationAsync(
        Guid organizationId,
        SightingParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<int> CountAsync(
        Guid organizationId,
        SightingStatus? status,
        CancellationToken cancellationToken = default
    );

    Task<SightingDto?> GetByIdAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, NotFound>> ApproveAsync(
        Guid organizationId,
        Guid id,
        Guid reviewerUserId,
        string reviewerDisplayName,
        string? reason,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, NotFound>> DenyAsync(
        Guid organizationId,
        Guid id,
        Guid reviewerUserId,
        string reviewerDisplayName,
        string reason,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, NotFound>> UnapproveAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken = default
    );
}
