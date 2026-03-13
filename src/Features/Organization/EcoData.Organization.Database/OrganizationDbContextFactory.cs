using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EcoData.Organization.Database;

public sealed class OrganizationDbContextFactory : IDesignTimeDbContextFactory<OrganizationDbContext>
{
    public OrganizationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrganizationDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=organization_design;Username=postgres;Password=postgres",
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("EcoData.Organization.Database");
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            });

        optionsBuilder.UseSnakeCaseNamingConvention();

        return new OrganizationDbContext(optionsBuilder.Options);
    }
}
