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
        builder.AddKeyedAzureNpgsqlDataSource(
            connectionName,
            configureDataSourceBuilder: dsBuilder => dsBuilder.UseNetTopologySuite()
        );

        builder.Services.AddDbContextPool<LocationsDbContext>(
            (sp, options) =>
            {
                var dataSource = sp.GetRequiredKeyedService<NpgsqlDataSource>(connectionName);
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

        builder.Services.AddDbContextFactory<LocationsDbContext>();

        return builder;
    }
}
