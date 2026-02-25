using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.AquaTrack.Database.Models;

public sealed class Reading
{
    public required Guid Id { get; set; }
    public required Guid SensorId { get; set; }
    public required string Parameter { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required DateTimeOffset RecordedAt { get; set; }
    public required DateTimeOffset IngestedAt { get; set; }

    public Sensor Sensor { get; set; } = null!;

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Reading>
    {
        public void Configure(EntityTypeBuilder<Reading> builder)
        {
            builder.ToTable("readings");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Parameter)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(static e => e.Unit)
                .HasMaxLength(30)
                .IsRequired();

            builder.HasIndex(static e => e.RecordedAt);

            builder.HasIndex(static e => new { e.SensorId, e.RecordedAt });

            builder.HasIndex(static e => new { e.SensorId, e.Parameter, e.RecordedAt });
        }
    }
}
