using EcoData.Wildlife.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Wildlife.Database.Models;

public sealed class Sighting
{
    public required Guid Id { get; set; }
    public required Guid OrganizationId { get; set; }
    public required Guid SpeciesId { get; set; }
    public required Guid ReporterUserId { get; set; }
    public required string ReporterDisplayName { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public required Guid? MunicipalityId { get; set; }
    public required DateTimeOffset ObservedAtUtc { get; set; }
    public required int? IndividualCount { get; set; }
    public required SightingStatus Status { get; set; }
    public required Guid? ReviewedByUserId { get; set; }
    public required string? ReviewedByDisplayName { get; set; }
    public required DateTimeOffset? ReviewedAtUtc { get; set; }
    public required string? ReviewReason { get; set; }
    public required DateTimeOffset CreatedAtUtc { get; set; }

    public Species? Species { get; set; }
    public ICollection<SightingNote> Notes { get; set; } = [];
    public ICollection<SightingImage> Images { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Sighting>
    {
        public void Configure(EntityTypeBuilder<Sighting> builder)
        {
            builder.ToTable("sightings");
            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.OrganizationId).IsRequired();

            builder.Property(static e => e.ReporterUserId).IsRequired();

            builder.Property(static e => e.ReporterDisplayName).HasMaxLength(200).IsRequired();

            builder.Property(static e => e.Latitude).IsRequired();

            builder.Property(static e => e.Longitude).IsRequired();

            builder.Property(static e => e.ObservedAtUtc).IsRequired();

            builder
                .Property(static e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(static e => e.ReviewedByDisplayName).HasMaxLength(200);

            builder.Property(static e => e.ReviewReason).HasMaxLength(1000);

            builder
                .Property(static e => e.CreatedAtUtc)
                .HasDefaultValueSql("now()");

            builder
                .HasOne(static e => e.Species)
                .WithMany(static e => e.Sightings)
                .HasForeignKey(static e => e.SpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            // Review queue: filtered by status, paged by id descending.
            builder
                .HasIndex(static e => new { e.OrganizationId, e.Status, e.Id })
                .HasDatabaseName("sightings_org_status_ix");

            // The reporter's own list, paged by id descending.
            builder
                .HasIndex(static e => new { e.ReporterUserId, e.Id })
                .HasDatabaseName("sightings_reporter_ix");

            // Species filter in the review queue and the FK.
            builder
                .HasIndex(static e => e.SpeciesId)
                .HasDatabaseName("sightings_species_id_ix");
        }
    }
}
