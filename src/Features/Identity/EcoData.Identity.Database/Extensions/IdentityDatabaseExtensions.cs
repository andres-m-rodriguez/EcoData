using Microsoft.EntityFrameworkCore;
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
        // Register keyed NpgsqlDataSource with Azure AD auth
        builder.AddKeyedAzureNpgsqlDataSource(connectionName);

        // Register pooled factory - this is the primary registration
        builder.Services.AddPooledDbContextFactory<IdentityDbContext>(
            (sp, options) =>
            {
                var dataSource = sp.GetRequiredKeyedService<NpgsqlDataSource>(connectionName);
                ConfigureNpgsqlOptions(options, dataSource);
            }
        );

        // Register DbContext as scoped, created from the factory
        builder.Services.AddScoped<IdentityDbContext>(
            sp => sp.GetRequiredService<IDbContextFactory<IdentityDbContext>>().CreateDbContext()
        );

        // Note: Aspire features (health checks, telemetry) are provided by AddKeyedAzureNpgsqlDataSource

        return builder;
    }

    private static void ConfigureNpgsqlOptions(DbContextOptionsBuilder options, NpgsqlDataSource dataSource)
    {
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
}
