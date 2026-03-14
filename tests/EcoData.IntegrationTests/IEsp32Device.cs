using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Requests;

namespace EcoData.IntegrationTests;

public interface IEsp32Device
{
    Guid SensorId { get; }
    bool IsRegistered { get; }
    bool IsAuthenticated { get; }

    Task<SensorRegistrationResultDto?> RegisterAsync(
        RegisterSensorRequest request,
        CancellationToken ct = default
    );

    Task SendSensorDataAsync(SensorReadingDto reading, CancellationToken ct = default);
}
