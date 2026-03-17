using EcoData.Identity.Application.Client.HttpClients;
using EcoData.Identity.Contracts.Requests;
using EcoData.Organization.Application.Client;
using EcoData.Sensors.Application.Client;
using Xunit;

namespace EcoData.IntegrationTests;

/// <summary>
/// Base class for tests that run without authentication.
/// </summary>
[Collection(EcoDataTestCollection.Name)]
public abstract class AnonymousTestBase
{
    protected readonly EcoDataTestFixture Fixture;

    protected AnonymousTestBase(EcoDataTestFixture fixture)
    {
        Fixture = fixture;
    }

    protected HttpClient HttpClient => Fixture.HttpClient;
    protected IAuthHttpClient AuthHttpClient => Fixture.AuthHttpClient;
    protected IOrganizationHttpClient OrganizationHttpClient => Fixture.OrganizationHttpClient;
    protected ISensorHttpClient SensorHttpClient => Fixture.SensorHttpClient;
    protected ISensorHealthHttpClient SensorHealthHttpClient => Fixture.SensorHealthHttpClient;
    protected IDeviceFactory DeviceFactory => Fixture.DeviceFactory;
}

/// <summary>
/// Base class for tests that require authentication.
/// Automatically logs in as admin before each test.
/// </summary>
[Collection(EcoDataTestCollection.Name)]
public abstract class AuthenticatedTestBase : IAsyncLifetime
{
    protected readonly EcoDataTestFixture Fixture;

    protected AuthenticatedTestBase(EcoDataTestFixture fixture)
    {
        Fixture = fixture;
    }

    protected HttpClient HttpClient => Fixture.HttpClient;
    protected IAuthHttpClient AuthHttpClient => Fixture.AuthHttpClient;
    protected IOrganizationHttpClient OrganizationHttpClient => Fixture.OrganizationHttpClient;
    protected ISensorHttpClient SensorHttpClient => Fixture.SensorHttpClient;
    protected ISensorHealthHttpClient SensorHealthHttpClient => Fixture.SensorHealthHttpClient;
    protected IDeviceFactory DeviceFactory => Fixture.DeviceFactory;

    public async Task InitializeAsync()
    {
        var result = await AuthHttpClient.LoginAsync(
            new LoginRequest("admin@gmail.com", "Admin@123")
        );

        if (!result.IsT0)
        {
            throw new InvalidOperationException("Failed to authenticate for test setup");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
