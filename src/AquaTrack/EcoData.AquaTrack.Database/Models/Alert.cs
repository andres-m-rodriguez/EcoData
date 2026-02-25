using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.AquaTrack.Database.Models;

public sealed class Alert
{
    public required Guid Id { get; set; }
    public required Guid SensorId { get; set; }
    public required string Parameter { get; set; }
    public double? ThresholdMin { get; set; }
    public double? ThresholdMax { get; set; }
    public required DateTimeOffset TriggeredAt { get; set; }
    public required double Value { get; set; }
    public required bool Resolved { get; set; }

    public Sensor Sensor { get; set; } = null!;

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Alert>
    {
        public void Configure(EntityTypeBuilder<Alert> builder)
        {
            builder.ToTable("alerts");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Parameter)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(static e => e.TriggeredAt);

            builder.HasIndex(static e => new { e.SensorId, e.Resolved });
        }
    }
}
