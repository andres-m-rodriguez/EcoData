using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Wildlife.Database.Models;

public sealed class SpeciesCategoryLink
{
    public required Guid Id { get; set; }
    public required Guid SpeciesId { get; set; }
    public required Guid CategoryId { get; set; }

    public Species Species { get; set; } = null!;
    public SpeciesCategory Category { get; set; } = null!;

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SpeciesCategoryLink>
    {
        public void Configure(EntityTypeBuilder<SpeciesCategoryLink> builder)
        {
            builder.ToTable("species_category_links");
            builder.HasKey(static e => e.Id);

            builder
                .HasOne(static e => e.Species)
                .WithMany(static e => e.CategoryLinks)
                .HasForeignKey(static e => e.SpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(static e => e.Category)
                .WithMany(static e => e.SpeciesLinks)
                .HasForeignKey(static e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasIndex(static e => new { e.SpeciesId, e.CategoryId })
                .IsUnique()
                .HasDatabaseName("species_category_links_species_category_uidx");
        }
    }
}
