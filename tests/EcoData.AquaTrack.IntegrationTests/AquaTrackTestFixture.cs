using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using EcoData.Identity.Application.Client;
using EcoData.Identity.Contracts.Requests;
using EcoData.Organization.Application.Client;
using EcoData.Sensors.Application.Client;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.AquaTrack.IntegrationTests;

public sealed class AquaTrackTestFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;

    private IAuthHttpClient? _authClient;
    private IOrganizationHttpClient? _organizationClient;
    private ISensorHttpClient? _sensorClient;
    private ISensorHealthHttpClient? _sensorHealthClient;

    public HttpClient WebAppClient =>
        _httpClient
        ?? throw new InvalidOperationException(
            "Fixture not initialized. Ensure tests run within the AquaTrack collection."
        );

    public IAuthHttpClient Auth =>
        _authClient
        ?? throw new InvalidOperationException(
            "Fixture not initialized. Ensure tests run within the AquaTrack collection."
        );

    public IOrganizationHttpClient Organizations =>
        _organizationClient
        ?? throw new InvalidOperationException(
            "Fixture not initialized. Ensure tests run within the AquaTrack collection."
        );

    public ISensorHttpClient Sensors =>
        _sensorClient
        ?? throw new InvalidOperationException(
            "Fixture not initialized. Ensure tests run within the AquaTrack collection."
        );

    public ISensorHealthHttpClient SensorHealth =>
        _sensorHealthClient
        ?? throw new InvalidOperationException(
            "Fixture not initialized. Ensure tests run within the AquaTrack collection."
        );

    public async Task<bool> LoginAsAdminAsync(CancellationToken cancellationToken = default)
    {
        var result = await Auth.LoginAsync(
            new LoginRequest("admin@gmail.com", "Admin@123"),
            cancellationToken
        );
        return result.Success;
    }

    public async Task InitializeAsync()
    {
        var appHost =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.EcoData_AppHost>();

        _app = await appHost.BuildAsync();

        var resourceNotificationService =
            _app.Services.GetRequiredService<ResourceNotificationService>();

        await _app.StartAsync();

        await resourceNotificationService
            .WaitForResourceAsync("ecoportal", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromMinutes(5));

        _httpClient = _app.CreateHttpClient("ecoportal");

        _authClient = new AuthHttpClient(_httpClient);
        _organizationClient = new OrganizationHttpClient(_httpClient);
        _sensorClient = new SensorHttpClient(_httpClient);
        _sensorHealthClient = new SensorHealthHttpClient(_httpClient);
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class AquaTrackTestCollection : ICollectionFixture<AquaTrackTestFixture>
{
    public const string Name = "AquaTrack Integration Tests";
}
