using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EcoData.Identity.Database.Extensions;

public static class IdentityDatabaseExtensions
{
    public static IHostApplicationBuilder AddIdentityDatabase(
        this IHostApplicationBuilder builder,
        string connectionName = "identity")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName);

        builder.Services.AddDbContextPool<IdentityDbContext>(options =>
        {
            ConfigureDbContext(connectionString, options);
        });

        builder.Services.AddPooledDbContextFactory<IdentityDbContext>(options =>
        {
            ConfigureDbContext(connectionString, options);
        });

        return builder;
    }

    private static void ConfigureDbContext(string? connectionString, DbContextOptionsBuilder options)
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("EcoData.Identity.Database");
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5);
        });
        options.UseSnakeCaseNamingConvention();
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        options.EnableThreadSafetyChecks(false);
    }
}
