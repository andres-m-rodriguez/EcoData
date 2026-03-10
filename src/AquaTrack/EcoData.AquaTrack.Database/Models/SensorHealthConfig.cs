using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.AquaTrack.Database.Models;

public sealed class SensorHealthConfig
{
    public required Guid Id { get; set; }
    public required Guid SensorId { get; set; }
    public required int ExpectedIntervalSeconds { get; set; }
    public required int StaleThresholdSeconds { get; set; }
    public required int UnhealthyThresholdSeconds { get; set; }
    public required bool IsMonitoringEnabled { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }

    public Sensor? Sensor { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SensorHealthConfig>
    {
        public void Configure(EntityTypeBuilder<SensorHealthConfig> builder)
        {
            builder.ToTable("sensor_health_configs");

            builder.HasKey(static e => e.Id);

            builder.HasIndex(static e => e.SensorId).IsUnique();

            builder.Property(static e => e.ExpectedIntervalSeconds).IsRequired();

            builder.Property(static e => e.StaleThresholdSeconds).IsRequired();

            builder.Property(static e => e.UnhealthyThresholdSeconds).IsRequired();

            builder.Property(static e => e.IsMonitoringEnabled).IsRequired();

            builder.Property(static e => e.CreatedAt).IsRequired();

            builder.Property(static e => e.UpdatedAt).IsRequired();
        }
    }
}
