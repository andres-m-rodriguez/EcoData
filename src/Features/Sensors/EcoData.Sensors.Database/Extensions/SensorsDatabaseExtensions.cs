using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EcoData.Sensors.Database.Extensions;

public static class SensorsDatabaseExtensions
{
    public static IHostApplicationBuilder AddSensorsDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "sensors")
    {
        // Use AddAzureNpgsqlDbContext for Entra ID auth support
        builder.AddAzureNpgsqlDbContext<SensorsDbContext>(connectionName,
            configureDbContextOptions: ConfigureOptions);

        // Also register factory - required by repositories
        builder.Services.AddPooledDbContextFactory<SensorsDbContext>((sp, options) =>
        {
            ConfigureOptions(options);
        });

        return builder;
    }

    private static void ConfigureOptions(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("EcoData.Sensors.Database");
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            npgsqlOptions.UseNetTopologySuite();
        });
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
