using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Organization.Database.Models;

public sealed class OrganizationRole
{
    public required Guid Id { get; set; }
    public required Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public Organization? Organization { get; set; }
    public ICollection<OrganizationRolePermission> Permissions { get; set; } = [];
    public ICollection<OrganizationMember> Members { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<OrganizationRole>
    {
        public void Configure(EntityTypeBuilder<OrganizationRole> builder)
        {
            builder.ToTable("organization_roles");
            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Name).HasMaxLength(100).IsRequired();
            builder.Property(static e => e.CreatedAt).IsRequired();

            // Unique constraint: role name per org
            builder.HasIndex(static e => new { e.OrganizationId, e.Name }).IsUnique();

            builder
                .HasOne(static e => e.Organization)
                .WithMany()
                .HasForeignKey(static e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
