using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Pagination;
using EcoData.Common.Problems;
using EcoData.Wildlife.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using OneOf;
using OneOf.Types;

namespace EcoData.Wildlife.Application.Client;

// Lost connections and timeouts arrive as status-zero problems; the host's
// handlers own them, so nothing here catches.
public sealed class SightingHttpClient(HttpClient httpClient) : ISightingHttpClient
{
    public async Task<OneOf<SightingDto, ValidationFailed, RequestFailed>> ReportAsync(
        Guid organizationId,
        SightingDtoForCreate dto,
        CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"wildlife/organizations/{organizationId}/sightings",
            dto,
            ct);
        var result = await response.ReadOneOfAsync<SightingDto>(ct);
        if (result.TryPickT0(out var sighting, out var problem))
            return sighting;
        if (problem.Type == ProblemTypes.Validation)
            return ValidationFailed.From(problem);
        return RequestFailed.From(problem);
    }

    public IAsyncEnumerable<SightingDto> GetMineAsync(
        SightingParameters? parameters = null,
        CancellationToken ct = default)
    {
        parameters ??= new SightingParameters();

        var queryString = new QueryStringBuilder()
            .AddCursorParameters(parameters)
            .Add("status", parameters.Status)
            .Add("speciesId", parameters.SpeciesId)
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<SightingDto>(
            $"wildlife/me/sightings{queryString}",
            ct)!;
    }

    public async Task<OneOf<SightingImageDto, ValidationFailed, RequestFailed>> UploadImageAsync(
        Guid sightingId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var file = new StreamContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        var response = await httpClient.PostAsync($"wildlife/sightings/{sightingId}/images", form, ct);
        var result = await response.ReadOneOfAsync<SightingImageDto>(ct);
        if (result.TryPickT0(out var image, out var problem))
            return image;
        if (problem.Type == ProblemTypes.Validation)
            return ValidationFailed.From(problem);
        return RequestFailed.From(problem);
    }

    public async Task<OneOf<Success, RequestFailed>> DeleteImageAsync(
        Guid sightingId,
        Guid imageId,
        CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"wildlife/sightings/{sightingId}/images/{imageId}", ct);
        var problem = await response.ReadProblemAsync(ct);
        if (problem is null)
            return new Success();
        return RequestFailed.From(problem);
    }

    public async Task<OneOf<SightingNoteDto, ValidationFailed, RequestFailed>> AddNoteAsync(
        Guid sightingId,
        SightingNoteDtoForCreate dto,
        CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"wildlife/sightings/{sightingId}/notes",
            dto,
            ct);
        var result = await response.ReadOneOfAsync<SightingNoteDto>(ct);
        if (result.TryPickT0(out var note, out var problem))
            return note;
        if (problem.Type == ProblemTypes.Validation)
            return ValidationFailed.From(problem);
        return RequestFailed.From(problem);
    }

    public async Task<OneOf<IReadOnlyList<SightingDto>, RequestFailed>> GetByOrganizationAsync(
        Guid organizationId,
        SightingParameters parameters,
        CancellationToken ct = default)
    {
        var queryString = new QueryStringBuilder()
            .AddCursorParameters(parameters)
            .Add("status", parameters.Status)
            .Add("speciesId", parameters.SpeciesId)
            .Build();

        var response = await httpClient.GetAsync(
            $"wildlife/organizations/{organizationId}/sightings{queryString}",
            ct);
        var result = await response.ReadOneOfAsync<IReadOnlyList<SightingDto>>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<int, RequestFailed>> CountAsync(
        Guid organizationId,
        SightingStatus? status,
        CancellationToken ct = default)
    {
        var queryString = new QueryStringBuilder().Add("status", status).Build();

        var response = await httpClient.GetAsync(
            $"wildlife/organizations/{organizationId}/sightings/count{queryString}",
            ct);
        var result = await response.ReadOneOfAsync<int>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<SightingDto, RequestFailed>> GetByIdAsync(
        Guid organizationId,
        Guid id,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            $"wildlife/organizations/{organizationId}/sightings/{id}",
            ct);
        var result = await response.ReadOneOfAsync<SightingDto>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<Success, ValidationFailed, RequestFailed>> ApproveAsync(
        Guid organizationId,
        Guid id,
        SightingApprovalDto dto,
        CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"wildlife/organizations/{organizationId}/sightings/{id}/approve",
            dto,
            ct);
        var problem = await response.ReadProblemAsync(ct);
        if (problem is null)
            return new Success();
        if (problem.Type == ProblemTypes.Validation)
            return ValidationFailed.From(problem);
        return RequestFailed.From(problem);
    }

    public async Task<OneOf<Success, ValidationFailed, RequestFailed>> DenyAsync(
        Guid organizationId,
        Guid id,
        SightingDenialDto dto,
        CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"wildlife/organizations/{organizationId}/sightings/{id}/deny",
            dto,
            ct);
        var problem = await response.ReadProblemAsync(ct);
        if (problem is null)
            return new Success();
        if (problem.Type == ProblemTypes.Validation)
            return ValidationFailed.From(problem);
        return RequestFailed.From(problem);
    }

    public async Task<OneOf<Success, RequestFailed>> UnapproveAsync(
        Guid organizationId,
        Guid id,
        CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync(
            $"wildlife/organizations/{organizationId}/sightings/{id}/unapprove",
            null,
            ct);
        var problem = await response.ReadProblemAsync(ct);
        if (problem is null)
            return new Success();
        return RequestFailed.From(problem);
    }
}
