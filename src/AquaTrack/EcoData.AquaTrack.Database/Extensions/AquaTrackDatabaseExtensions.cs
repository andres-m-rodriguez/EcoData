using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace EcoData.AquaTrack.Database.Extensions;

public static class AquaTrackDatabaseExtensions
{
    public static IHostApplicationBuilder AddAquaTrackDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "aquatrack")
    {
        builder.AddNpgsqlDbContext<AquaTrackDbContext>(connectionName, configureDbContextOptions: options =>
        {
            options.UseNpgsql(npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            });
            options.UseSnakeCaseNamingConvention();
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        return builder;
    }
}
