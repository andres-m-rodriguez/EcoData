using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.DataAccess.Interfaces;

public interface IFwsActionRepository
{
    Task<IReadOnlyList<FwsActionDtoForList>> GetAllAsync(CancellationToken cancellationToken = default);
}
