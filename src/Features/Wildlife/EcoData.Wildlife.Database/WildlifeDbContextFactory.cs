using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EcoData.Wildlife.Database;

public sealed class WildlifeDbContextFactory : IDesignTimeDbContextFactory<WildlifeDbContext>
{
    public WildlifeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WildlifeDbContext>();

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
