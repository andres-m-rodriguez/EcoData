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

    // The image id is minted by the caller because the blob is named after it
    // and written before the row exists.
    Task<SightingImageDto> AddImageAsync(
        Guid sightingId,
        Guid imageId,
        Guid uploaderUserId,
        string uploaderDisplayName,
        string blobName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default
    );

    Task<SightingImageLocation?> GetImageAsync(
        Guid sightingId,
        Guid imageId,
        CancellationToken cancellationToken = default
    );

    Task<int> CountImagesAsync(Guid sightingId, CancellationToken cancellationToken = default);

    Task<OneOf<Success, NotFound>> DeleteImageAsync(
        Guid sightingId,
        Guid imageId,
        CancellationToken cancellationToken = default
    );
}

public sealed record SightingImageLocation(string BlobName, string ContentType);
