using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Wildlife.Database.Models;

public sealed class SightingNote
{
    public required Guid Id { get; set; }
    public required Guid SightingId { get; set; }

    // Soft reference to Identity users; the name is snapshotted.
    public required Guid AuthorUserId { get; set; }
    public required string AuthorDisplayName { get; set; }
    public required string Text { get; set; }
    public required DateTimeOffset CreatedAtUtc { get; set; }

    public Sighting? Sighting { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SightingNote>
    {
        public void Configure(EntityTypeBuilder<SightingNote> builder)
        {
            builder.ToTable("sighting_notes");
            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.AuthorUserId).IsRequired();

            builder.Property(static e => e.AuthorDisplayName).HasMaxLength(200).IsRequired();

            builder.Property(static e => e.Text).HasMaxLength(2000).IsRequired();

            builder
                .Property(static e => e.CreatedAtUtc)
                .HasDefaultValueSql("now()");

            builder
                .HasOne(static e => e.Sighting)
                .WithMany(static e => e.Notes)
                .HasForeignKey(static e => e.SightingId)
                .OnDelete(DeleteBehavior.Cascade);

            // The thread of one sighting in creation order.
            builder
                .HasIndex(static e => new { e.SightingId, e.Id })
                .HasDatabaseName("sighting_notes_sighting_ix");
        }
    }
}
