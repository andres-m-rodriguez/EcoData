using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.AquaTrack.Database.Models;

public sealed class SensorHealthStatus
{
    public required Guid Id { get; set; }
    public required Guid SensorId { get; set; }
    public required DateTimeOffset? LastReadingAt { get; set; }
    public required DateTimeOffset? LastHeartbeatAt { get; set; }
    public required SensorHealthStatusType Status { get; set; }
    public required int ConsecutiveFailures { get; set; }
    public required string? LastErrorMessage { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }

    public Sensor? Sensor { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SensorHealthStatus>
    {
        public void Configure(EntityTypeBuilder<SensorHealthStatus> builder)
        {
            builder.ToTable("sensor_health_statuses");

            builder.HasKey(static e => e.Id);

            builder.HasIndex(static e => e.SensorId).IsUnique();

            builder.HasIndex(static e => e.Status);

            builder.Property(static e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(static e => e.LastErrorMessage).HasMaxLength(500);

            builder.Property(static e => e.UpdatedAt).IsRequired();
        }
    }
}
