using EcoData.Identity.Contracts.Requests;
using EcoData.Organization.Contracts.Parameters;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Requests;
using Xunit;

namespace EcoData.IntegrationTests;

[Collection(EcoDataTestCollection.Name)]
public sealed class DeviceTests
{
    private readonly EcoDataTestFixture _fixture;

    public DeviceTests(EcoDataTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData(false)] // C# mock
    // [InlineData(true)]  // C++ exe - uncomment when exe is built
    public async Task Device_CanRegisterAndPushReadings(bool useCpp)
    {
        // Arrange - login as admin
        var loginResult = await _fixture.AuthHttpClient.LoginAsync(
            new LoginRequest("admin@gmail.com", "Admin@123")
        );
        Assert.True(loginResult.Success);

        // Get or create test organization
        var existingOrg = await _fixture
            .OrganizationHttpClient.GetOrganizationsAsync(
                new OrganizationParameters(PageSize: 1, Search: "Test Org")
            )
            .FirstOrDefaultAsync();

        var orgId =
            existingOrg?.Id
            ?? (await _fixture.OrganizationHttpClient.CreateAsync(new("Test Org", null, null)))
                .AsT0
                .Id;

        // Create device and register
        var device = _fixture.CreateDevice(useCpp);

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
