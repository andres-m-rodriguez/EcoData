using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EcoData.Identity.Database.Extensions;

public static class IdentityDatabaseExtensions
{
    public static IHostApplicationBuilder AddIdentityDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "identity")
    {
        // Use AddAzureNpgsqlDbContext for Entra ID auth support
        builder.AddAzureNpgsqlDbContext<IdentityDbContext>(connectionName,
            configureDbContextOptions: ConfigureOptions);

        // Also register factory - required by repositories
        builder.Services.AddPooledDbContextFactory<IdentityDbContext>((sp, options) =>
        {
            ConfigureOptions(options);
        });

        return builder;
    }

    private static void ConfigureOptions(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("EcoData.Identity.Database");
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
        });
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
