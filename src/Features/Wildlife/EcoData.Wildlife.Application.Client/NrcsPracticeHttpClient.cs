using EcoData.Common.Problems;
using EcoData.Wildlife.Contracts.Dtos;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public sealed class NrcsPracticeHttpClient(HttpClient httpClient) : INrcsPracticeHttpClient
{
    public async Task<OneOf<IReadOnlyList<NrcsPracticeDtoForList>, RequestFailed>> GetListAsync(
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("wildlife/nrcs-practices", ct);
        var result = await response.ReadOneOfAsync<IReadOnlyList<NrcsPracticeDtoForList>>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }
}
