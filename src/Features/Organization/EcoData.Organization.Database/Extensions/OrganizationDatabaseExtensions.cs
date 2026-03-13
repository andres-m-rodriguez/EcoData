using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EcoData.Organization.Database.Extensions;

public static class OrganizationDatabaseExtensions
{
    public static IHostApplicationBuilder AddOrganizationDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "organization")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName);

        builder.Services.AddDbContextPool<OrganizationDbContext>(options =>
        {
            ConfigureDbContext(connectionString, options);
        });

        builder.Services.AddPooledDbContextFactory<OrganizationDbContext>(options =>
        {
            ConfigureDbContext(connectionString, options);
        });

        return builder;
    }

    private static void ConfigureDbContext(string? connectionString, DbContextOptionsBuilder options)
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("EcoData.Organization.Database");
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5);
        });
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        options.EnableThreadSafetyChecks(false);
    }
}
