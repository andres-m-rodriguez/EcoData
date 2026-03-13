using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;

namespace EcoData.Locations.DataAccess.Interfaces;

public interface IStateRepository
{
    IAsyncEnumerable<StateDtoForList> GetStatesAsync(
        StateParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<StateDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<StateDtoForDetail?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default
    );

    Task<Guid?> GetIdByCodeAsync(
        string code,
        CancellationToken cancellationToken = default
    );
}
