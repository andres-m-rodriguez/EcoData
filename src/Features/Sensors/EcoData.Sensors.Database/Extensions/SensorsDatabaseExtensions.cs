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
        // Use Aspire Azure EF Core integration - handles Azure AD auth automatically
        builder.AddAzureNpgsqlDbContext<SensorsDbContext>(
            connectionName,
            configureDbContextOptions: options =>
            {
                options.UseNpgsql(npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly("EcoData.Sensors.Database");
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
        builder.Services.AddDbContextFactory<SensorsDbContext>();

        return builder;
    }
}
