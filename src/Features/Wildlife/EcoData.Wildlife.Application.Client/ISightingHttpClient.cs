using EcoData.Common.Problems.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Errors;
using EcoData.Wildlife.Contracts.Parameters;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public interface ISightingHttpClient
{
    Task<OneOf<SightingDto, ValidationFailed, RequestFailed>> ReportAsync(
        Guid organizationId,
        SightingDtoForCreate dto,
        CancellationToken ct = default);

    IAsyncEnumerable<SightingDto> GetMineAsync(
        SightingParameters? parameters = null,
        CancellationToken ct = default);

    Task<OneOf<SightingNoteDto, ValidationFailed, RequestFailed>> AddNoteAsync(
        Guid sightingId,
        SightingNoteDtoForCreate dto,
        CancellationToken ct = default);
}
