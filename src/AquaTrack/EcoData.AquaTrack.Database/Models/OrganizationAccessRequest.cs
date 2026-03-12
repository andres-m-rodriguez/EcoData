using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.AquaTrack.Database.Models;

public sealed class OrganizationAccessRequest
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required Guid OrganizationId { get; set; }
    public required OrganizationAccessRequestStatus Status { get; set; }
    public required string? RequestMessage { get; set; }
    public required string? ReviewNotes { get; set; }
    public required Guid? ReviewedByUserId { get; set; }
    public required DateTimeOffset? ReviewedAt { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public Organization? Organization { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<OrganizationAccessRequest>
    {
        public void Configure(EntityTypeBuilder<OrganizationAccessRequest> builder)
        {
            builder.ToTable("organization_access_requests");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.UserId).IsRequired();

            builder.Property(static e => e.OrganizationId).IsRequired();

            builder.Property(static e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(static e => e.RequestMessage).HasMaxLength(1000);

            builder.Property(static e => e.ReviewNotes).HasMaxLength(1000);

            builder.HasIndex(static e => new { e.UserId, e.OrganizationId });

            builder.HasIndex(static e => e.Status);

            builder.HasIndex(static e => e.OrganizationId);

            builder
                .HasOne(static e => e.Organization)
                .WithMany()
                .HasForeignKey(static e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

public enum OrganizationAccessRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}
