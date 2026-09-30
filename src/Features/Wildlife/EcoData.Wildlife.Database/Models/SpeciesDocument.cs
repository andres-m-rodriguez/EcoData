using EcoData.Common.i18n;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Wildlife.Database.Models;

// A PDF about one species. The file ships with the app under
// /documents/species/, so only its name is stored here.
public sealed class SpeciesDocument
{
    public required Guid Id { get; set; }
    public required Guid SpeciesId { get; set; }
    public required string FileName { get; set; }
    public List<LocaleValue> Title { get; set; } = [];

    public Species Species { get; set; } = null!;

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SpeciesDocument>
    {
        public void Configure(EntityTypeBuilder<SpeciesDocument> builder)
        {
            builder.ToTable("species_documents");
            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.FileName).HasMaxLength(200).IsRequired();

            builder.OwnsMany(static e => e.Title, b => b.ToJson());

            builder
                .HasOne(static e => e.Species)
                .WithMany(static e => e.Documents)
                .HasForeignKey(static e => e.SpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasIndex(static e => new { e.SpeciesId, e.FileName })
                .IsUnique()
                .HasDatabaseName("species_documents_species_id_file_name_uidx");
        }
    }
}
