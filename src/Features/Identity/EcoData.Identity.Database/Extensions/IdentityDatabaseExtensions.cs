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
        builder.AddKeyedAzureNpgsqlDataSource(connectionName);

        builder.Services.AddPooledDbContextFactory<IdentityDbContext>(
            (sp, options) =>
            {
                var dataSource = sp.GetRequiredKeyedService<NpgsqlDataSource>(connectionName);
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

        builder.Services.AddScoped<IdentityDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<IdentityDbContext>>().CreateDbContext()
        );

        return builder;
    }
}
