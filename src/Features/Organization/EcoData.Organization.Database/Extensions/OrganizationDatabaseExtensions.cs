using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EcoData.Organization.Database.Extensions;

public static class OrganizationDatabaseExtensions
{
    public static IHostApplicationBuilder AddOrganizationDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "organization")
    {
        // Use AddAzureNpgsqlDbContext for Entra ID auth support
        builder.AddAzureNpgsqlDbContext<OrganizationDbContext>(connectionName,
            configureDbContextOptions: ConfigureOptions);

        // Also register factory - required by repositories
        builder.Services.AddPooledDbContextFactory<OrganizationDbContext>((sp, options) =>
        {
            ConfigureOptions(options);
        });

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
