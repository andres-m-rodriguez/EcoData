using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EcoData.Sensors.Database.Extensions;

public static class SensorsDatabaseExtensions
{
    public static IHostApplicationBuilder AddSensorsDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "sensors")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName);

        builder.Services.AddDbContextPool<SensorsDbContext>(options =>
        {
            ConfigureDbContext(connectionString, options);
        });

        builder.Services.AddPooledDbContextFactory<SensorsDbContext>(options =>
        {
            ConfigureDbContext(connectionString, options);
        });

        return builder;
    }

    private static void ConfigureDbContext(string? connectionString, DbContextOptionsBuilder options)
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("EcoData.Sensors.Database");
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5);
        });
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        options.EnableThreadSafetyChecks(false);
    }
}
