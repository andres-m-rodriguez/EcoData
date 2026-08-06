using System.Globalization;
using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public sealed class SpeciesHttpClient(HttpClient httpClient) : ISpeciesHttpClient
{
    public IAsyncEnumerable<SpeciesDtoForList> GetSpeciesAsync(
        SpeciesParameters? parameters = null,
        CancellationToken ct = default)
    {
        parameters ??= new SpeciesParameters();

        var queryString = BuildListQueryString(parameters, includePageSize: true);

        return httpClient.GetFromJsonAsAsyncEnumerable<SpeciesDtoForList>(
            $"wildlife/species{queryString}",
            ct)!;
    }

    public async Task<OneOf<int, RequestFailed>> GetCountAsync(
        SpeciesParameters? parameters = null,
        CancellationToken ct = default)
    {
        parameters ??= new SpeciesParameters();

        var queryString = BuildListQueryString(parameters, includePageSize: false);

        try
        {
            var response = await httpClient.GetAsync($"wildlife/species/count{queryString}", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var payload = await response.Content.ReadFromJsonAsync<CountPayload>(ct);
            if (payload is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return payload.Count;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<SpeciesDtoForDetail, RequestFailed>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"wildlife/species/{id}", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var species = await response.Content.ReadFromJsonAsync<SpeciesDtoForDetail>(ct);
            if (species is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return species;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<IReadOnlyList<SpeciesDtoForList>, RequestFailed>> GetByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"wildlife/species/by-municipality/{municipalityId}",
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var species = await response.Content.ReadFromJsonAsync<IReadOnlyList<SpeciesDtoForList>>(ct);
            if (species is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<SpeciesDtoForList>, RequestFailed>.FromT0(species);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<IReadOnlyList<SpeciesDtoForList>, RequestFailed>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"wildlife/species/by-category/{categoryId}",
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var species = await response.Content.ReadFromJsonAsync<IReadOnlyList<SpeciesDtoForList>>(ct);
            if (species is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<SpeciesDtoForList>, RequestFailed>.FromT0(species);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<SpeciesStatsDto, RequestFailed>> GetStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync("wildlife/species/stats", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var stats = await response.Content.ReadFromJsonAsync<SpeciesStatsDto>(ct);
            if (stats is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return stats;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<SpeciesFacetsDto, RequestFailed>> GetFacetsAsync(
        SpeciesParameters? parameters = null,
        CancellationToken ct = default)
    {
        parameters ??= new SpeciesParameters();

        var queryString = BuildListQueryString(parameters, includePageSize: false);

        try
        {
            var response = await httpClient.GetAsync($"wildlife/species/facets{queryString}", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var facets = await response.Content.ReadFromJsonAsync<SpeciesFacetsDto>(ct);
            if (facets is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return facets;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<IReadOnlyList<SpeciesDtoForList>, RequestFailed>> GetFeaturedAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync("wildlife/species/featured", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var species = await response.Content.ReadFromJsonAsync<IReadOnlyList<SpeciesDtoForList>>(ct);
            if (species is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<SpeciesDtoForList>, RequestFailed>.FromT0(species);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<IReadOnlyList<MunicipalitySpeciesCountDto>, RequestFailed>> GetCountsByMunicipalityAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync("wildlife/species/counts-by-municipality", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var counts = await response.Content.ReadFromJsonAsync<IReadOnlyList<MunicipalitySpeciesCountDto>>(ct);
            if (counts is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<MunicipalitySpeciesCountDto>, RequestFailed>.FromT0(counts);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<IReadOnlyList<SpeciesNearbyDto>, RequestFailed>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        CancellationToken ct = default)
    {
        var query = new QueryStringBuilder()
            .Add("latitude", latitude.ToString(CultureInfo.InvariantCulture))
            .Add("longitude", longitude.ToString(CultureInfo.InvariantCulture))
            .Add("radiusMeters", radiusMeters.ToString(CultureInfo.InvariantCulture))
            .Build();

        try
        {
            var response = await httpClient.GetAsync($"wildlife/species/nearby{query}", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var species = await response.Content.ReadFromJsonAsync<IReadOnlyList<SpeciesNearbyDto>>(ct);
            if (species is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<SpeciesNearbyDto>, RequestFailed>.FromT0(species);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<IReadOnlyList<SpeciesNearbyDto>, RequestFailed>> GetInPolygonAsync(
        IReadOnlyList<PolygonCoordinate> coordinates,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "wildlife/species/in-polygon",
                new PolygonSearchParameters(coordinates),
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var species = await response.Content.ReadFromJsonAsync<IReadOnlyList<SpeciesNearbyDto>>(ct);
            if (species is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<SpeciesNearbyDto>, RequestFailed>.FromT0(species);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<IReadOnlyList<HeatmapPointDto>, RequestFailed>> GetHeatmapAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync("wildlife/species/heatmap", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var points = await response.Content.ReadFromJsonAsync<IReadOnlyList<HeatmapPointDto>>(ct);
            if (points is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<HeatmapPointDto>, RequestFailed>.FromT0(points);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    private static string BuildListQueryString(SpeciesParameters parameters, bool includePageSize)
    {
        var builder = new QueryStringBuilder()
            .Add("cursor", parameters.Cursor)
            .Add("search", parameters.Search)
            .Add("categoryId", parameters.CategoryId)
            .Add("municipalityId", parameters.MunicipalityId)
            .Add("isFauna", parameters.IsFauna)
            .Add("isEndemic", parameters.IsEndemic)
            .Add("hasProfileImage", parameters.HasProfileImage)
            .Add("iucnStatuses", parameters.IucnStatuses)
            .Add("taxonCodes", parameters.TaxonCodes)
            .Add("minMunicipalityCount", parameters.MinMunicipalityCount)
            .Add("observedSinceUtc", parameters.ObservedSinceUtc);

        if (parameters.Sort != SpeciesSort.ScientificNameAsc)
        {
            builder.Add<SpeciesSort>("sort", parameters.Sort);
        }

        if (includePageSize && parameters.PageSize != 20)
        {
            builder.Add("pageSize", parameters.PageSize);
        }

        return builder.Build();
    }

    private sealed record CountPayload(int Count);
}
