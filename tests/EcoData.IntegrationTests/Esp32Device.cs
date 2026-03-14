using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Requests;

namespace EcoData.IntegrationTests;

/// <summary>
/// Simulates an ESP32 device for integration tests using C# HTTP clients.
/// </summary>
public sealed class Esp32Device : IEsp32Device
{
    private readonly ISensorHttpClient _sensors;

    private Guid? _sensorId;
    private string? _token;

    public Esp32Device(HttpClient httpClient)
    {
        _sensors = new SensorHttpClient(httpClient);
    }

    public Guid SensorId =>
        _sensorId ?? throw new InvalidOperationException("Sensor not registered.");

    public bool IsRegistered => _sensorId.HasValue;
    public bool IsAuthenticated => _token is not null;

    public async Task<SensorRegistrationResultDto?> RegisterAsync(
        RegisterSensorRequest request,
        CancellationToken ct = default
    )
    {
        var result = await _sensors.RegisterAsync(request, ct);

        if (result is not null)
        {
            _sensorId = result.SensorId;
            _token = result.AccessToken;
        }

        return result;
    }

    public async Task SendSensorDataAsync(SensorReadingDto reading, CancellationToken ct = default)
    {
        if (_token is null)
            throw new InvalidOperationException("Sensor must be registered first.");

        var result = await _sensors.PostReadingAsync(SensorId, reading, ct);

        if (result is null)
            throw new InvalidOperationException("Failed to post sensor reading.");
    }
}
