using System.Net.Http.Json;
using EcoData.Common.Problems.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public sealed class SpeciesCategoryHttpClient(HttpClient httpClient) : ISpeciesCategoryHttpClient
{
    public async Task<OneOf<IReadOnlyList<SpeciesCategoryDtoForList>, RequestFailed>> GetListAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync("wildlife/species-categories", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var categories = await response.Content.ReadFromJsonAsync<IReadOnlyList<SpeciesCategoryDtoForList>>(ct);
            if (categories is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<SpeciesCategoryDtoForList>, RequestFailed>.FromT0(categories);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<SpeciesCategoryDtoForDetail, RequestFailed>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"wildlife/species-categories/{id}", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var category = await response.Content.ReadFromJsonAsync<SpeciesCategoryDtoForDetail>(ct);
            if (category is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return category;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<SpeciesCategoryDtoForDetail, RequestFailed>> GetByCodeAsync(
        string code,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"wildlife/species-categories/by-code/{code}", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var category = await response.Content.ReadFromJsonAsync<SpeciesCategoryDtoForDetail>(ct);
            if (category is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return category;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<IReadOnlyList<TaxonFacetDto>, RequestFailed>> GetCountsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync("wildlife/species-categories/counts", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var counts = await response.Content.ReadFromJsonAsync<IReadOnlyList<TaxonFacetDto>>(ct);
            if (counts is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<TaxonFacetDto>, RequestFailed>.FromT0(counts);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
