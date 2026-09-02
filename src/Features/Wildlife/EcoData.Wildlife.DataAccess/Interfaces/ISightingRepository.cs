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
}
