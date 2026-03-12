using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Sensors.Database.Models;

public sealed class Parameter
{
    public required Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string DefaultUnit { get; set; }
    public required Guid SensorTypeId { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public SensorType? SensorType { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Parameter>
    {
        public void Configure(EntityTypeBuilder<Parameter> builder)
        {
            builder.ToTable("parameters");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Code).HasMaxLength(50).IsRequired();

            builder.Property(static e => e.Name).HasMaxLength(100).IsRequired();

            builder.Property(static e => e.DefaultUnit).HasMaxLength(50).IsRequired();

            builder.Property(static e => e.CreatedAt).IsRequired();

            builder.HasIndex(static e => e.Code).IsUnique();

            builder.HasIndex(static e => e.SensorTypeId);
        }
    }
}
