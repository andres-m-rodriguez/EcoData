using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using EcoData.Identity.Application.Client.HttpClients;
using EcoData.Identity.Contracts.Requests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests;

public sealed class EcoDataTestFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;
    private HttpClientHandler? _httpClientHandler;
    private ServiceProvider? _serviceProvider;
    private ServiceProvider? _domainServiceProvider;

    public IServiceProvider Services =>
        _serviceProvider ?? throw new InvalidOperationException("Fixture not initialized.");

    public IServiceProvider DomainServices =>
        _domainServiceProvider ?? throw new InvalidOperationException("Fixture not initialized.");

    public DistributedApplication App =>
        _app ?? throw new InvalidOperationException("Fixture not initialized.");

    public async Task InitializeAsync()
    {
        var appHost =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.EcoData_AppHost>();
        _app = await appHost.BuildAsync();

        var resourceNotificationService =
            _app.Services.GetRequiredService<ResourceNotificationService>();

        await _app.StartAsync();

        // Wait for ecoportal to be running
        Console.WriteLine("Waiting for ecoportal...");
        await resourceNotificationService
            .WaitForResourceAsync("ecoportal", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromMinutes(5));
        Console.WriteLine("ecoportal is running");

        // The seeder runs migrations but .WaitFor() only waits for it to start, not complete.
        // Wait for seeder to either exit or fail
        Console.WriteLine("Waiting for seeder to finish its work...");
        await resourceNotificationService
            .WaitForResourceAsync(
                "seeder",
                s => s.Snapshot.State?.Text is "Exited" or "Finished" or "FailedToStart"
                    || s.Snapshot.ExitCode.HasValue
            )
            .WaitAsync(TimeSpan.FromMinutes(5));
        Console.WriteLine("Seeder finished");

        // Get connection strings from Aspire resources
        var identityConnStr = await _app.GetConnectionStringAsync("identity");
        var organizationConnStr = await _app.GetConnectionStringAsync("organization");
        var locationsConnStr = await _app.GetConnectionStringAsync("locations");

        // Build seeding service provider with database contexts
        var seedingServices = new ServiceCollection();
        seedingServices.AddTestDatabases(organizationConnStr, locationsConnStr);
        await using var seedingProvider = seedingServices.BuildServiceProvider();

        // Seed test data
        var seeder = new TestSeeder(seedingProvider);
        var (organizations, locations) = await seeder.SeedAsync();

        // Create shared HTTP client for cookie-based auth
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

        // Build HTTP test services (API clients)
        var services = new ServiceCollection();
        services.AddIntegrationTestServices(_httpClient);
        services.AddTestStores(organizations, locations);
        _serviceProvider = services.BuildServiceProvider();

        // Build domain services (same registrations as app uses)
        var domainServices = new ServiceCollection();
        domainServices.AddDomainServices(identityConnStr, organizationConnStr);
        domainServices.AddTestStores(organizations, locations);
        _domainServiceProvider = domainServices.BuildServiceProvider();

        // Authenticate once for all tests (cookies stored in shared HttpClient)
        var authClient = _serviceProvider.GetRequiredService<IAuthHttpClient>();
        var authResult = await authClient.LoginAsync(
            new LoginRequest("admin@gmail.com", "Admin@123")
        );

        if (!authResult.IsT0)
            throw new InvalidOperationException(
                $"Failed to authenticate for test setup: {authResult.Value}"
            );
    }

    public async Task DisposeAsync()
    {
        if (_domainServiceProvider is not null)
            await _domainServiceProvider.DisposeAsync();

        if (_serviceProvider is not null)
            await _serviceProvider.DisposeAsync();

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
