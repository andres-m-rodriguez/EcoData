using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace EcoData.Organization.Database.Extensions;

public static class OrganizationDatabaseExtensions
{
    public static IHostApplicationBuilder AddOrganizationDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "organization")
    {
        builder.AddAzureNpgsqlDbContext<OrganizationDbContext>(connectionName,
            configureDbContextOptions: options =>
            {
                options.UseNpgsql(npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly("EcoData.Organization.Database");
                    npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
                });
                options.UseSnakeCaseNamingConvention();
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

        return builder;
    }
}
