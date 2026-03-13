using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Organization.Database.Models;

public sealed class ApiKey
{
    public required Guid Id { get; set; }
    public required Guid OrganizationId { get; set; }
    public required string KeyHash { get; set; }
    public required string KeyPrefix { get; set; }
    public required string Name { get; set; }
    public required string[] Scopes { get; set; }
    public required DateTimeOffset? ExpiresAt { get; set; }
    public required DateTimeOffset? LastUsedAt { get; set; }
    public required bool IsActive { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset? RevokedAt { get; set; }

    public Organization? Organization { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<ApiKey>
    {
        public void Configure(EntityTypeBuilder<ApiKey> builder)
        {
            builder.ToTable("api_keys");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.KeyHash).HasMaxLength(128).IsRequired();

            builder.Property(static e => e.KeyPrefix).HasMaxLength(16).IsRequired();

            builder.Property(static e => e.Name).HasMaxLength(100).IsRequired();

            builder.Property(static e => e.Scopes).IsRequired();

            builder.Property(static e => e.IsActive).IsRequired();

            builder.Property(static e => e.CreatedAt).IsRequired();

            builder.HasIndex(static e => e.KeyHash).IsUnique();

            builder.HasIndex(static e => e.OrganizationId);

            builder.HasIndex(static e => new { e.IsActive, e.ExpiresAt });
        }
    }
}
