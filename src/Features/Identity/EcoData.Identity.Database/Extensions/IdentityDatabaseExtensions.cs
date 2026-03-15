using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace EcoData.Identity.Database.Extensions;

public static class IdentityDatabaseExtensions
{
    public static IHostApplicationBuilder AddIdentityDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "identity")
    {
        builder.AddAzureNpgsqlDbContext<IdentityDbContext>(connectionName,
            configureDbContextOptions: options =>
            {
                options.UseNpgsql(npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly("EcoData.Identity.Database");
                    npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
                });
                options.UseSnakeCaseNamingConvention();
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

        return builder;
    }
}
