using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Wildlife.Database.Models;

public sealed class SightingImage
{
    public required Guid Id { get; set; }
    public required Guid SightingId { get; set; }
    public required string BlobName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }

    public required Guid UploadedByUserId { get; set; }
    public required string UploadedByDisplayName { get; set; }
    public required DateTimeOffset CreatedAtUtc { get; set; }

    public Sighting? Sighting { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SightingImage>
    {
        public void Configure(EntityTypeBuilder<SightingImage> builder)
        {
            builder.ToTable("sighting_images");
            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.BlobName).HasMaxLength(500).IsRequired();

            builder.Property(static e => e.ContentType).HasMaxLength(100).IsRequired();

            builder.Property(static e => e.SizeBytes).IsRequired();

            builder.Property(static e => e.UploadedByUserId).IsRequired();

            builder.Property(static e => e.UploadedByDisplayName).HasMaxLength(200).IsRequired();

            builder
                .Property(static e => e.CreatedAtUtc)
                .HasDefaultValueSql("now()");

            builder
                .HasOne(static e => e.Sighting)
                .WithMany(static e => e.Images)
                .HasForeignKey(static e => e.SightingId)
                .OnDelete(DeleteBehavior.Cascade);

            // The images of one sighting in upload order, and the per-sighting count limit.
            builder
                .HasIndex(static e => new { e.SightingId, e.Id })
                .HasDatabaseName("sighting_images_sighting_ix");
        }
    }
}
