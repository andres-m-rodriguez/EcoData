using EcoData.IntegrationTests.Bases;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Requests;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class DeviceTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    [Fact]
    public async Task Device_CanRegisterAndPushReadings()
    {
        var registration = await SensorHttpClient.RegisterAsync(new RegisterSensorRequest(
            OrganizationId: Organizations.OrganizationId,
            OrganizationName: Organizations.OrganizationName,
            Name: $"Test-Sensor-{Guid.NewGuid().ToString("N")[..8]}",
            ExternalId: Guid.NewGuid().ToString(),
            Latitude: Locations.Latitude,
            Longitude: Locations.Longitude,
            MunicipalityId: Locations.MunicipalityId));
        Assert.True(registration.IsT0, "Sensor registration failed");
        var credentials = registration.AsT0;

        using var device = DeviceFactory.CreateEsp32Device(credentials.SensorId, credentials.AccessToken);

        var reading = new SensorReadingDto(Temperature: 25.5, Ph: 7.2, DissolvedOxygen: 8.0);
        await device.SendSensorDataAsync(reading);
    }
}
