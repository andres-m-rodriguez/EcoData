using System.Globalization;
using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems;
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

        var response = await httpClient.GetAsync($"wildlife/species/count{queryString}", ct);
        var result = await response.ReadOneOfAsync<CountPayload>(ct);
        return result.MapT0(payload => payload.Count).MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<SpeciesDtoForDetail, RequestFailed>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"wildlife/species/{id}", ct);
        var result = await response.ReadOneOfAsync<SpeciesDtoForDetail>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<SpeciesStatsDto, RequestFailed>> GetStatsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("wildlife/species/stats", ct);
        var result = await response.ReadOneOfAsync<SpeciesStatsDto>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<SpeciesFacetsDto, RequestFailed>> GetFacetsAsync(
        SpeciesParameters? parameters = null,
        CancellationToken ct = default)
    {
        parameters ??= new SpeciesParameters();

        var queryString = BuildListQueryString(parameters, includePageSize: false);

        var response = await httpClient.GetAsync($"wildlife/species/facets{queryString}", ct);
        var result = await response.ReadOneOfAsync<SpeciesFacetsDto>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<IReadOnlyList<SpeciesDtoForList>, RequestFailed>> GetFeaturedAsync(
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("wildlife/species/featured", ct);
        var result = await response.ReadOneOfAsync<IReadOnlyList<SpeciesDtoForList>>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<IReadOnlyList<MunicipalitySpeciesCountDto>, RequestFailed>> GetCountsByMunicipalityAsync(
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("wildlife/species/counts-by-municipality", ct);
        var result = await response.ReadOneOfAsync<IReadOnlyList<MunicipalitySpeciesCountDto>>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
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

        var response = await httpClient.GetAsync($"wildlife/species/nearby{query}", ct);
        var result = await response.ReadOneOfAsync<IReadOnlyList<SpeciesNearbyDto>>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<IReadOnlyList<SpeciesNearbyDto>, RequestFailed>> GetInPolygonAsync(
        IReadOnlyList<PolygonCoordinate> coordinates,
        CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "wildlife/species/in-polygon",
            new PolygonSearchParameters(coordinates),
            ct);
        var result = await response.ReadOneOfAsync<IReadOnlyList<SpeciesNearbyDto>>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    private static string BuildListQueryString(SpeciesParameters parameters, bool includePageSize)
    {
        var builder = new QueryStringBuilder()
            .Add("cursor", parameters.Cursor)
            .Add("search", parameters.Search)
            .Add("categoryId", parameters.CategoryId)
            .Add("municipalityId", parameters.MunicipalityId)
            .Add("isFauna", parameters.IsFauna)
            .Add("endemicStatuses", parameters.EndemicStatuses)
            .Add("hasProfileImage", parameters.HasProfileImage)
            .Add("iucnStatuses", parameters.IucnStatuses)
            .Add("taxonCodes", parameters.TaxonCodes)
            .Add("minMunicipalityCount", parameters.MinMunicipalityCount)
            .Add("observedSinceUtc", parameters.ObservedSinceUtc)
            .Add("nrcsPracticeCodes", parameters.NrcsPracticeCodes)
            .Add("fwsActionCodes", parameters.FwsActionCodes);

        if (parameters.Sort != SpeciesSort.ScientificNameAsc)
            builder.Add<SpeciesSort>("sort", parameters.Sort);

        if (includePageSize && parameters.PageSize != 20)
            builder.Add("pageSize", parameters.PageSize);

        return builder.Build();
    }

    private sealed record CountPayload(int Count);
}
