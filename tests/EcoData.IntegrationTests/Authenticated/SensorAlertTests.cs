using EcoData.IntegrationTests.Bases;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class SensorAlertTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    ISensorAlertHttpClient SensorAlertHttpClient =>
        Services.GetRequiredService<ISensorAlertHttpClient>();

    [Fact]
    public async Task GetAlerts_ReturnsPagedResults()
    {
        var parameters = new SensorHealthAlertParameters(PageSize: 10);
        var alerts = new List<SensorHealthAlertDtoForList>();

        await foreach (var alert in SensorAlertHttpClient.GetAlertsAsync(parameters))
        {
            alerts.Add(alert);
            if (alerts.Count >= 10)
                break;
        }

        alerts
            .Should()
            .AllSatisfy(a =>
            {
                a.Id.Should().NotBeEmpty();
                a.SensorId.Should().NotBeEmpty();
                a.AlertType.Should().NotBeNullOrEmpty();
                a.Message.Should().NotBeNullOrEmpty();
            });
    }

    [Fact]
    public async Task GetAlerts_FilterByResolved_ReturnsOnlyResolvedAlerts()
    {
        var parameters = new SensorHealthAlertParameters(PageSize: 10, IsResolved: true);
        var alerts = new List<SensorHealthAlertDtoForList>();

        await foreach (var alert in SensorAlertHttpClient.GetAlertsAsync(parameters))
        {
            alerts.Add(alert);
            if (alerts.Count >= 10)
                break;
        }

        alerts.Should().AllSatisfy(a => a.ResolvedAt.Should().NotBeNull());
    }

    [Fact]
    public async Task GetAlerts_FilterByUnresolved_ReturnsOnlyUnresolvedAlerts()
    {
        var parameters = new SensorHealthAlertParameters(PageSize: 10, IsResolved: false);
        var alerts = new List<SensorHealthAlertDtoForList>();

        await foreach (var alert in SensorAlertHttpClient.GetAlertsAsync(parameters))
        {
            alerts.Add(alert);
            if (alerts.Count >= 10)
                break;
        }

        alerts.Should().AllSatisfy(a => a.ResolvedAt.Should().BeNull());
    }

    [Fact]
    public async Task GetAlertById_WhenAlertExists_ReturnsDetail()
    {
        var parameters = new SensorHealthAlertParameters(PageSize: 1);
        SensorHealthAlertDtoForList? firstAlert = null;

        await foreach (var alert in SensorAlertHttpClient.GetAlertsAsync(parameters))
        {
            firstAlert = alert;
            break;
        }

        if (firstAlert is null)
        {
            // No alerts in system, skip this test
            return;
        }

        var detailResult = await SensorAlertHttpClient.GetAlertByIdAsync(firstAlert.Id);

        detailResult.IsT0.Should().BeTrue("Should return alert detail");
        var detail = detailResult.AsT0;
        detail.Id.Should().Be(firstAlert.Id);
        detail.SensorId.Should().Be(firstAlert.SensorId);
        detail.AlertType.Should().Be(firstAlert.AlertType);
    }

    [Fact]
    public async Task GetAlertById_WhenAlertDoesNotExist_ReturnsProblem()
    {
        var nonExistentId = Guid.CreateVersion7();

        var result = await SensorAlertHttpClient.GetAlertByIdAsync(nonExistentId);

        result.IsT1.Should().BeTrue("Should return problem for non-existent alert");
    }

    [Fact]
    public async Task GetAlerts_FilterByDateRange_ReturnsAlertsInRange()
    {
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;
        var parameters = new SensorHealthAlertParameters(
            PageSize: 10,
            FromDate: fromDate,
            ToDate: toDate
        );

        var alerts = new List<SensorHealthAlertDtoForList>();

        await foreach (var alert in SensorAlertHttpClient.GetAlertsAsync(parameters))
        {
            alerts.Add(alert);
            if (alerts.Count >= 10)
                break;
        }

        alerts
            .Should()
            .AllSatisfy(a =>
            {
                a.TriggeredAt.Should().BeOnOrAfter(fromDate);
                a.TriggeredAt.Should().BeOnOrBefore(toDate);
            });
    }
}
