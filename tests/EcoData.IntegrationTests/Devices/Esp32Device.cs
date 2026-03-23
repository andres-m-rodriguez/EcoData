using System.Net.Http.Headers;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;

namespace EcoData.IntegrationTests.Devices;

public sealed class Esp32Device : IDisposable
{
    private readonly HttpClient _deviceHttpClient;
    private readonly ISensorReadingHttpClient _readings;

    public Esp32Device(HttpClient baseHttpClient, Guid sensorId, string accessToken)
    {
        SensorId = sensorId;
        AccessToken = accessToken;

        _deviceHttpClient = new HttpClient { BaseAddress = baseHttpClient.BaseAddress };
        _deviceHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );

        _readings = new SensorReadingHttpClient(_deviceHttpClient);
    }

    public Guid SensorId { get; }
    public string AccessToken { get; }

    public async Task SendSensorDataAsync(SensorReadingDto reading, CancellationToken ct = default)
    {
        var result = await _readings.PostReadingsAsync(SensorId, reading, ct);

        if (!result.IsT0)
        {
            var problem = result.AsT1;
            throw new InvalidOperationException($"Failed to post sensor reading: {problem.Title} - {problem.Detail}");
        }
    }

    public void Dispose() => _deviceHttpClient.Dispose();
}
