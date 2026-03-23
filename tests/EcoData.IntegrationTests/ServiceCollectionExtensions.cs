using EcoData.Identity.Application.Client.HttpClients;
using EcoData.Identity.DataAccess.Extensions;
using EcoData.Identity.Database;
using EcoData.IntegrationTests.Devices;
using EcoData.IntegrationTests.Stores;
using EcoData.Locations.Application.Client;
using EcoData.Locations.Database;
using EcoData.Organization.Application.Client;
using EcoData.Organization.DataAccess;
using EcoData.Organization.Database;
using EcoData.Sensors.Application.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.IntegrationTests;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrationTestServices(
        this IServiceCollection services,
        HttpClient httpClient
    )
    {
        // Shared HttpClient for cookie-based auth across all clients
        services.AddSingleton(httpClient);

        // HTTP clients (must share HttpClient for cookie auth)
        services.AddSingleton<IAuthHttpClient, AuthHttpClient>();
        services.AddSingleton<IOrganizationHttpClient, OrganizationHttpClient>();
        services.AddSingleton<ISensorHttpClient, SensorHttpClient>();
        services.AddSingleton<ISensorReadingHttpClient, SensorReadingHttpClient>();
        services.AddSingleton<ISensorHealthHttpClient, SensorHealthHttpClient>();
        services.AddSingleton<ISensorAlertHttpClient, SensorAlertHttpClient>();
        services.AddSingleton<IMunicipalityHttpClient, MunicipalityHttpClient>();
        services.AddSingleton<IDeviceFactory, DeviceFactory>();
        services.AddSingleton<ISensorsTestStore, SensorsTestStore>();

        return services;
    }

    public static IServiceCollection AddDomainServices(
        this IServiceCollection services,
        string identityConnectionString,
        string organizationConnectionString
    )
    {
        // Database contexts (using factory pattern like the app)
        services.AddPooledDbContextFactory<IdentityDbContext>(options =>
        {
            options.UseNpgsql(identityConnectionString);
            options.UseSnakeCaseNamingConvention();
        });

        services.AddPooledDbContextFactory<OrganizationDbContext>(options =>
        {
            options.UseNpgsql(organizationConnectionString);
            options.UseSnakeCaseNamingConvention();
        });

        // Domain services - same registrations as the app uses
        services.AddIdentityDataAccess();
        services.AddOrganizationDataAccess();

        return services;
    }

    public static IServiceCollection AddTestDatabases(
        this IServiceCollection services,
        string organizationConnectionString,
        string locationsConnectionString
    )
    {
        services.AddDbContext<OrganizationDbContext>(options =>
        {
            options.UseNpgsql(organizationConnectionString);
            options.UseSnakeCaseNamingConvention();
        });

        services.AddDbContext<LocationsDbContext>(options =>
        {
            options.UseNpgsql(locationsConnectionString, npgsql => npgsql.UseNetTopologySuite());
            options.UseSnakeCaseNamingConvention();
        });

        return services;
    }

    public static IServiceCollection AddTestStores(
        this IServiceCollection services,
        OrganizationsTestStore organizations,
        LocationsTestStore locations
    )
    {
        services.AddSingleton(organizations);
        services.AddSingleton(locations);
        return services;
    }
}
