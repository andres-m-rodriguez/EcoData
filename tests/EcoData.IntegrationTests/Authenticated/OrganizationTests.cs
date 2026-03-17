using EcoData.Organization.Contracts.Dtos;
using EcoData.Sensors.Contracts.Requests;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class OrganizationTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    [Fact]
    public async Task DeleteOrganization_WithSensors_ReturnsConflict()
    {
        // Create a new test organization
        var orgName = $"Test Org Delete {Guid.NewGuid():N}";
        var createResult = await OrganizationHttpClient.CreateAsync(
            new OrganizationDtoForCreate(orgName, null, null, null, null)
        );
        Assert.True(createResult.IsT0, "Organization creation should succeed");
        var orgId = createResult.AsT0.Id;

        // Register a sensor for this organization
        var device = DeviceFactory.CreateEsp32Device();
        var registration = await device.RegisterAsync(
            new RegisterSensorRequest(
                OrganizationId: orgId,
                OrganizationName: orgName,
                Name: "Test Sensor",
                ExternalId: Guid.NewGuid().ToString(),
                Latitude: 18.2208m,
                Longitude: -66.5901m,
                MunicipalityId: Guid.Empty
            )
        );
        Assert.NotNull(registration);

        // Act - try to delete the organization
        var deleteResult = await OrganizationHttpClient.DeleteAsync(orgId);

        // Assert - should return conflict error
        Assert.True(deleteResult.IsT2, "Delete should return ConflictError when sensors exist");
        var conflictError = deleteResult.AsT2;
        Assert.Contains("sensor", conflictError.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteOrganization_WithoutSensors_Succeeds()
    {
        // Create a new test organization (without sensors)
        var orgName = $"Test Org Delete Empty {Guid.NewGuid():N}";
        var createResult = await OrganizationHttpClient.CreateAsync(
            new OrganizationDtoForCreate(orgName, null, null, null, null)
        );
        Assert.True(createResult.IsT0, "Organization creation should succeed");
        var orgId = createResult.AsT0.Id;

        // Act - delete the organization
        var deleteResult = await OrganizationHttpClient.DeleteAsync(orgId);

        // Assert - should succeed
        Assert.True(deleteResult.IsT0, "Delete should succeed when no sensors exist");

        // Verify organization is deleted
        var getResult = await OrganizationHttpClient.GetByIdAsync(orgId);
        Assert.True(getResult.IsT1, "Organization should not be found after deletion");
    }
}
