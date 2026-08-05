using EcoData.IntegrationTests.Bases;
using EcoData.IntegrationTests.Stores;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class SensorCrudTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    private ISensorsTestStore Sensors => Services.GetRequiredService<ISensorsTestStore>();
    private ISensorHttpClient SensorHttpClient => Services.GetRequiredService<ISensorHttpClient>();
    private OrganizationsTestStore Organizations =>
        Services.GetRequiredService<OrganizationsTestStore>();
    private LocationsTestStore Locations => Services.GetRequiredService<LocationsTestStore>();

    [Fact]
    public async Task GetSensors_ReturnsPagedResults()
    {
        var parameters = new SensorParameters(PageSize: 10);
        var sensors = new List<SensorDtoForList>();

        await foreach (var sensor in SensorHttpClient.GetSensorsAsync(parameters))
        {
            sensors.Add(sensor);
            if (sensors.Count >= 10)
                break;
        }

        sensors
            .Should()
            .AllSatisfy(s =>
            {
                s.Id.Should().NotBeEmpty();
                s.Name.Should().NotBeNullOrEmpty();
            });
    }

    [Fact]
    public async Task GetSensorCount_ReturnsCount()
    {
        var parameters = new SensorParameters();

        var countResult = await SensorHttpClient.GetSensorCountAsync(parameters);

        countResult.IsT0.Should().BeTrue("Count request should succeed");
        countResult.AsT0.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetSensorById_WhenExists_ReturnsDetail()
    {
        var credentials = await Sensors.GetOrCreateAsync(nameof(GetSensorById_WhenExists_ReturnsDetail));

        var detailResult = await SensorHttpClient.GetByIdAsync(credentials.SensorId);

        detailResult.IsT0.Should().BeTrue("Sensor should be found");
        var detail = detailResult.AsT0;
        detail.Id.Should().Be(credentials.SensorId);
        detail.OrganizationId.Should().Be(Organizations.OrganizationId);
    }

    [Fact]
    public async Task GetSensorById_WhenDoesNotExist_ReturnsNotFound()
    {
        var nonExistentId = Guid.CreateVersion7();

        var detailResult = await SensorHttpClient.GetByIdAsync(nonExistentId);

        detailResult.IsT1.Should().BeTrue("Missing sensor should be a request failure");
        detailResult.AsT1.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateSensor_ChangesName()
    {
        var credentials = await Sensors.GetOrCreateAsync(nameof(UpdateSensor_ChangesName));

        var newName = $"Updated-{Guid.CreateVersion7().ToString("N")[..8]}";
        var updateRequest = new SensorDtoForUpdate(
            ExternalId: Guid.CreateVersion7().ToString(),
            Name: newName,
            Latitude: Locations.Latitude,
            Longitude: Locations.Longitude,
            MunicipalityId: Locations.MunicipalityId,
            IsActive: true
        );

        var updateResult = await SensorHttpClient.UpdateAsync(credentials.SensorId, updateRequest);

        updateResult.IsT0.Should().BeTrue("Update should succeed");
        var updated = updateResult.AsT0;
        updated.Name.Should().Be(newName);
    }

    [Fact]
    public async Task UpdateSensor_CanDeactivate()
    {
        var credentials = await Sensors.GetOrCreateAsync(nameof(UpdateSensor_CanDeactivate));

        var detailResult = await SensorHttpClient.GetByIdAsync(credentials.SensorId);
        detailResult.IsT0.Should().BeTrue("Sensor should be found");
        var detail = detailResult.AsT0;

        var updateRequest = new SensorDtoForUpdate(
            ExternalId: detail.ExternalId,
            Name: detail.Name,
            Latitude: Locations.Latitude,
            Longitude: Locations.Longitude,
            MunicipalityId: Locations.MunicipalityId,
            IsActive: false
        );

        var updateResult = await SensorHttpClient.UpdateAsync(credentials.SensorId, updateRequest);

        updateResult.IsT0.Should().BeTrue("Update should succeed");
        var updated = updateResult.AsT0;
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSensor_RemovesSensor()
    {
        // Note: This test deletes the sensor, so we create it directly without caching
        var credentials = await Sensors.GetOrCreateAsync(nameof(DeleteSensor_RemovesSensor));

        var deleteResult = await SensorHttpClient.DeleteAsync(credentials.SensorId);

        deleteResult.IsT0.Should().BeTrue("Delete should succeed");

        var detailResult = await SensorHttpClient.GetByIdAsync(credentials.SensorId);
        detailResult.IsT1.Should().BeTrue("Sensor should not be found after deletion");
        detailResult.AsT1.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteSensor_WhenDoesNotExist_ReturnsProblem()
    {
        var nonExistentId = Guid.CreateVersion7();

        var deleteResult = await SensorHttpClient.DeleteAsync(nonExistentId);

        deleteResult.IsT1.Should().BeTrue("Delete non-existent sensor should return problem");
    }

    [Fact]
    public async Task GetSensors_FilterByOrganization_ReturnsOnlyMatchingOrg()
    {
        await Sensors.GetOrCreateAsync(nameof(GetSensors_FilterByOrganization_ReturnsOnlyMatchingOrg));

        var parameters = new SensorParameters(
            PageSize: 10,
            OrganizationId: Organizations.OrganizationId
        );
        var sensors = new List<SensorDtoForList>();

        await foreach (var sensor in SensorHttpClient.GetSensorsAsync(parameters))
        {
            sensors.Add(sensor);
            if (sensors.Count >= 10)
                break;
        }

        sensors.Should().NotBeEmpty();
        sensors.Should().AllSatisfy(s => s.OrganizationId.Should().Be(Organizations.OrganizationId));
    }

    [Fact]
    public async Task GetSensors_FilterByActive_ReturnsOnlyActiveSensors()
    {
        var parameters = new SensorParameters(PageSize: 10, IsActive: true);
        var sensors = new List<SensorDtoForList>();

        await foreach (var sensor in SensorHttpClient.GetSensorsAsync(parameters))
        {
            sensors.Add(sensor);
            if (sensors.Count >= 10)
                break;
        }

        sensors.Should().AllSatisfy(s => s.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetSensors_SearchByName_ReturnsMatchingSensors()
    {
        var uniqueName = nameof(GetSensors_SearchByName_ReturnsMatchingSensors);
        await Sensors.GetOrCreateAsync(uniqueName);

        var parameters = new SensorParameters(PageSize: 10, Search: uniqueName[..10]);
        var sensors = new List<SensorDtoForList>();

        await foreach (var sensor in SensorHttpClient.GetSensorsAsync(parameters))
        {
            sensors.Add(sensor);
            if (sensors.Count >= 10)
                break;
        }

        sensors.Should().Contain(s => s.Name == uniqueName);
    }
}
