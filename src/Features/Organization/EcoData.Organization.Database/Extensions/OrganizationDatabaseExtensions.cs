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
        builder.AddKeyedAzureNpgsqlDataSource(connectionName);

        builder.Services.AddPooledDbContextFactory<OrganizationDbContext>(
            (sp, options) =>
            {
                var dataSource = sp.GetRequiredKeyedService<NpgsqlDataSource>(connectionName);
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

        builder.Services.AddScoped<OrganizationDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<OrganizationDbContext>>().CreateDbContext()
        );

        return builder;
    }
}
