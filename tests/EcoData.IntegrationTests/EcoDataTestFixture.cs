using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using EcoData.Identity.Application.Client.HttpClients;
using EcoData.IntegrationTests.Devices;
using EcoData.IntegrationTests.Stores;
using EcoData.Locations.Application.Client;
using EcoData.Organization.Application.Client;
using EcoData.Sensors.Application.Client;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests;

public sealed class EcoDataTestFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;
    private HttpClientHandler? _httpClientHandler;

    private IAuthHttpClient? _authHttpClient;
    private IOrganizationHttpClient? _organizationHttpClient;
    private ISensorHttpClient? _sensorHttpClient;
    private ISensorReadingHttpClient? _sensorReadingHttpClient;
    private ISensorHealthHttpClient? _sensorHealthHttpClient;
    private IMunicipalityHttpClient? _municipalityHttpClient;
    private IDeviceFactory? _deviceFactory;
    private OrganizationsTestStore? _organizations;
    private LocationsTestStore? _locations;

    public HttpClient HttpClient =>
        _httpClient ?? throw new InvalidOperationException("Fixture not initialized.");
    public IAuthHttpClient AuthHttpClient =>
        _authHttpClient ?? throw new InvalidOperationException("Fixture not initialized.");
    public IOrganizationHttpClient OrganizationHttpClient =>
        _organizationHttpClient ?? throw new InvalidOperationException("Fixture not initialized.");
    public ISensorHttpClient SensorHttpClient =>
        _sensorHttpClient ?? throw new InvalidOperationException("Fixture not initialized.");
    public ISensorReadingHttpClient SensorReadingHttpClient =>
        _sensorReadingHttpClient ?? throw new InvalidOperationException("Fixture not initialized.");
    public ISensorHealthHttpClient SensorHealthHttpClient =>
        _sensorHealthHttpClient ?? throw new InvalidOperationException("Fixture not initialized.");
    public IMunicipalityHttpClient MunicipalityHttpClient =>
        _municipalityHttpClient ?? throw new InvalidOperationException("Fixture not initialized.");
    public IDeviceFactory DeviceFactory =>
        _deviceFactory ?? throw new InvalidOperationException("Fixture not initialized.");
    public OrganizationsTestStore Organizations =>
        _organizations ?? throw new InvalidOperationException("Fixture not initialized.");
    public LocationsTestStore Locations =>
        _locations ?? throw new InvalidOperationException("Fixture not initialized.");

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

        (_organizations, _locations) = await TestSeeder.SeedAsync(_app);

        using var tempClient = _app.CreateHttpClient("ecoportal", "https");
        var baseAddress = tempClient.BaseAddress;

        _httpClientHandler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true,
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };
        _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = baseAddress };

        _authHttpClient = new AuthHttpClient(_httpClient);
        _organizationHttpClient = new OrganizationHttpClient(_httpClient);
        _sensorHttpClient = new SensorHttpClient(_httpClient);
        _sensorReadingHttpClient = new SensorReadingHttpClient(_httpClient);
        _sensorHealthHttpClient = new SensorHealthHttpClient(_httpClient);
        _municipalityHttpClient = new MunicipalityHttpClient(_httpClient);
        _deviceFactory = new DeviceFactory(_httpClient);
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        _httpClientHandler?.Dispose();

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class EcoDataTestCollection : ICollectionFixture<EcoDataTestFixture>
{
    public const string Name = "EcoData Integration Tests";
}
