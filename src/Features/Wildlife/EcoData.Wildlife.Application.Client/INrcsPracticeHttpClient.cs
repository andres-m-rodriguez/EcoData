using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.Application.Client;

public interface INrcsPracticeHttpClient
{
    Task<IReadOnlyList<NrcsPracticeDtoForList>> GetAllAsync(CancellationToken ct = default);
}
