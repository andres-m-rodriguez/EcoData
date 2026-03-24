using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        // Use AddAzureNpgsqlDbContext for Entra ID auth support
        // This registers both the DbContext and sets up the NpgsqlDataSource with Azure AD auth
        builder.AddAzureNpgsqlDbContext<LocationsDbContext>(
            connectionName,
            configureDbContextOptions: ConfigureOptions
        );

        // Register factory using the same NpgsqlDataSource that Aspire configured
        // This ensures the factory uses Azure AD authentication in production
        builder.Services.AddPooledDbContextFactory<LocationsDbContext>(
            (sp, options) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
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
        );

        return builder;
    }

    private static void ConfigureOptions(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("EcoData.Locations.Database");
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            // Configure the data source builder with NetTopologySuite before it's built
            npgsqlOptions.ConfigureDataSource(dsBuilder => dsBuilder.UseNetTopologySuite());
            npgsqlOptions.UseNetTopologySuite();
        });
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
