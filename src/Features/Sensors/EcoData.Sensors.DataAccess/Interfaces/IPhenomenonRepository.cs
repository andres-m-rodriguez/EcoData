using EcoData.Sensors.Contracts.Dtos;

namespace EcoData.Sensors.DataAccess.Interfaces;

public interface IPhenomenonRepository
{
    Task<IReadOnlyList<PhenomenonDtoForList>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PhenomenonDtoForList>> GetByCapabilityAsync(
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
