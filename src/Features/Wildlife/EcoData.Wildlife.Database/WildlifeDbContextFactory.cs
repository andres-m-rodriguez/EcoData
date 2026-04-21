using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EcoData.Wildlife.Database;

/// <summary>
/// Design-time factory for EF Core migrations.
/// </summary>
public sealed class WildlifeDbContextFactory : IDesignTimeDbContextFactory<WildlifeDbContext>
{
    public WildlifeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WildlifeDbContext>();

        // This connection string is only used for design-time operations like migrations
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=wildlife;Username=postgres;Password=postgres",
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("EcoData.Wildlife.Database");
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
                npgsqlOptions.UseNetTopologySuite();
            }
        );
        optionsBuilder.UseSnakeCaseNamingConvention();

        return new WildlifeDbContext(optionsBuilder.Options);
    }
}
