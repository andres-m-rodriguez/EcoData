using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace EcoData.Identity.Database.Extensions;

public static class IdentityDatabaseExtensions
{
    public static IHostApplicationBuilder AddIdentityDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "identity"
    )
    {
        // Use AddAzureNpgsqlDbContext for Entra ID auth support
        // This registers both the DbContext and sets up the NpgsqlDataSource with Azure AD auth
        builder.AddAzureNpgsqlDbContext<IdentityDbContext>(
            connectionName,
            configureDbContextOptions: ConfigureOptions
        );

        // Register factory using the same NpgsqlDataSource that Aspire configured
        // This ensures the factory uses Azure AD authentication in production
        builder.Services.AddPooledDbContextFactory<IdentityDbContext>(
            (sp, options) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                options.UseNpgsql(
                    dataSource,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly("EcoData.Identity.Database");
                        npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
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
            npgsqlOptions.MigrationsAssembly("EcoData.Identity.Database");
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
        });
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
