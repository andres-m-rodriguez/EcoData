using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Pagination;
using EcoData.Common.Problems.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Errors;
using EcoData.Wildlife.Contracts.Parameters;
using OneOf;

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
}
