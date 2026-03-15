using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace EcoData.Locations.Database.Extensions;

public static class LocationsDatabaseExtensions
{
    public static IHostApplicationBuilder AddLocationsDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "locations")
    {
        builder.AddAzureNpgsqlDbContext<LocationsDbContext>(connectionName,
            configureDbContextOptions: options =>
            {
                options.UseNpgsql(npgsqlOptions =>
                {
                    npgsqlOptions.UseNetTopologySuite();
                    npgsqlOptions.MigrationsAssembly("EcoData.Locations.Database");
                    npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
                });
                options.UseSnakeCaseNamingConvention();
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

        return builder;
    }
}
