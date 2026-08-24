using EcoData.Sensors.Contracts.Dtos;

namespace EcoData.Sensors.DataAccess.Interfaces;

public interface IPhenomenonRepository
{
    IAsyncEnumerable<PhenomenonDtoForList> GetListAsync(
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<PhenomenonDtoForList> GetByCapabilityAsync(
        string capability,
        CancellationToken cancellationToken = default
    );

    Task<PhenomenonDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<PhenomenonDtoForDetail?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default
    );
}
