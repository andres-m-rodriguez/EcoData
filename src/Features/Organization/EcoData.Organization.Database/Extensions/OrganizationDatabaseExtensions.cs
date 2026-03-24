using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace EcoData.Organization.Database.Extensions;

public static class OrganizationDatabaseExtensions
{
    public static IHostApplicationBuilder AddOrganizationDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "organization"
    )
    {
        // Register keyed NpgsqlDataSource with Azure AD auth
        builder.AddKeyedAzureNpgsqlDataSource(connectionName);

        // Register pooled factory - this is the primary registration
        builder.Services.AddPooledDbContextFactory<OrganizationDbContext>(
            (sp, options) =>
            {
                var dataSource = sp.GetRequiredKeyedService<NpgsqlDataSource>(connectionName);
                ConfigureNpgsqlOptions(options, dataSource);
            }
        );

        // Register DbContext as scoped, created from the factory
        builder.Services.AddScoped<OrganizationDbContext>(
            sp => sp.GetRequiredService<IDbContextFactory<OrganizationDbContext>>().CreateDbContext()
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
                npgsqlOptions.MigrationsAssembly("EcoData.Organization.Database");
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            }
        );
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
