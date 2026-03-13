using EcoData.Sensors.Contracts.Dtos;

namespace EcoData.Sensors.DataAccess.Interfaces;

public interface ISensorTypeRepository
{
    Task<IReadOnlyList<SensorTypeDtoForList>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    Task<SensorTypeDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<SensorTypeDtoForDetail?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default
    );
}

public interface IParameterRepository
{
    Task<IReadOnlyList<ParameterDtoForList>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<ParameterDtoForList>> GetBySensorTypeAsync(
        Guid sensorTypeId,
        CancellationToken cancellationToken = default
    );

    Task<ParameterDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<ParameterDtoForDetail?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default
    );
}
