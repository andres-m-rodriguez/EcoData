using System.Net.Http.Json;
using EcoData.Common.Problems.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public sealed class NrcsPracticeHttpClient(HttpClient httpClient) : INrcsPracticeHttpClient
{
    public async Task<OneOf<IReadOnlyList<NrcsPracticeDtoForList>, RequestFailed>> GetAllAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync("wildlife/nrcs-practices", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var practices = await response.Content.ReadFromJsonAsync<IReadOnlyList<NrcsPracticeDtoForList>>(ct);
            if (practices is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<NrcsPracticeDtoForList>, RequestFailed>.FromT0(practices);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
