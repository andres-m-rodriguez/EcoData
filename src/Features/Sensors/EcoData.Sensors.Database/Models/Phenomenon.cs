using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Sensors.Database.Models;

public sealed class Phenomenon
{
    public required Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string? Description { get; set; }
    public required string CanonicalUnit { get; set; }
    public required ValueShape DefaultValueShape { get; set; }
    public required string[] Capabilities { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public ICollection<Parameter> Parameters { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Phenomenon>
    {
        public void Configure(EntityTypeBuilder<Phenomenon> builder)
        {
            builder.ToTable("phenomena");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Code).HasMaxLength(50).IsRequired();

            builder.Property(static e => e.Name).HasMaxLength(100).IsRequired();

            builder.Property(static e => e.Description).HasMaxLength(500);

            builder.Property(static e => e.CanonicalUnit).HasMaxLength(30).IsRequired();

            builder
                .Property(static e => e.DefaultValueShape)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(static e => e.Capabilities).HasColumnType("text[]").IsRequired();

            builder.Property(static e => e.CreatedAt).IsRequired();

            builder.HasIndex(static e => e.Code).IsUnique();
        }
    }
}
