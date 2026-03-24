using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        // Use AddAzureNpgsqlDbContext for Entra ID auth support
        // This registers both the DbContext and sets up the NpgsqlDataSource with Azure AD auth
        builder.AddAzureNpgsqlDbContext<OrganizationDbContext>(
            connectionName,
            configureDbContextOptions: ConfigureOptions
        );

        // Register factory using the same NpgsqlDataSource that Aspire configured
        // This ensures the factory uses Azure AD authentication in production
        builder.Services.AddPooledDbContextFactory<OrganizationDbContext>(
            (sp, options) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
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
        );

        return builder;
    }

    private static void ConfigureOptions(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("EcoData.Organization.Database");
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
        });
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
