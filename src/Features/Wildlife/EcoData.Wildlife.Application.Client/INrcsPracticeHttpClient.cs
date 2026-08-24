using EcoData.Common.Problems.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public interface INrcsPracticeHttpClient
{
    Task<OneOf<IReadOnlyList<NrcsPracticeDtoForList>, RequestFailed>> GetListAsync(CancellationToken ct = default);
}
