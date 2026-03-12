using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Organization.Database.Models;

public sealed class OrganizationMember
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required Guid OrganizationId { get; set; }
    public required Guid RoleId { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public Organization? Organization { get; set; }
    public OrganizationRole? Role { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<OrganizationMember>
    {
        public void Configure(EntityTypeBuilder<OrganizationMember> builder)
        {
            builder.ToTable("organization_members");
            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.UserId).IsRequired();
            builder.Property(static e => e.OrganizationId).IsRequired();
            builder.Property(static e => e.RoleId).IsRequired();
            builder.Property(static e => e.CreatedAt).IsRequired();

            // Unique constraint: user can only be in an organization once
            builder.HasIndex(static e => new { e.UserId, e.OrganizationId }).IsUnique();

            // Index for querying by user
            builder.HasIndex(static e => e.UserId);

            builder
                .HasOne(static e => e.Organization)
                .WithMany(static e => e.Members)
                .HasForeignKey(static e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(static e => e.Role)
                .WithMany(static e => e.Members)
                .HasForeignKey(static e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Note: UserId references users table in Identity database
            // No FK constraint since it's a cross-database reference
        }
    }
}
