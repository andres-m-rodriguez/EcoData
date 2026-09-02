using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Pagination;
using EcoData.Common.Problems.Contracts;
using EcoData.Wildlife.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Errors;
using EcoData.Wildlife.Contracts.Parameters;
using OneOf;
using OneOf.Types;

namespace EcoData.Wildlife.Application.Client;

public sealed class SightingHttpClient(HttpClient httpClient) : ISightingHttpClient
{
    public async Task<OneOf<SightingDto, ValidationFailed, RequestFailed>> ReportAsync(
        Guid organizationId,
        SightingDtoForCreate dto,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"wildlife/organizations/{organizationId}/sightings",
                dto,
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                if (problem?.Errors is { Count: > 0 } errors)
                    return new ValidationFailed(errors);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var sighting = await response.Content.ReadFromJsonAsync<SightingDto>(ct);
            if (sighting is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return sighting;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
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

    public async Task<OneOf<SightingNoteDto, ValidationFailed, RequestFailed>> AddNoteAsync(
        Guid sightingId,
        SightingNoteDtoForCreate dto,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"wildlife/sightings/{sightingId}/notes",
                dto,
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                if (problem?.Errors is { Count: > 0 } errors)
                    return new ValidationFailed(errors);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var note = await response.Content.ReadFromJsonAsync<SightingNoteDto>(ct);
            if (note is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return note;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
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

        try
        {
            var response = await httpClient.GetAsync(
                $"wildlife/organizations/{organizationId}/sightings{queryString}",
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var sightings = await response.Content.ReadFromJsonAsync<IReadOnlyList<SightingDto>>(ct);
            if (sightings is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<SightingDto>, RequestFailed>.FromT0(sightings);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<int, RequestFailed>> CountAsync(
        Guid organizationId,
        SightingStatus? status,
        CancellationToken ct = default)
    {
        var queryString = new QueryStringBuilder().Add("status", status).Build();

        try
        {
            var response = await httpClient.GetAsync(
                $"wildlife/organizations/{organizationId}/sightings/count{queryString}",
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            return await response.Content.ReadFromJsonAsync<int>(ct);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<SightingDto, RequestFailed>> GetByIdAsync(
        Guid organizationId,
        Guid id,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"wildlife/organizations/{organizationId}/sightings/{id}",
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var sighting = await response.Content.ReadFromJsonAsync<SightingDto>(ct);
            if (sighting is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return sighting;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<Success, ValidationFailed, RequestFailed>> ApproveAsync(
        Guid organizationId,
        Guid id,
        SightingReviewDto dto,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"wildlife/organizations/{organizationId}/sightings/{id}/approve",
                dto,
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                if (problem?.Errors is { Count: > 0 } errors)
                    return new ValidationFailed(errors);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            return new Success();
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<Success, ValidationFailed, RequestFailed>> DenyAsync(
        Guid organizationId,
        Guid id,
        SightingReviewDto dto,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"wildlife/organizations/{organizationId}/sightings/{id}/deny",
                dto,
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                if (problem?.Errors is { Count: > 0 } errors)
                    return new ValidationFailed(errors);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            return new Success();
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<Success, RequestFailed>> UnapproveAsync(
        Guid organizationId,
        Guid id,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsync(
                $"wildlife/organizations/{organizationId}/sightings/{id}/unapprove",
                null,
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            return new Success();
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
