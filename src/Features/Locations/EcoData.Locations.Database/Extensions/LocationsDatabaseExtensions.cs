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
        // Use Aspire Azure EF Core integration - handles Azure AD auth automatically
        builder.AddAzureNpgsqlDbContext<LocationsDbContext>(
            connectionName,
            configureDbContextOptions: options =>
            {
                options.UseNpgsql(npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly("EcoData.Locations.Database");
                    npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
                    npgsqlOptions.UseNetTopologySuite();
                    npgsqlOptions.ConfigureDataSource(dsBuilder => dsBuilder.UseNetTopologySuite());
                });
                options.UseSnakeCaseNamingConvention();
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }
        );

        // AddAzureNpgsqlDbContext registers a pooled DbContext
        // Register IDbContextFactory for code that needs it
        builder.Services.AddDbContextFactory<LocationsDbContext>();

        return builder;
    }
}
