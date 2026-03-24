using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace EcoData.Locations.Database.Extensions;

public static class LocationsDatabaseExtensions
{
    public static IHostApplicationBuilder AddLocationsDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "locations"
    )
    {
        // Register keyed NpgsqlDataSource with Azure AD auth and NetTopologySuite
        builder.AddKeyedAzureNpgsqlDataSource(
            connectionName,
            configureDataSourceBuilder: dsBuilder => dsBuilder.UseNetTopologySuite()
        );

        // Register pooled factory - this is the primary registration
        builder.Services.AddPooledDbContextFactory<LocationsDbContext>(
            (sp, options) =>
            {
                var dataSource = sp.GetRequiredKeyedService<NpgsqlDataSource>(connectionName);
                ConfigureNpgsqlOptions(options, dataSource);
            }
        );

        // Register DbContext as scoped, created from the factory
        builder.Services.AddScoped<LocationsDbContext>(
            sp => sp.GetRequiredService<IDbContextFactory<LocationsDbContext>>().CreateDbContext()
        );

        // Note: Aspire features (health checks, telemetry) are provided by AddKeyedAzureNpgsqlDataSource

        return builder;
    }

    private static void ConfigureNpgsqlOptions(DbContextOptionsBuilder options, NpgsqlDataSource dataSource)
    {
        options.UseNpgsql(
            dataSource,
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("EcoData.Locations.Database");
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
                npgsqlOptions.UseNetTopologySuite();
            }
        );
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
