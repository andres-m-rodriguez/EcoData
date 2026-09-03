using EcoData.Common.Problems;
using EcoData.Wildlife.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using OneOf;
using OneOf.Types;

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

    Task<OneOf<SightingImageDto, ValidationFailed, RequestFailed>> UploadImageAsync(
        Guid sightingId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    Task<OneOf<Success, RequestFailed>> DeleteImageAsync(
        Guid sightingId,
        Guid imageId,
        CancellationToken ct = default);

    Task<OneOf<IReadOnlyList<SightingDto>, RequestFailed>> GetByOrganizationAsync(
        Guid organizationId,
        SightingParameters parameters,
        CancellationToken ct = default);

    Task<OneOf<int, RequestFailed>> CountAsync(
        Guid organizationId,
        SightingStatus? status,
        CancellationToken ct = default);

    Task<OneOf<SightingDto, RequestFailed>> GetByIdAsync(
        Guid organizationId,
        Guid id,
        CancellationToken ct = default);

    Task<OneOf<Success, ValidationFailed, RequestFailed>> ApproveAsync(
        Guid organizationId,
        Guid id,
        SightingApprovalDto dto,
        CancellationToken ct = default);

    Task<OneOf<Success, ValidationFailed, RequestFailed>> DenyAsync(
        Guid organizationId,
        Guid id,
        SightingDenialDto dto,
        CancellationToken ct = default);

    Task<OneOf<Success, RequestFailed>> UnapproveAsync(
        Guid organizationId,
        Guid id,
        CancellationToken ct = default);
}
