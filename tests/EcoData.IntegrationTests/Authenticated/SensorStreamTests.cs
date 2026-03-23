using EcoData.IntegrationTests.Bases;
using EcoData.IntegrationTests.Devices;
using EcoData.IntegrationTests.Stores;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class SensorStreamTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    private ISensorsTestStore Sensors => Services.GetRequiredService<ISensorsTestStore>();
    private ISensorReadingHttpClient ReadingClient => Services.GetRequiredService<ISensorReadingHttpClient>();
    private IDeviceFactory Devices => Services.GetRequiredService<IDeviceFactory>();

    [Fact]
    public async Task Stream_ReceivesReadings_WhenDevicePushesData()
    {
        // Arrange
        var sensor = await Sensors.GetOrCreateAsync();
        using var device = Devices.CreateEsp32Device(sensor.SensorId, sensor.AccessToken);

        // Act
        var readings = await CollectReadingsAsync(device.SensorId, count: 1, async () =>
        {
            await device.SendSensorDataAsync(new SensorReadingDto(Temperature: 25.5));
        });

        // Assert
        readings.Should().ContainSingle()
            .Which.SensorId.Should().Be(device.SensorId);
    }

    [Fact]
    public async Task Stream_MultipleSubscribers_AllReceiveReadings()
    {
        // Arrange
        var sensor = await Sensors.GetOrCreateAsync();
        using var device = Devices.CreateEsp32Device(sensor.SensorId, sensor.AccessToken);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act - Start two subscribers
        var readings1 = new List<ReadingDtoForCreate>();
        var readings2 = new List<ReadingDtoForCreate>();

        var sub1 = CollectAsync(device.SensorId, readings1, count: 1, cts.Token);
        var sub2 = CollectAsync(device.SensorId, readings2, count: 1, cts.Token);

        await Task.Delay(500, cts.Token);
        await device.SendSensorDataAsync(new SensorReadingDto(Temperature: 22.0));
        await Task.WhenAll(sub1, sub2);

        // Assert
        readings1.Should().ContainSingle();
        readings2.Should().ContainSingle();
    }

    [Fact]
    public async Task Stream_OnlyReceivesReadings_ForSubscribedSensor()
    {
        // Arrange - Two sensors
        var sensor1 = await Sensors.GetOrCreateAsync("isolation-test-1");
        var sensor2 = await Sensors.GetOrCreateAsync("isolation-test-2");
        using var device1 = Devices.CreateEsp32Device(sensor1.SensorId, sensor1.AccessToken);
        using var device2 = Devices.CreateEsp32Device(sensor2.SensorId, sensor2.AccessToken);

        // Act - Subscribe to sensor1, but push to both
        var readings = await CollectReadingsAsync(device1.SensorId, count: 1, async () =>
        {
            await device2.SendSensorDataAsync(new SensorReadingDto(Temperature: 99.0)); // Should be ignored
            await Task.Delay(200);
            await device1.SendSensorDataAsync(new SensorReadingDto(Temperature: 20.0)); // Should be received
        });

        // Assert
        readings.Should().ContainSingle()
            .Which.SensorId.Should().Be(device1.SensorId);
    }

    [Fact]
    public async Task Stream_ReceivesMultipleReadings_InOrder()
    {
        // Arrange
        var sensor = await Sensors.GetOrCreateAsync();
        using var device = Devices.CreateEsp32Device(sensor.SensorId, sensor.AccessToken);
        var temperatures = new[] { 10.0, 20.0, 30.0, 40.0, 50.0 };

        // Act
        var readings = await CollectReadingsAsync(device.SensorId, count: 5, async () =>
        {
            foreach (var temp in temperatures)
            {
                await device.SendSensorDataAsync(new SensorReadingDto(Temperature: temp));
                await Task.Delay(100);
            }
        });

        // Assert
        readings.Should().HaveCount(5);
        readings.Select(r => r.Value).Should().ContainInOrder(temperatures);
    }

    [Fact]
    public async Task Stream_HandlesHighThroughput()
    {
        // Arrange
        var sensor = await Sensors.GetOrCreateAsync();
        using var device = Devices.CreateEsp32Device(sensor.SensorId, sensor.AccessToken);
        const int count = 10;

        // Act
        var readings = await CollectReadingsAsync(device.SensorId, count, async () =>
        {
            for (var i = 0; i < count; i++)
            {
                await device.SendSensorDataAsync(new SensorReadingDto(Temperature: 20.0 + i));
                await Task.Delay(50);
            }
        }, timeout: TimeSpan.FromSeconds(30));

        // Assert
        readings.Should().HaveCount(count);
        readings.Should().OnlyContain(r => r.SensorId == device.SensorId);
    }

    [Fact]
    public async Task Stream_ReconnectsAfterDisconnect()
    {
        // Arrange
        var sensor = await Sensors.GetOrCreateAsync();
        using var device = Devices.CreateEsp32Device(sensor.SensorId, sensor.AccessToken);

        // Act - First connection
        var readings1 = await CollectReadingsAsync(device.SensorId, count: 1, async () =>
        {
            await device.SendSensorDataAsync(new SensorReadingDto(Temperature: 25.0));
        });

        // Act - Second connection (reconnect)
        var readings2 = await CollectReadingsAsync(device.SensorId, count: 1, async () =>
        {
            await device.SendSensorDataAsync(new SensorReadingDto(Temperature: 30.0));
        });

        // Assert
        readings1.Should().ContainSingle();
        readings2.Should().ContainSingle();
        readings1[0].Value.Should().NotBe(readings2[0].Value);
    }

    #region Helpers

    private async Task<List<ReadingDtoForCreate>> CollectReadingsAsync(
        Guid sensorId,
        int count,
        Func<Task> sendAction,
        TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
        var readings = new List<ReadingDtoForCreate>();

        var collectTask = CollectAsync(sensorId, readings, count, cts.Token);

        await Task.Delay(500, cts.Token); // Wait for SSE connection
        await sendAction();
        await collectTask;

        return readings;
    }

    private async Task CollectAsync(
        Guid sensorId,
        List<ReadingDtoForCreate> readings,
        int count,
        CancellationToken ct)
    {
        await foreach (var reading in ReadingClient.SubscribeToReadingsAsync(sensorId, ct))
        {
            readings.Add(reading);
            if (readings.Count >= count)
                break;
        }
    }

    #endregion
}
