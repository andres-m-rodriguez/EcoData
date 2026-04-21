using EcoData.Wildlife.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace EcoData.Wildlife.Database.Models;

/// <summary>
/// Represents a wildlife sighting reported by a user.
/// </summary>
public sealed class Sighting
{
    public required Guid Id { get; set; }

    // Species reference - either verified species or user-submitted species
    public required Guid? SpeciesId { get; set; }
    public required Guid? UserSpeciesId { get; set; }

    // Sighting details
    public required SightingMode Mode { get; set; }
    public required ConfidenceLevel Confidence { get; set; }
    public required CountRange Count { get; set; }
    public required Behavior Behaviors { get; set; }
    public required EvidenceType Evidence { get; set; }
    public required Weather? Weather { get; set; }
    public required string? Notes { get; set; }

    // Location data
    public required Point Location { get; set; }
    // Cross-module reference to Locations.Municipality by ID only
    public required Guid? MunicipalityId { get; set; }

    // Timestamp
    public required DateTimeOffset ObservedAt { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    // Media
    public required byte[]? PhotoData { get; set; }
    public required string? PhotoContentType { get; set; }
    public required byte[]? AudioData { get; set; }
    public required string? AudioContentType { get; set; }

    // Review status
    public required SightingStatus Status { get; set; }
    public required bool IsFlaggedForReview { get; set; }
    public required bool IsNewMunicipalityRecord { get; set; }
    public required string? ReviewNotes { get; set; }
    public required DateTimeOffset? ReviewedAt { get; set; }
    // Cross-module reference to Identity.User by ID only
    public required Guid? ReviewedByUserId { get; set; }

    // Cross-module reference to Identity.User by ID only
    public required Guid ReportedByUserId { get; set; }

    // Navigation properties within Wildlife feature only
    public Species? Species { get; set; }
    public UserSpecies? UserSpecies { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Sighting>
    {
        public void Configure(EntityTypeBuilder<Sighting> builder)
        {
            builder.ToTable("sightings");
            builder.HasKey(static e => e.Id);

            builder
                .Property(static e => e.Mode)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder
                .Property(static e => e.Confidence)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder
                .Property(static e => e.Count)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(static e => e.Behaviors).IsRequired();

            builder.Property(static e => e.Evidence).IsRequired();

            builder.Property(static e => e.Weather).HasConversion<string>().HasMaxLength(20);

            builder.Property(static e => e.Notes).HasMaxLength(2000);

            builder
                .Property(static e => e.Location)
                .HasColumnType("geometry(Point, 4326)")
                .IsRequired();

            builder.Property(static e => e.PhotoContentType).HasMaxLength(100);

            builder.Property(static e => e.AudioContentType).HasMaxLength(100);

            builder
                .Property(static e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(static e => e.ReviewNotes).HasMaxLength(2000);

            // Relationships within Wildlife feature
            builder
                .HasOne(static e => e.Species)
                .WithMany()
                .HasForeignKey(static e => e.SpeciesId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne(static e => e.UserSpecies)
                .WithMany(static e => e.Sightings)
                .HasForeignKey(static e => e.UserSpeciesId)
                .OnDelete(DeleteBehavior.Restrict);

            // Note: MunicipalityId and UserId references are by ID only - no navigation properties to other modules

            // Indexes
            builder
                .HasIndex(static e => e.Location)
                .HasMethod("gist")
                .HasDatabaseName("sightings_location_gist_idx");

            builder.HasIndex(static e => e.ObservedAt).HasDatabaseName("sightings_observed_at_idx");

            builder.HasIndex(static e => e.Status).HasDatabaseName("sightings_status_idx");

            builder
                .HasIndex(static e => e.ReportedByUserId)
                .HasDatabaseName("sightings_reported_by_user_id_idx");

            builder
                .HasIndex(static e => e.IsFlaggedForReview)
                .HasDatabaseName("sightings_is_flagged_for_review_idx");

            builder.HasIndex(static e => e.MunicipalityId).HasDatabaseName("sightings_municipality_id_idx");

            // Check constraint: must have either SpeciesId or UserSpeciesId
            builder.ToTable(static t =>
                t.HasCheckConstraint(
                    "CK_sightings_species_reference",
                    "species_id IS NOT NULL OR user_species_id IS NOT NULL"
                )
            );
        }
    }
}
