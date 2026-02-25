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

        builder.Services.AddDbContext<AquaTrackDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            });
            options.UseSnakeCaseNamingConvention();
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        return builder;
    }
}
