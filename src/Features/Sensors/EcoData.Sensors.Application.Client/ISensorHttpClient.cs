using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.Contracts.Requests;

namespace EcoData.Sensors.Application.Client;

public interface ISensorHttpClient
{
    Task<SensorRegistrationResultDto?> RegisterAsync(
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

    Task<ReadingBatchResult?> PostReadingAsync(
        Guid sensorId,
        SensorReadingDto reading,
        CancellationToken cancellationToken = default
    );
}
