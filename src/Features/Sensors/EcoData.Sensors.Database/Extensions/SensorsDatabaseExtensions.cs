using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace EcoData.Sensors.Database.Extensions;

public static class SensorsDatabaseExtensions
{
    public static IHostApplicationBuilder AddSensorsDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "sensors"
    )
    {
        builder.AddKeyedAzureNpgsqlDataSource(
            connectionName,
            configureDataSourceBuilder: dsBuilder => dsBuilder.UseNetTopologySuite()
        );

        builder.Services.AddDbContextPool<SensorsDbContext>(
            (sp, options) =>
            {
                var dataSource = sp.GetRequiredKeyedService<NpgsqlDataSource>(connectionName);
                options.UseNpgsql(
                    dataSource,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly("EcoData.Sensors.Database");
                        npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
                        npgsqlOptions.UseNetTopologySuite();
                    }
                );
                options.UseSnakeCaseNamingConvention();
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }
        );

        builder.Services.AddDbContextFactory<SensorsDbContext>();

        return builder;
    }
}
