using EcoData.IntegrationTests.Bases;
using EcoData.IntegrationTests.Devices;
using EcoData.IntegrationTests.Stores;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class SensorHealthTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    private ISensorsTestStore Sensors => Services.GetRequiredService<ISensorsTestStore>();
    private ISensorHealthHttpClient SensorHealthHttpClient =>
        Services.GetRequiredService<ISensorHealthHttpClient>();
    private IDeviceFactory DeviceFactory => Services.GetRequiredService<IDeviceFactory>();

    [Fact]
    public async Task GetHealthSummary_ReturnsValidSummary()
    {
        var result = await SensorHealthHttpClient.GetSummaryAsync();

        result.IsT0.Should().BeTrue("Health summary should be returned successfully");
        var summary = result.AsT0;

        summary.TotalMonitored.Should().BeGreaterThanOrEqualTo(0);
        (summary.Healthy + summary.Stale + summary.Unhealthy + summary.Unknown)
            .Should()
            .Be(summary.TotalMonitored);
    }

    [Fact]
    public async Task GetSensorHealthStatuses_ReturnsPagedResults()
    {
        var parameters = new SensorHealthParameters(PageSize: 10);
        var statuses = new List<SensorHealthStatusDtoForList>();

        await foreach (var status in SensorHealthHttpClient.GetSensorHealthStatusesAsync(parameters))
        {
            statuses.Add(status);
            if (statuses.Count >= 10)
                break;
        }

        statuses
            .Should()
            .AllSatisfy(s =>
            {
                s.SensorId.Should().NotBeEmpty();
                s.SensorName.Should().NotBeNullOrEmpty();
                s.Status.Should().NotBeNullOrEmpty();
            });
    }

    [Fact]
    public async Task GetSensorHealth_ForRegisteredSensor_ReturnsHealthStatus()
    {
        var credentials = await Sensors.GetOrCreateAsync(
            nameof(GetSensorHealth_ForRegisteredSensor_ReturnsHealthStatus)
        );

        var healthResult = await SensorHealthHttpClient.GetSensorHealthAsync(credentials.SensorId);

        healthResult.IsT0.Should().BeTrue("Health status should be returned for registered sensor");
        var health = healthResult.AsT0;
        health.SensorId.Should().Be(credentials.SensorId);
    }

    [Fact]
    public async Task GetSensorHealthConfig_ForRegisteredSensor_ReturnsConfig()
    {
        var credentials = await Sensors.GetOrCreateAsync(
            nameof(GetSensorHealthConfig_ForRegisteredSensor_ReturnsConfig)
        );

        var configResult = await SensorHealthHttpClient.GetSensorHealthConfigAsync(
            credentials.SensorId
        );

        configResult.IsT0.Should().BeTrue("Health config should be returned for registered sensor");
        var config = configResult.AsT0;
        config.SensorId.Should().Be(credentials.SensorId);
    }

    [Fact]
    public async Task SensorHealth_AfterPushingReadings_UpdatesLastReadingTime()
    {
        var credentials = await Sensors.GetOrCreateAsync(
            nameof(SensorHealth_AfterPushingReadings_UpdatesLastReadingTime)
        );

        using var device = DeviceFactory.CreateEsp32Device(
            credentials.SensorId,
            credentials.AccessToken
        );
        await device.SendSensorDataAsync(new SensorReadingDto(Temperature: 25.5, Ph: 7.0));

        await Task.Delay(500);

        var healthResult = await SensorHealthHttpClient.GetSensorHealthAsync(credentials.SensorId);

        healthResult.IsT0.Should().BeTrue("Health status should be returned");
        var health = healthResult.AsT0;
        health.LastReadingAt.Should().NotBeNull("Last reading time should be set after pushing data");
        health.LastReadingAt!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetSensorHealth_ForNonExistentSensor_ReturnsProblem()
    {
        var nonExistentId = Guid.CreateVersion7();

        var healthResult = await SensorHealthHttpClient.GetSensorHealthAsync(nonExistentId);

        healthResult.IsT1.Should().BeTrue("Should return problem for non-existent sensor");
    }
}
