using EcoData.IntegrationTests.Bases;
using EcoData.IntegrationTests.Stores;
using EcoData.Organization.Application.Client;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Requests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class OrganizationTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    IOrganizationHttpClient OrganizationHttpClient =>
        Services.GetRequiredService<IOrganizationHttpClient>();
    ISensorHttpClient SensorHttpClient => Services.GetRequiredService<ISensorHttpClient>();
    LocationsTestStore Locations => Services.GetRequiredService<LocationsTestStore>();

    [Fact]
    public async Task DeleteOrganization_WithSensors_ReturnsConflict()
    {
        var orgName = $"Test Org Delete {Guid.CreateVersion7():N}";
        var createResult = await OrganizationHttpClient.CreateAsync(
            new OrganizationDtoForCreate(orgName, null, null, null, null)
        );
        createResult.IsT0.Should().BeTrue("Organization creation should succeed");
        var orgId = createResult.AsT0.Id;

        var registration = await SensorHttpClient.RegisterAsync(
            new RegisterSensorRequest(
                OrganizationId: orgId,
                OrganizationName: orgName,
                Name: nameof(DeleteOrganization_WithSensors_ReturnsConflict),
                ExternalId: Guid.CreateVersion7().ToString(),
                Latitude: Locations.Latitude,
                Longitude: Locations.Longitude,
                MunicipalityId: Locations.MunicipalityId
            )
        );
        registration.IsT0.Should().BeTrue("Sensor registration should succeed");

        var deleteResult = await OrganizationHttpClient.DeleteAsync(orgId);

        deleteResult.IsT1.Should().BeTrue("Delete should return ProblemDetail when sensors exist");
        var problem = deleteResult.AsT1;
        problem.Status.Should().Be(409, "Should be a conflict error");
    }

    [Fact]
    public async Task DeleteOrganization_WithoutSensors_Succeeds()
    {
        var orgName = $"Test Org Delete Empty {Guid.CreateVersion7():N}";
        var createResult = await OrganizationHttpClient.CreateAsync(
            new OrganizationDtoForCreate(orgName, null, null, null, null)
        );
        createResult.IsT0.Should().BeTrue("Organization creation should succeed");
        var orgId = createResult.AsT0.Id;

        var deleteResult = await OrganizationHttpClient.DeleteAsync(orgId);

        deleteResult.IsT0.Should().BeTrue("Delete should succeed when no sensors exist");

        var getResult = await OrganizationHttpClient.GetByIdAsync(orgId);
        getResult.IsT1.Should().BeTrue("Organization should not be found after deletion");
    }
}
