using EcoData.IntegrationTests.Bases;
using EcoData.IntegrationTests.Devices;
using EcoData.IntegrationTests.Stores;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class SensorStreamTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    private ISensorsTestStore Sensors => Services.GetRequiredService<ISensorsTestStore>();
    private ISensorReadingHttpClient SensorReadingHttpClient =>
        Services.GetRequiredService<ISensorReadingHttpClient>();
    private IDeviceFactory DeviceFactory => Services.GetRequiredService<IDeviceFactory>();

    [Fact]
    public async Task Stream_ReceivesReadings_WhenDevicePushesData()
    {
        var credentials = await Sensors.GetOrCreateAsync(nameof(Stream_ReceivesReadings_WhenDevicePushesData));

        using var device = DeviceFactory.CreateEsp32Device(
            credentials.SensorId,
            credentials.AccessToken
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var readings = new List<ReadingDtoForCreate>();

        var streamTask = Task.Run(
            async () =>
            {
                await foreach (
                    var r in SensorReadingHttpClient.SubscribeToReadingsAsync(
                        device.SensorId,
                        cts.Token
                    )
                )
                {
                    readings.Add(r);
                    if (readings.Count >= 1)
                        break;
                }
            },
            cts.Token
        );

        await Task.Delay(500, cts.Token);
        await device.SendSensorDataAsync(new SensorReadingDto(Temperature: 25.5));
        await streamTask;

        Assert.Single(readings);
        Assert.Equal(device.SensorId, readings[0].SensorId);
    }

    [Fact]
    public async Task Stream_MultipleSubscribers_AllReceiveReadings()
    {
        var credentials = await Sensors.GetOrCreateAsync(nameof(Stream_MultipleSubscribers_AllReceiveReadings));

        using var device = DeviceFactory.CreateEsp32Device(
            credentials.SensorId,
            credentials.AccessToken
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var readings1 = new List<ReadingDtoForCreate>();
        var readings2 = new List<ReadingDtoForCreate>();

        var task1 = Task.Run(
            async () =>
            {
                await foreach (
                    var r in SensorReadingHttpClient.SubscribeToReadingsAsync(
                        device.SensorId,
                        cts.Token
                    )
                )
                {
                    readings1.Add(r);
                    if (readings1.Count >= 1)
                        break;
                }
            },
            cts.Token
        );

        var task2 = Task.Run(
            async () =>
            {
                await foreach (
                    var r in SensorReadingHttpClient.SubscribeToReadingsAsync(
                        device.SensorId,
                        cts.Token
                    )
                )
                {
                    readings2.Add(r);
                    if (readings2.Count >= 1)
                        break;
                }
            },
            cts.Token
        );

        await Task.Delay(500, cts.Token);
        await device.SendSensorDataAsync(new SensorReadingDto(Temperature: 22.0));
        await Task.WhenAll(task1, task2);

        Assert.Single(readings1);
        Assert.Single(readings2);
    }

    [Fact]
    public async Task Stream_OnlyReceivesReadings_ForSubscribedSensor()
    {
        var credentials1 = await Sensors.GetOrCreateAsync($"{nameof(Stream_OnlyReceivesReadings_ForSubscribedSensor)}_1");
        var credentials2 = await Sensors.GetOrCreateAsync($"{nameof(Stream_OnlyReceivesReadings_ForSubscribedSensor)}_2");

        using var device1 = DeviceFactory.CreateEsp32Device(
            credentials1.SensorId,
            credentials1.AccessToken
        );
        using var device2 = DeviceFactory.CreateEsp32Device(
            credentials2.SensorId,
            credentials2.AccessToken
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var readings = new List<ReadingDtoForCreate>();

        var streamTask = Task.Run(
            async () =>
            {
                await foreach (
                    var r in SensorReadingHttpClient.SubscribeToReadingsAsync(
                        device1.SensorId,
                        cts.Token
                    )
                )
                {
                    readings.Add(r);
                    if (readings.Count >= 1)
                        break;
                }
            },
            cts.Token
        );

        await Task.Delay(500, cts.Token);
        await device2.SendSensorDataAsync(new SensorReadingDto(Temperature: 99.0));
        await Task.Delay(200, cts.Token);
        await device1.SendSensorDataAsync(new SensorReadingDto(Temperature: 20.0));
        await streamTask;

        Assert.Single(readings);
        Assert.Equal(device1.SensorId, readings[0].SensorId);
    }

    [Fact]
    public async Task Stream_ReceivesMultipleReadings_InOrder()
    {
        var credentials = await Sensors.GetOrCreateAsync(nameof(Stream_ReceivesMultipleReadings_InOrder));

        using var device = DeviceFactory.CreateEsp32Device(
            credentials.SensorId,
            credentials.AccessToken
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var readings = new List<ReadingDtoForCreate>();
        const int expectedCount = 5;

        var streamTask = Task.Run(
            async () =>
            {
                await foreach (
                    var r in SensorReadingHttpClient.SubscribeToReadingsAsync(
                        device.SensorId,
                        cts.Token
                    )
                )
                {
                    readings.Add(r);
                    if (readings.Count >= expectedCount)
                        break;
                }
            },
            cts.Token
        );

        await Task.Delay(500, cts.Token);

        // Send 5 readings with distinct temperatures
        var temperatures = new[] { 10.0, 20.0, 30.0, 40.0, 50.0 };
        foreach (var temp in temperatures)
        {
            await device.SendSensorDataAsync(new SensorReadingDto(Temperature: temp));
            await Task.Delay(100, cts.Token);
        }

        await streamTask;

        Assert.Equal(expectedCount, readings.Count);
        // Verify readings arrived in order by checking temperature values
        for (var i = 0; i < expectedCount; i++)
        {
            Assert.Equal(device.SensorId, readings[i].SensorId);
            Assert.Equal(temperatures[i], readings[i].Value);
        }
    }

    [Fact]
    public async Task Stream_HandlesHighThroughput()
    {
        var credentials = await Sensors.GetOrCreateAsync(nameof(Stream_HandlesHighThroughput));

        using var device = DeviceFactory.CreateEsp32Device(
            credentials.SensorId,
            credentials.AccessToken
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var readings = new List<ReadingDtoForCreate>();
        const int expectedCount = 10;

        var streamTask = Task.Run(
            async () =>
            {
                await foreach (
                    var r in SensorReadingHttpClient.SubscribeToReadingsAsync(
                        device.SensorId,
                        cts.Token
                    )
                )
                {
                    readings.Add(r);
                    if (readings.Count >= expectedCount)
                        break;
                }
            },
            cts.Token
        );

        // Wait for SSE connection to establish and system to be ready
        await Task.Delay(1000, cts.Token);

        // Send readings sequentially with small delays to avoid rate limits
        for (var i = 0; i < expectedCount; i++)
        {
            await device.SendSensorDataAsync(new SensorReadingDto(Temperature: 20.0 + i * 0.1));
            await Task.Delay(50, cts.Token);
        }

        await streamTask;

        Assert.Equal(expectedCount, readings.Count);
        Assert.All(readings, r => Assert.Equal(device.SensorId, r.SensorId));
    }

    [Fact]
    public async Task Stream_ReconnectsAfterDisconnect()
    {
        var credentials = await Sensors.GetOrCreateAsync(nameof(Stream_ReconnectsAfterDisconnect));

        using var device = DeviceFactory.CreateEsp32Device(
            credentials.SensorId,
            credentials.AccessToken
        );

        // First subscription
        using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var readings1 = new List<ReadingDtoForCreate>();

        var stream1Task = Task.Run(
            async () =>
            {
                await foreach (
                    var r in SensorReadingHttpClient.SubscribeToReadingsAsync(
                        device.SensorId,
                        cts1.Token
                    )
                )
                {
                    readings1.Add(r);
                    if (readings1.Count >= 1)
                        break;
                }
            },
            cts1.Token
        );

        await Task.Delay(500, cts1.Token);
        await device.SendSensorDataAsync(new SensorReadingDto(Temperature: 25.0));
        await stream1Task;

        Assert.Single(readings1);

        // Second subscription (reconnect)
        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var readings2 = new List<ReadingDtoForCreate>();

        var stream2Task = Task.Run(
            async () =>
            {
                await foreach (
                    var r in SensorReadingHttpClient.SubscribeToReadingsAsync(
                        device.SensorId,
                        cts2.Token
                    )
                )
                {
                    readings2.Add(r);
                    if (readings2.Count >= 1)
                        break;
                }
            },
            cts2.Token
        );

        await Task.Delay(500, cts2.Token);
        await device.SendSensorDataAsync(new SensorReadingDto(Temperature: 30.0));
        await stream2Task;

        Assert.Single(readings2);
        Assert.NotEqual(readings1[0].Value, readings2[0].Value);
    }
}
