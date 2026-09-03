using EcoData.Common.Problems;
using EcoData.Wildlife.Contracts.Dtos;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public interface IFwsActionHttpClient
{
    Task<OneOf<IReadOnlyList<FwsActionDtoForList>, RequestFailed>> GetListAsync(CancellationToken ct = default);
}
