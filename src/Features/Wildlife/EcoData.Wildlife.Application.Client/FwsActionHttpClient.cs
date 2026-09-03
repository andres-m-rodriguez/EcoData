using EcoData.Common.Problems;
using EcoData.Wildlife.Contracts.Dtos;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public sealed class FwsActionHttpClient(HttpClient httpClient) : IFwsActionHttpClient
{
    public async Task<OneOf<IReadOnlyList<FwsActionDtoForList>, RequestFailed>> GetListAsync(
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("wildlife/fws-actions", ct);
        var result = await response.ReadOneOfAsync<IReadOnlyList<FwsActionDtoForList>>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }
}
