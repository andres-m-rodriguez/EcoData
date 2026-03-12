using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.AquaTrack.Database.Models;

public sealed class OrganizationBlockedUser
{
    public required Guid Id { get; set; }
    public required Guid OrganizationId { get; set; }
    public required Guid UserId { get; set; }
    public required Guid BlockedByUserId { get; set; }
    public required string? Reason { get; set; }
    public required DateTimeOffset BlockedAt { get; set; }

    public Organization? Organization { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<OrganizationBlockedUser>
    {
        public void Configure(EntityTypeBuilder<OrganizationBlockedUser> builder)
        {
            builder.ToTable("organization_blocked_users");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.OrganizationId).IsRequired();
            builder.Property(static e => e.UserId).IsRequired();
            builder.Property(static e => e.BlockedByUserId).IsRequired();
            builder.Property(static e => e.Reason).HasMaxLength(500);
            builder.Property(static e => e.BlockedAt).IsRequired();

            // A user can only be blocked once per organization
            builder.HasIndex(static e => new { e.OrganizationId, e.UserId }).IsUnique();

            // Index for checking if user is blocked
            builder.HasIndex(static e => e.UserId);

            builder
                .HasOne(static e => e.Organization)
                .WithMany()
                .HasForeignKey(static e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
