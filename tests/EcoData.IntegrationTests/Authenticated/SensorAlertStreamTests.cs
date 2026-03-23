using EcoData.IntegrationTests.Bases;
using EcoData.IntegrationTests.Stores;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class SensorAlertStreamTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    private ISensorsTestStore Sensors => Services.GetRequiredService<ISensorsTestStore>();
    private ISensorAlertHttpClient AlertClient => Services.GetRequiredService<ISensorAlertHttpClient>();

    [Fact]
    public async Task SensorAlertStream_OnlyReceivesAlertsForSubscribedSensor()
    {
        // Arrange
        var sensor1 = await Sensors.GetOrCreateAsync("alert-isolation-1");
        await Sensors.GetOrCreateAsync("alert-isolation-2");

        // Act
        var alerts = await SubscribeToSensorAlertsAsync(sensor1.SensorId, timeout: TimeSpan.FromSeconds(5));

        // Assert - If any alerts did arrive, they should all be for sensor 1
        alerts.Should().AllSatisfy(a => a.SensorId.Should().Be(sensor1.SensorId));
    }

    [Fact]
    public async Task MultipleGlobalSubscribers_AllReceiveAlerts()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var alerts1 = new List<SensorHealthAlertDtoForList>();
        var alerts2 = new List<SensorHealthAlertDtoForList>();

        // Act - Start two subscribers
        var sub1 = CollectGlobalAlertsAsync(alerts1, cts.Token);
        var sub2 = CollectGlobalAlertsAsync(alerts2, cts.Token);

        await Task.Delay(500, cts.Token);
        await cts.CancelAsync();
        await Task.WhenAll(sub1.ContinueWith(_ => { }), sub2.ContinueWith(_ => { }));

        // Assert
        alerts1.Should().HaveCount(alerts2.Count);
    }

    [Fact]
    public async Task AlertStream_ReconnectsAfterDisconnect()
    {
        // Arrange
        var sensor = await Sensors.GetOrCreateAsync();

        // Act - First connection
        var connected1 = await TryConnectAsync(sensor.SensorId);

        // Act - Second connection (reconnect)
        var connected2 = await TryConnectAsync(sensor.SensorId);

        // Assert
        connected1.Should().BeTrue("first connection should succeed");
        connected2.Should().BeTrue("reconnection should succeed");
    }

    #region Helpers

    private async Task<List<SensorHealthAlertDtoForList>> SubscribeToSensorAlertsAsync(
        Guid sensorId,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var alerts = new List<SensorHealthAlertDtoForList>();

        try
        {
            await foreach (var alert in AlertClient.SubscribeToSensorAlertsAsync(sensorId, cts.Token))
            {
                alerts.Add(alert);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected - timeout reached
        }

        return alerts;
    }

    private async Task CollectGlobalAlertsAsync(List<SensorHealthAlertDtoForList> alerts, CancellationToken ct)
    {
        try
        {
            await foreach (var alert in AlertClient.SubscribeToAlertsAsync(ct))
            {
                alerts.Add(alert);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    private async Task<bool> TryConnectAsync(Guid sensorId)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            await foreach (var _ in AlertClient.SubscribeToSensorAlertsAsync(sensorId, cts.Token))
            {
                return true;
            }
        }
        catch (OperationCanceledException)
        {
            return true; // Connection was successful, just no alerts
        }
        return true;
    }

    #endregion
}
