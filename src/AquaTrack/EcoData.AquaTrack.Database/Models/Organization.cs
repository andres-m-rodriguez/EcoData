using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.AquaTrack.Database.Models;

public sealed class Organization
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string? ProfilePictureUrl { get; set; }
    public required string? CardPictureUrl { get; set; }
    public required string? AboutUs { get; set; }
    public required string? WebsiteUrl { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Sensor> Sensors { get; set; } = [];
    public ICollection<ApiKey> ApiKeys { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.ToTable("organizations");
            builder.HasKey(static e => e.Id);
            builder.Property(static e => e.Name).HasMaxLength(200).IsRequired();
            builder.Property(static e => e.ProfilePictureUrl).HasMaxLength(500);
            builder.Property(static e => e.CardPictureUrl).HasMaxLength(500);
            builder.Property(static e => e.AboutUs).HasMaxLength(2000);
            builder.Property(static e => e.WebsiteUrl).HasMaxLength(500);
            builder.Property(static e => e.CreatedAt).IsRequired();
            builder.Property(static e => e.UpdatedAt).IsRequired();

            builder.HasIndex(static e => e.Name).IsUnique();

            builder
                .HasMany(static e => e.ApiKeys)
                .WithOne(static e => e.Organization)
                .HasForeignKey(static e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
