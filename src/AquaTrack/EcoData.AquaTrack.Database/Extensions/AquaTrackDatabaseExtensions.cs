using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EcoData.AquaTrack.Database.Extensions;

public static class AquaTrackDatabaseExtensions
{
    public static IHostApplicationBuilder AddAquaTrackDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "aquatrack")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName);

        builder.Services.AddDbContextPool<AquaTrackDbContext>(options =>
        {
            ConfigureDbContext(connectionString, options);
        });

        builder.Services.AddPooledDbContextFactory<AquaTrackDbContext>(options =>
        {
            ConfigureDbContext(connectionString, options);
        });

        return builder;
    }

    private static void ConfigureDbContext(string? connectionString, DbContextOptionsBuilder options)
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("EcoData.AquaTrack.Database");
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5);
        });
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        options.EnableThreadSafetyChecks(false);
    }
}
