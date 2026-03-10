using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.AquaTrack.Database.Models;

public sealed class SensorHealthAlert
{
    public required Guid Id { get; set; }
    public required Guid SensorId { get; set; }
    public required SensorHealthAlertType AlertType { get; set; }
    public required DateTimeOffset TriggeredAt { get; set; }
    public required DateTimeOffset? ResolvedAt { get; set; }
    public required string Message { get; set; }

    public Sensor? Sensor { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SensorHealthAlert>
    {
        public void Configure(EntityTypeBuilder<SensorHealthAlert> builder)
        {
            builder.ToTable("sensor_health_alerts");

            builder.HasKey(static e => e.Id);

            builder.HasIndex(static e => e.SensorId);

            builder.HasIndex(static e => e.TriggeredAt);

            builder.HasIndex(static e => new { e.SensorId, e.ResolvedAt });

            builder.Property(static e => e.AlertType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(static e => e.Message).HasMaxLength(500).IsRequired();
        }
    }
}
