using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Wildlife.Database.Models;

public sealed class SpeciesLocation
{
    public required Guid Id { get; set; }
    public required Guid SpeciesId { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public required double RadiusMeters { get; set; }
    public string? Description { get; set; }

    public Species Species { get; set; } = null!;

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SpeciesLocation>
    {
        public void Configure(EntityTypeBuilder<SpeciesLocation> builder)
        {
            builder.ToTable("species_locations");
            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Latitude).IsRequired();

            builder.Property(static e => e.Longitude).IsRequired();

            builder.Property(static e => e.RadiusMeters).IsRequired();

            builder.Property(static e => e.Description).HasMaxLength(500);

            builder
                .HasOne(static e => e.Species)
                .WithMany(static e => e.Locations)
                .HasForeignKey(static e => e.SpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasIndex(static e => e.SpeciesId)
                .HasDatabaseName("species_locations_species_id_idx");
        }
    }
}
