using EcoData.Organization.Contracts.Parameters;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Requests;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class DeviceTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    [Fact]
    public async Task Device_CanRegisterAndPushReadings()
    {
        // Get or create test organization
        var existingOrg = await OrganizationHttpClient
            .GetOrganizationsAsync(new OrganizationParameters(PageSize: 1, Search: "Test Org"))
            .FirstOrDefaultAsync();

        var orgId =
            existingOrg?.Id
            ?? (await OrganizationHttpClient.CreateAsync(new("Test Org", null, null))).AsT0.Id;

        // Create device and register
        var device = DeviceFactory.CreateEsp32Device();

        var registration = await device.RegisterAsync(
            new RegisterSensorRequest(
                OrganizationId: orgId,
                OrganizationName: "Test Org",
                Name: "Test Sensor",
                ExternalId: Guid.NewGuid().ToString(),
                Latitude: 0,
                Longitude: 0,
                MunicipalityId: Guid.Empty
            )
        );
        Assert.NotNull(registration);
        Assert.True(device.IsAuthenticated);

        // Push readings
        var reading = new SensorReadingDto(Temperature: 25.5, Ph: 7.2, DissolvedOxygen: 8.0);
        await device.SendSensorDataAsync(reading);
    }
}
