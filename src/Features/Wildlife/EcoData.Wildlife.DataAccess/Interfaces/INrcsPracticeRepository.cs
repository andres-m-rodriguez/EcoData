using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.DataAccess.Interfaces;

public interface INrcsPracticeRepository
{
    Task<IReadOnlyList<NrcsPracticeDtoForList>> GetListAsync(CancellationToken cancellationToken = default);
}
