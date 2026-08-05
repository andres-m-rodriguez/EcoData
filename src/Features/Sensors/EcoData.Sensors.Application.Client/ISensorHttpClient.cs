using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Errors;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.Contracts.Requests;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public interface ISensorHttpClient
{
    Task<OneOf<SensorDtoForRegistered, ValidationFailed, RequestFailed>> RegisterAsync(
        RegisterSensorRequest request,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SensorDtoForList> GetSensorsAsync(
        SensorParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<int, RequestFailed>> GetSensorCountAsync(
        SensorParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SensorDtoForDetail, RequestFailed>> GetByIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SensorDtoForDetail, ValidationFailed, RequestFailed>> UpdateAsync(
        Guid sensorId,
        SensorDtoForUpdate request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OneOf.Types.Success, RequestFailed>> DeleteAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );
}
