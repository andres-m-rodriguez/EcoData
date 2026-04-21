using EcoData.Wildlife.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Wildlife.Database;

public sealed class WildlifeDbContext(DbContextOptions<WildlifeDbContext> options) : DbContext(options)
{
    public DbSet<Species> Species => Set<Species>();
    public DbSet<SpeciesLocation> SpeciesLocations => Set<SpeciesLocation>();
    public DbSet<MunicipalitySpecies> MunicipalitySpecies => Set<MunicipalitySpecies>();
    public DbSet<SpeciesCategory> SpeciesCategories => Set<SpeciesCategory>();
    public DbSet<SpeciesCategoryLink> SpeciesCategoryLinks => Set<SpeciesCategoryLink>();
    public DbSet<FwsAction> FwsActions => Set<FwsAction>();
    public DbSet<NrcsPractice> NrcsPractices => Set<NrcsPractice>();
    public DbSet<FwsLink> FwsLinks => Set<FwsLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfiguration(new Species.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new SpeciesLocation.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new MunicipalitySpecies.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new SpeciesCategory.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new SpeciesCategoryLink.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new FwsAction.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new NrcsPractice.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new FwsLink.EntityConfiguration());
    }
}
