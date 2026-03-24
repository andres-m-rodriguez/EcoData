using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        // Use AddAzureNpgsqlDbContext for Entra ID auth support
        // This registers both the DbContext and sets up the NpgsqlDataSource with Azure AD auth
        builder.AddAzureNpgsqlDbContext<SensorsDbContext>(
            connectionName,
            configureDbContextOptions: ConfigureOptions
        );

        // Register factory using the same NpgsqlDataSource that Aspire configured
        // This ensures the factory uses Azure AD authentication in production
        builder.Services.AddPooledDbContextFactory<SensorsDbContext>(
            (sp, options) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
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

        return builder;
    }

    private static void ConfigureOptions(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("EcoData.Sensors.Database");
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            // Configure the data source builder with NetTopologySuite before it's built
            npgsqlOptions.ConfigureDataSource(dsBuilder => dsBuilder.UseNetTopologySuite());
            npgsqlOptions.UseNetTopologySuite();
        });
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
