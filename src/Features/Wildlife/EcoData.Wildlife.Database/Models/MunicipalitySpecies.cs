using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Wildlife.Database.Models;

/// <summary>
/// Junction table linking species to municipalities where they are found.
/// MunicipalityId references EcoData.Locations.Municipality (cross-module reference by ID only).
/// </summary>
public sealed class MunicipalitySpecies
{
    public required Guid Id { get; set; }

    // Cross-module reference to Locations.Municipality by ID only
    public required Guid MunicipalityId { get; set; }
    public required Guid SpeciesId { get; set; }

    public Species Species { get; set; } = null!;

    public sealed class EntityConfiguration : IEntityTypeConfiguration<MunicipalitySpecies>
    {
        public void Configure(EntityTypeBuilder<MunicipalitySpecies> builder)
        {
            builder.ToTable("municipality_species");
            builder.HasKey(static e => e.Id);

            builder
                .HasOne(static e => e.Species)
                .WithMany(static s => s.MunicipalitySpecies)
                .HasForeignKey(static e => e.SpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            // Note: No navigation property to Municipality - cross-module reference by ID only

            builder
                .HasIndex(static e => new { e.MunicipalityId, e.SpeciesId })
                .IsUnique()
                .HasDatabaseName("municipality_species_municipality_species_uidx");

            builder
                .HasIndex(static e => e.MunicipalityId)
                .HasDatabaseName("municipality_species_municipality_id_idx");
        }
    }
}
