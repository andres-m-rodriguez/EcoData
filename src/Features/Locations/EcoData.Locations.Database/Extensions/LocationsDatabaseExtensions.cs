using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EcoData.Locations.Database.Extensions;

public static class LocationsDatabaseExtensions
{
    public static IHostApplicationBuilder AddLocationsDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "locations")
    {
        // Use AddAzureNpgsqlDbContext for Entra ID auth support
        builder.AddAzureNpgsqlDbContext<LocationsDbContext>(connectionName,
            configureDbContextOptions: ConfigureOptions);

        // Also register factory - required by repositories
        builder.Services.AddPooledDbContextFactory<LocationsDbContext>((sp, options) =>
        {
            ConfigureOptions(options);
        });

        return builder;
    }

    private static void ConfigureOptions(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(npgsqlOptions =>
        {
            npgsqlOptions.UseNetTopologySuite();
            npgsqlOptions.MigrationsAssembly("EcoData.Locations.Database");
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
        });
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
