using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.Application.Client;

public interface IFwsActionHttpClient
{
    Task<IReadOnlyList<FwsActionDtoForList>> GetAllAsync(CancellationToken ct = default);
}
