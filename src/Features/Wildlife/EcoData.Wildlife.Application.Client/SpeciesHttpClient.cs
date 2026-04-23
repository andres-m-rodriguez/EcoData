using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;

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

    public async Task<int> GetCountAsync(
        SpeciesParameters? parameters = null,
        CancellationToken ct = default)
    {
        parameters ??= new SpeciesParameters();

        var queryString = BuildListQueryString(parameters, includePageSize: false);

        var response = await httpClient.GetAsync($"wildlife/species/count{queryString}", ct);
        if (!response.IsSuccessStatusCode) return 0;

        var payload = await response.Content.ReadFromJsonAsync<CountPayload>(ct);
        return payload?.Count ?? 0;
    }

    public async Task<SpeciesDtoForDetail?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"wildlife/species/{id}", ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<SpeciesDtoForDetail>(ct);
    }

    public async Task<IReadOnlyList<SpeciesDtoForList>> GetByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            $"wildlife/species/by-municipality/{municipalityId}",
            ct);

        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<SpeciesDtoForList>>(ct) ?? [];
    }

    public async Task<IReadOnlyList<SpeciesDtoForList>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            $"wildlife/species/by-category/{categoryId}",
            ct);

        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<SpeciesDtoForList>>(ct) ?? [];
    }

    public async Task<SpeciesStatsDto?> GetStatsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("wildlife/species/stats", ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<SpeciesStatsDto>(ct);
    }

    public async Task<SpeciesFacetsDto?> GetFacetsAsync(
        SpeciesParameters? parameters = null,
        CancellationToken ct = default)
    {
        parameters ??= new SpeciesParameters();

        var queryString = BuildListQueryString(parameters, includePageSize: false);

        var response = await httpClient.GetAsync($"wildlife/species/facets{queryString}", ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<SpeciesFacetsDto>(ct);
    }

    public async Task<IReadOnlyList<SpeciesDtoForList>> GetFeaturedAsync(
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("wildlife/species/featured", ct);

        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<SpeciesDtoForList>>(ct) ?? [];
    }

    public async Task<IReadOnlyList<MunicipalitySpeciesCountDto>> GetCountsByMunicipalityAsync(
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("wildlife/species/counts-by-municipality", ct);

        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<MunicipalitySpeciesCountDto>>(ct) ?? [];
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
