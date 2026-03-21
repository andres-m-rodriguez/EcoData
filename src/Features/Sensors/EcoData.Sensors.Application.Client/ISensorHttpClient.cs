using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.Contracts.Requests;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public interface ISensorHttpClient
{
    Task<OneOf<SensorDtoForRegistered, ProblemDetail>> RegisterAsync(
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

    Task<OneOf<SensorDtoForDetail, ProblemDetail>> UpdateAsync(
        Guid sensorId,
        SensorDtoForUpdate request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<bool, ProblemDetail>> DeleteAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );
}
