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
        // Set environment to "Testing" before AppHost.cs executes so SEED_TEST_DATA is set on seeder
        var appHost =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.EcoData_AppHost>(
                [],
                (_, settings) => settings.EnvironmentName = "Testing"
            );
        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        await _app
            .Services.GetRequiredService<ResourceNotificationService>()
            .WaitForResourceAsync("ecoportal", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromMinutes(5));

        var identityConnStr = await _app.GetConnectionStringAsync("identity");
        var organizationConnStr = await _app.GetConnectionStringAsync("organization");
        var locationsConnStr = await _app.GetConnectionStringAsync("locations");

        //(we Also used to retrieve seeded data)
        _domainServiceProvider = new ServiceCollection()
            .AddDomainServices(identityConnStr, organizationConnStr, locationsConnStr)
            .BuildServiceProvider();

        var (organizations, locations) = await new TestSeeder(_domainServiceProvider).SeedAsync();

        using var tempClient = _app.CreateHttpClient("ecoportal", "https");
        _httpClient = ServiceCollectionExtensions.ConfigureHttpClient(tempClient.BaseAddress!);
        _serviceProvider = new ServiceCollection()
            .AddIntegrationTestServices(_httpClient)
            .AddTestStores(organizations, locations)
            .BuildServiceProvider();

        var authResult = await _serviceProvider
            .GetRequiredService<IAuthHttpClient>()
            .LoginAsync(new LoginRequest("admin@gmail.com", "Admin@123"));

        if (!authResult.IsT0)
            throw new InvalidOperationException($"Failed to authenticate: {authResult.Value}");
    }

    public async Task DisposeAsync()
    {
        if (_domainServiceProvider is not null)
            await _domainServiceProvider.DisposeAsync();
        if (_serviceProvider is not null)
            await _serviceProvider.DisposeAsync();
        _httpClient?.Dispose();
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
