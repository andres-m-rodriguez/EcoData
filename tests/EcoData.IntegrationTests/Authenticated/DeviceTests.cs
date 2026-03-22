using EcoData.IntegrationTests.Bases;
using EcoData.IntegrationTests.Devices;
using EcoData.IntegrationTests.Stores;
using EcoData.Sensors.Contracts.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class DeviceTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    private ISensorsTestStore Sensors => Services.GetRequiredService<ISensorsTestStore>();
    private IDeviceFactory DeviceFactory => Services.GetRequiredService<IDeviceFactory>();

    [Fact]
    public async Task Device_CanRegisterAndPushReadings()
    {
        var credentials = await Sensors.GetOrCreateAsync(nameof(Device_CanRegisterAndPushReadings));

        using var device = DeviceFactory.CreateEsp32Device(
            credentials.SensorId,
            credentials.AccessToken
        );

        var reading = new SensorReadingDto(Temperature: 25.5, Ph: 7.2, DissolvedOxygen: 8.0);
        await device.SendSensorDataAsync(reading);
    }
}
