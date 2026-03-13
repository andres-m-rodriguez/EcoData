using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Sensors.Database.Models;

public sealed class SensorType
{
    public required Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string? Description { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public ICollection<Parameter> Parameters { get; set; } = [];
    public ICollection<Sensor> Sensors { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SensorType>
    {
        public void Configure(EntityTypeBuilder<SensorType> builder)
        {
            builder.ToTable("sensor_types");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Code).HasMaxLength(50).IsRequired();

            builder.Property(static e => e.Name).HasMaxLength(100).IsRequired();

            builder.Property(static e => e.Description).HasMaxLength(500);

            builder.Property(static e => e.CreatedAt).IsRequired();

            builder.HasIndex(static e => e.Code).IsUnique();

            builder
                .HasMany(static e => e.Parameters)
                .WithOne(static e => e.SensorType)
                .HasForeignKey(static e => e.SensorTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasMany(static e => e.Sensors)
                .WithOne(static e => e.SensorType)
                .HasForeignKey(static e => e.SensorTypeId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
