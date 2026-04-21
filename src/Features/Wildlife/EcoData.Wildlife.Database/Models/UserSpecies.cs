using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Wildlife.Database.Models;

/// <summary>
/// Represents a species submitted by a user that is not yet in the main species database.
/// These are flagged for expert verification before being added to the main Species table.
/// </summary>
public sealed class UserSpecies
{
    public required Guid Id { get; set; }
    public required string CommonName { get; set; }
    public required string? ScientificName { get; set; }
    public required string? Description { get; set; }
    public required byte[]? PhotoData { get; set; }
    public required string? PhotoContentType { get; set; }
    public required bool IsVerified { get; set; }
    public required bool IsEndangered { get; set; }
    // Cross-module reference to Identity.User by ID only
    public required Guid CreatedByUserId { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset? VerifiedAt { get; set; }
    // Cross-module reference to Identity.User by ID only
    public required Guid? VerifiedByUserId { get; set; }
    public required Guid? ApprovedSpeciesId { get; set; }

    // Navigation properties within Wildlife feature only
    public Species? ApprovedSpecies { get; set; }
    public ICollection<Sighting> Sightings { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<UserSpecies>
    {
        public void Configure(EntityTypeBuilder<UserSpecies> builder)
        {
            builder.ToTable("user_species");
            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.CommonName).HasMaxLength(200).IsRequired();

            builder.Property(static e => e.ScientificName).HasMaxLength(200);

            builder.Property(static e => e.Description).HasMaxLength(2000);

            builder.Property(static e => e.PhotoContentType).HasMaxLength(100);

            // Note: User references are by ID only - no navigation properties to Identity feature
            builder
                .HasOne(static e => e.ApprovedSpecies)
                .WithMany()
                .HasForeignKey(static e => e.ApprovedSpeciesId)
                .OnDelete(DeleteBehavior.SetNull);

            builder
                .HasIndex(static e => e.CommonName)
                .HasDatabaseName("user_species_common_name_idx");

            builder
                .HasIndex(static e => e.IsVerified)
                .HasDatabaseName("user_species_is_verified_idx");

            builder
                .HasIndex(static e => e.CreatedByUserId)
                .HasDatabaseName("user_species_created_by_user_id_idx");
        }
    }
}
