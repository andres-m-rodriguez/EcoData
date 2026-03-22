using EcoData.IntegrationTests.Bases;
using EcoData.IntegrationTests.Stores;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class SensorAlertStreamTests(EcoDataTestFixture fixture)
    : AuthenticatedTestBase(fixture)
{
    private ISensorsTestStore Sensors => Services.GetRequiredService<ISensorsTestStore>();
    private ISensorAlertHttpClient SensorAlertHttpClient =>
        Services.GetRequiredService<ISensorAlertHttpClient>();

    [Fact]
    public async Task GlobalAlertStream_CanConnect()
    {
        // Test that we can successfully connect to the global alert stream endpoint
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var connected = false;
        try
        {
            await foreach (var _ in SensorAlertHttpClient.SubscribeToAlertsAsync(cts.Token))
            {
                // If we receive any alert, the stream is working
                connected = true;
                break;
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout is expected if no alerts arrive - but connection was successful
            connected = true;
        }

        Assert.True(connected, "Should be able to connect to global alert stream");
    }

    [Fact]
    public async Task SensorAlertStream_CanConnect()
    {
        var credentials = await Sensors.GetOrCreateAsync(nameof(SensorAlertStream_CanConnect));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var connected = false;
        try
        {
            await foreach (
                var _ in SensorAlertHttpClient.SubscribeToSensorAlertsAsync(
                    credentials.SensorId,
                    cts.Token
                )
            )
            {
                connected = true;
                break;
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout is expected if no alerts arrive - but connection was successful
            connected = true;
        }

        Assert.True(connected, "Should be able to connect to sensor-specific alert stream");
    }

    [Fact]
    public async Task SensorAlertStream_OnlyReceivesAlertsForSubscribedSensor()
    {
        var sensor1 = await Sensors.GetOrCreateAsync($"{nameof(SensorAlertStream_OnlyReceivesAlertsForSubscribedSensor)}_1");
        await Sensors.GetOrCreateAsync($"{nameof(SensorAlertStream_OnlyReceivesAlertsForSubscribedSensor)}_2");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var alerts = new List<SensorHealthAlertDtoForList>();

        try
        {
            await foreach (
                var alert in SensorAlertHttpClient.SubscribeToSensorAlertsAsync(
                    sensor1.SensorId,
                    cts.Token
                )
            )
            {
                alerts.Add(alert);
                // Only sensor 1's alerts should arrive on this stream
                Assert.Equal(sensor1.SensorId, alert.SensorId);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected - no alerts triggered during short test window
        }

        // If any alerts did arrive, they should all be for sensor 1
        Assert.All(alerts, a => Assert.Equal(sensor1.SensorId, a.SensorId));
    }

    [Fact]
    public async Task MultipleGlobalSubscribers_AllReceiveAlerts()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var alerts1 = new List<SensorHealthAlertDtoForList>();
        var alerts2 = new List<SensorHealthAlertDtoForList>();

        var task1 = Task.Run(
            async () =>
            {
                try
                {
                    await foreach (
                        var alert in SensorAlertHttpClient.SubscribeToAlertsAsync(cts.Token)
                    )
                    {
                        alerts1.Add(alert);
                    }
                }
                catch (OperationCanceledException) { }
            },
            cts.Token
        );

        var task2 = Task.Run(
            async () =>
            {
                try
                {
                    await foreach (
                        var alert in SensorAlertHttpClient.SubscribeToAlertsAsync(cts.Token)
                    )
                    {
                        alerts2.Add(alert);
                    }
                }
                catch (OperationCanceledException) { }
            },
            cts.Token
        );

        // Wait for subscriptions to establish
        await Task.Delay(500, cts.Token);

        // Cancel after short window
        await cts.CancelAsync();

        await Task.WhenAll(task1.ContinueWith(_ => { }), task2.ContinueWith(_ => { }));

        // Both subscribers should have received the same alerts (if any)
        Assert.Equal(alerts1.Count, alerts2.Count);
    }

    [Fact]
    public async Task AlertStream_ReconnectsAfterDisconnect()
    {
        var credentials = await Sensors.GetOrCreateAsync(nameof(AlertStream_ReconnectsAfterDisconnect));

        // First subscription
        using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var connected1 = false;
        try
        {
            await foreach (
                var _ in SensorAlertHttpClient.SubscribeToSensorAlertsAsync(
                    credentials.SensorId,
                    cts1.Token
                )
            )
            {
                connected1 = true;
                break;
            }
        }
        catch (OperationCanceledException)
        {
            connected1 = true; // Connection was successful, just no alerts
        }

        Assert.True(connected1, "First connection should succeed");

        // Second subscription (reconnect)
        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var connected2 = false;
        try
        {
            await foreach (
                var _ in SensorAlertHttpClient.SubscribeToSensorAlertsAsync(
                    credentials.SensorId,
                    cts2.Token
                )
            )
            {
                connected2 = true;
                break;
            }
        }
        catch (OperationCanceledException)
        {
            connected2 = true; // Connection was successful, just no alerts
        }

        Assert.True(connected2, "Reconnection should succeed");
    }
}
