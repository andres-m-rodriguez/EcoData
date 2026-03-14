using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Errors;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.Contracts.Requests;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public interface ISensorHttpClient
{
    Task<OneOf<SensorRegistrationResultDto, ValidationError, ForbiddenError, ConflictError>> RegisterAsync(
        RegisterSensorRequest request,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SensorDtoForList> GetSensorsAsync(
        SensorParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<int> GetSensorCountAsync(
        SensorParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<SensorDtoForDetail?> GetByIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SensorDtoForDetail, ValidationError, NotFoundError, ForbiddenError>> UpdateAsync(
        Guid sensorId,
        SensorDtoForUpdate request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<bool, NotFoundError, ForbiddenError>> DeleteAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );
}
