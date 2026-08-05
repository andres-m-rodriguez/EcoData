using System.Net.Http.Json;
using EcoData.Common.Problems.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public sealed class FwsActionHttpClient(HttpClient httpClient) : IFwsActionHttpClient
{
    public async Task<OneOf<IReadOnlyList<FwsActionDtoForList>, RequestFailed>> GetAllAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync("wildlife/fws-actions", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var actions = await response.Content.ReadFromJsonAsync<IReadOnlyList<FwsActionDtoForList>>(ct);
            if (actions is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<FwsActionDtoForList>, RequestFailed>.FromT0(actions);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
