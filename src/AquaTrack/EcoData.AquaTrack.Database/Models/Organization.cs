using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.AquaTrack.Database.Models;

public sealed class Organization
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Sensor> Sensors { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.ToTable("organizations");
            builder.HasKey(static e => e.Id);
            builder.Property(static e => e.Name).HasMaxLength(200).IsRequired();
            builder.Property(static e => e.CreatedAt).IsRequired();
            builder.Property(static e => e.UpdatedAt).IsRequired();

            builder.HasIndex(static e => e.Name).IsUnique();
        }
    }
}
