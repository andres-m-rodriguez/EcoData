using EcoData.IntegrationTests.Bases;
using EcoData.IntegrationTests.Stores;
using EcoData.Organization.Application.Client;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Requests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class OrganizationTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    private IOrganizationHttpClient OrganizationHttpClient => Services.GetRequiredService<IOrganizationHttpClient>();
    private ISensorHttpClient SensorHttpClient => Services.GetRequiredService<ISensorHttpClient>();
    private LocationsTestStore Locations => Services.GetRequiredService<LocationsTestStore>();

    [Fact]
    public async Task DeleteOrganization_WithSensors_ReturnsConflict()
    {
        var orgName = $"Test Org Delete {Guid.NewGuid():N}";
        var createResult = await OrganizationHttpClient.CreateAsync(
            new OrganizationDtoForCreate(orgName, null, null, null, null));
        Assert.True(createResult.IsT0, "Organization creation should succeed");
        var orgId = createResult.AsT0.Id;

        var registration = await SensorHttpClient.RegisterAsync(new RegisterSensorRequest(
            OrganizationId: orgId,
            OrganizationName: orgName,
            Name: "Test Sensor",
            ExternalId: Guid.NewGuid().ToString(),
            Latitude: Locations.Latitude,
            Longitude: Locations.Longitude,
            MunicipalityId: Locations.MunicipalityId));
        Assert.True(registration.IsT0, "Sensor registration should succeed");

        var deleteResult = await OrganizationHttpClient.DeleteAsync(orgId);

        Assert.True(deleteResult.IsT2, "Delete should return ConflictError when sensors exist");
        var conflictError = deleteResult.AsT2;
        Assert.Contains("sensor", conflictError.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteOrganization_WithoutSensors_Succeeds()
    {
        var orgName = $"Test Org Delete Empty {Guid.NewGuid():N}";
        var createResult = await OrganizationHttpClient.CreateAsync(
            new OrganizationDtoForCreate(orgName, null, null, null, null));
        Assert.True(createResult.IsT0, "Organization creation should succeed");
        var orgId = createResult.AsT0.Id;

        var deleteResult = await OrganizationHttpClient.DeleteAsync(orgId);

        Assert.True(deleteResult.IsT0, "Delete should succeed when no sensors exist");

        var getResult = await OrganizationHttpClient.GetByIdAsync(orgId);
        Assert.True(getResult.IsT1, "Organization should not be found after deletion");
    }
}
