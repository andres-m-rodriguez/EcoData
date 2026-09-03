using EcoData.Common.Problems;
using EcoData.Wildlife.Contracts.Dtos;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public sealed class SpeciesCategoryHttpClient(HttpClient httpClient) : ISpeciesCategoryHttpClient
{
    public async Task<OneOf<IReadOnlyList<SpeciesCategoryDtoForList>, RequestFailed>> GetListAsync(
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("wildlife/species-categories", ct);
        var result = await response.ReadOneOfAsync<IReadOnlyList<SpeciesCategoryDtoForList>>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<SpeciesCategoryDtoForDetail, RequestFailed>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"wildlife/species-categories/{id}", ct);
        var result = await response.ReadOneOfAsync<SpeciesCategoryDtoForDetail>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<SpeciesCategoryDtoForDetail, RequestFailed>> GetByCodeAsync(
        string code,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"wildlife/species-categories/by-code/{code}", ct);
        var result = await response.ReadOneOfAsync<SpeciesCategoryDtoForDetail>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<IReadOnlyList<TaxonFacetDto>, RequestFailed>> GetCountsAsync(
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("wildlife/species-categories/counts", ct);
        var result = await response.ReadOneOfAsync<IReadOnlyList<TaxonFacetDto>>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }
}
