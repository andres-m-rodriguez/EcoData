using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EcoData.AquaTrack.Database;

public sealed class AquaTrackDbContextFactory : IDesignTimeDbContextFactory<AquaTrackDbContext>
{
    public AquaTrackDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AquaTrackDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=aquatrack_design;Username=postgres;Password=postgres",
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("EcoData.AquaTrack.Database");
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            });

        optionsBuilder.UseSnakeCaseNamingConvention();

        return new AquaTrackDbContext(optionsBuilder.Options);
    }
}
