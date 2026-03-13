using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EcoData.Locations.Database.Extensions;

public static class LocationsDatabaseExtensions
{
    public static IHostApplicationBuilder AddLocationsDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "locations")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName);

        builder.Services.AddDbContextPool<LocationsDbContext>(options =>
        {
            ConfigureDbContext(connectionString, options);
        });

        builder.Services.AddPooledDbContextFactory<LocationsDbContext>(options =>
        {
            ConfigureDbContext(connectionString, options);
        });

        return builder;
    }

    private static void ConfigureDbContext(string? connectionString, DbContextOptionsBuilder options)
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.UseNetTopologySuite();
            npgsqlOptions.MigrationsAssembly("EcoData.Locations.Database");
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5);
        });
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        options.EnableThreadSafetyChecks(false);
    }
}
