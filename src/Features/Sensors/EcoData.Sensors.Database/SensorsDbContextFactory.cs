using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EcoData.Sensors.Database;

public sealed class SensorsDbContextFactory : IDesignTimeDbContextFactory<SensorsDbContext>
{
    public SensorsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SensorsDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=sensors_design;Username=postgres;Password=postgres",
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("EcoData.Sensors.Database");
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            });

        optionsBuilder.UseSnakeCaseNamingConvention();

        return new SensorsDbContext(optionsBuilder.Options);
    }
}
