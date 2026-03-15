using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace EcoData.Sensors.Database.Extensions;

public static class SensorsDatabaseExtensions
{
    public static IHostApplicationBuilder AddSensorsDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "sensors")
    {
        builder.AddAzureNpgsqlDbContext<SensorsDbContext>(connectionName,
            configureDbContextOptions: options =>
            {
                options.UseNpgsql(npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly("EcoData.Sensors.Database");
                    npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
                });
                options.UseSnakeCaseNamingConvention();
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

        return builder;
    }
}
