using EcoData.IntegrationTests.Bases;
using EcoData.IntegrationTests.Devices;
using EcoData.IntegrationTests.Stores;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Requests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class SensorStreamTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    private ISensorHttpClient SensorHttpClient => Services.GetRequiredService<ISensorHttpClient>();
    private ISensorReadingHttpClient SensorReadingHttpClient => Services.GetRequiredService<ISensorReadingHttpClient>();
    private IDeviceFactory DeviceFactory => Services.GetRequiredService<IDeviceFactory>();
    private OrganizationsTestStore Organizations => Services.GetRequiredService<OrganizationsTestStore>();
    private LocationsTestStore Locations => Services.GetRequiredService<LocationsTestStore>();

    [Fact]
    public async Task Stream_ReceivesReadings_WhenDevicePushesData()
    {
        var registration = await SensorHttpClient.RegisterAsync(new RegisterSensorRequest(
            OrganizationId: Organizations.OrganizationId,
            OrganizationName: Organizations.OrganizationName,
            Name: $"SSE-Test-{Guid.NewGuid().ToString("N")[..8]}",
            ExternalId: Guid.NewGuid().ToString(),
            Latitude: Locations.Latitude,
            Longitude: Locations.Longitude,
            MunicipalityId: Locations.MunicipalityId));
        Assert.True(registration.IsT0, "Sensor registration failed");
        var credentials = registration.AsT0;

        using var device = DeviceFactory.CreateEsp32Device(credentials.SensorId, credentials.AccessToken);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var readings = new List<ReadingDtoForCreate>();

        var streamTask = Task.Run(async () =>
        {
            await foreach (var r in SensorReadingHttpClient.SubscribeToReadingsAsync(device.SensorId, cts.Token))
            {
                readings.Add(r);
                if (readings.Count >= 1) break;
            }
        }, cts.Token);

        await Task.Delay(500, cts.Token);
        await device.SendSensorDataAsync(new SensorReadingDto(Temperature: 25.5));
        await streamTask;

        Assert.Single(readings);
        Assert.Equal(device.SensorId, readings[0].SensorId);
    }

    [Fact]
    public async Task Stream_MultipleSubscribers_AllReceiveReadings()
    {
        var registration = await SensorHttpClient.RegisterAsync(new RegisterSensorRequest(
            OrganizationId: Organizations.OrganizationId,
            OrganizationName: Organizations.OrganizationName,
            Name: $"SSE-Multi-{Guid.NewGuid().ToString("N")[..8]}",
            ExternalId: Guid.NewGuid().ToString(),
            Latitude: Locations.Latitude,
            Longitude: Locations.Longitude,
            MunicipalityId: Locations.MunicipalityId));
        Assert.True(registration.IsT0, "Sensor registration failed");
        var credentials = registration.AsT0;

        using var device = DeviceFactory.CreateEsp32Device(credentials.SensorId, credentials.AccessToken);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var readings1 = new List<ReadingDtoForCreate>();
        var readings2 = new List<ReadingDtoForCreate>();

        var task1 = Task.Run(async () =>
        {
            await foreach (var r in SensorReadingHttpClient.SubscribeToReadingsAsync(device.SensorId, cts.Token))
            {
                readings1.Add(r);
                if (readings1.Count >= 1) break;
            }
        }, cts.Token);

        var task2 = Task.Run(async () =>
        {
            await foreach (var r in SensorReadingHttpClient.SubscribeToReadingsAsync(device.SensorId, cts.Token))
            {
                readings2.Add(r);
                if (readings2.Count >= 1) break;
            }
        }, cts.Token);

        await Task.Delay(500, cts.Token);
        await device.SendSensorDataAsync(new SensorReadingDto(Temperature: 22.0));
        await Task.WhenAll(task1, task2);

        Assert.Single(readings1);
        Assert.Single(readings2);
    }

    [Fact]
    public async Task Stream_OnlyReceivesReadings_ForSubscribedSensor()
    {
        var registration1 = await SensorHttpClient.RegisterAsync(new RegisterSensorRequest(
            OrganizationId: Organizations.OrganizationId,
            OrganizationName: Organizations.OrganizationName,
            Name: $"SSE-Iso-1-{Guid.NewGuid().ToString("N")[..8]}",
            ExternalId: Guid.NewGuid().ToString(),
            Latitude: Locations.Latitude,
            Longitude: Locations.Longitude,
            MunicipalityId: Locations.MunicipalityId));
        Assert.True(registration1.IsT0, "Sensor 1 registration failed");
        var credentials1 = registration1.AsT0;

        var registration2 = await SensorHttpClient.RegisterAsync(new RegisterSensorRequest(
            OrganizationId: Organizations.OrganizationId,
            OrganizationName: Organizations.OrganizationName,
            Name: $"SSE-Iso-2-{Guid.NewGuid().ToString("N")[..8]}",
            ExternalId: Guid.NewGuid().ToString(),
            Latitude: Locations.Latitude,
            Longitude: Locations.Longitude,
            MunicipalityId: Locations.MunicipalityId));
        Assert.True(registration2.IsT0, "Sensor 2 registration failed");
        var credentials2 = registration2.AsT0;

        using var device1 = DeviceFactory.CreateEsp32Device(credentials1.SensorId, credentials1.AccessToken);
        using var device2 = DeviceFactory.CreateEsp32Device(credentials2.SensorId, credentials2.AccessToken);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var readings = new List<ReadingDtoForCreate>();

        var streamTask = Task.Run(async () =>
        {
            await foreach (var r in SensorReadingHttpClient.SubscribeToReadingsAsync(device1.SensorId, cts.Token))
            {
                readings.Add(r);
                if (readings.Count >= 1) break;
            }
        }, cts.Token);

        await Task.Delay(500, cts.Token);
        await device2.SendSensorDataAsync(new SensorReadingDto(Temperature: 99.0));
        await Task.Delay(200, cts.Token);
        await device1.SendSensorDataAsync(new SensorReadingDto(Temperature: 20.0));
        await streamTask;

        Assert.Single(readings);
        Assert.Equal(device1.SensorId, readings[0].SensorId);
    }
}
