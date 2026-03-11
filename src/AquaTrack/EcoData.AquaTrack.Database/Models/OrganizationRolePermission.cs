using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.AquaTrack.Database.Models;

public sealed class OrganizationRolePermission
{
    public required Guid RoleId { get; set; }
    public required string Permission { get; set; }

    public OrganizationRole? Role { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<OrganizationRolePermission>
    {
        public void Configure(EntityTypeBuilder<OrganizationRolePermission> builder)
        {
            builder.ToTable("organization_role_permissions");
            builder.HasKey(static e => new { e.RoleId, e.Permission });

            builder.Property(static e => e.Permission).HasMaxLength(100).IsRequired();

            builder
                .HasOne(static e => e.Role)
                .WithMany(static e => e.Permissions)
                .HasForeignKey(static e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
