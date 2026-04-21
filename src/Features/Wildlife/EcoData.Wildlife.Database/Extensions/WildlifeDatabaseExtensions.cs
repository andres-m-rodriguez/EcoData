using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace EcoData.Wildlife.Database.Extensions;

public static class WildlifeDatabaseExtensions
{
    public static IHostApplicationBuilder AddWildlifeDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "wildlife"
    )
    {
        builder.AddKeyedAzureNpgsqlDataSource(
            connectionName,
            configureDataSourceBuilder: dsBuilder => dsBuilder.UseNetTopologySuite()
        );

        builder.Services.AddDbContextPool<WildlifeDbContext>(
            (sp, options) =>
            {
                var dataSource = sp.GetRequiredKeyedService<NpgsqlDataSource>(connectionName);
                options.UseNpgsql(
                    dataSource,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly("EcoData.Wildlife.Database");
                        npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
                        npgsqlOptions.UseNetTopologySuite();
                    }
                );
                options.UseSnakeCaseNamingConvention();
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }
        );

        builder.Services.AddDbContextFactory<WildlifeDbContext>();

        return builder;
    }
}
