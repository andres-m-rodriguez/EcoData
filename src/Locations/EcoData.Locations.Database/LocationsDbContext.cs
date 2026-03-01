using EcoData.Locations.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Locations.Database;

public sealed class LocationsDbContext(DbContextOptions<LocationsDbContext> options) : DbContext(options)
{
    public DbSet<State> States => Set<State>();
    public DbSet<Municipality> Municipalities => Set<Municipality>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfiguration(new State.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new Municipality.EntityConfiguration());
    }
}
