using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Sensors.Database.Models;

public sealed class UserSensorSubscription
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required Guid SensorId { get; set; }
    public required bool NotifyOnStale { get; set; }
    public required bool NotifyOnUnhealthy { get; set; }
    public required bool NotifyOnRecovered { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public Sensor? Sensor { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<UserSensorSubscription>
    {
        public void Configure(EntityTypeBuilder<UserSensorSubscription> builder)
        {
            builder.ToTable("user_sensor_subscriptions");

            builder.HasKey(static e => e.Id);

            builder.HasIndex(static e => new { e.UserId, e.SensorId }).IsUnique();

            builder.HasIndex(static e => e.UserId);

            builder.HasIndex(static e => e.SensorId);

            builder
                .HasOne(static e => e.Sensor)
                .WithMany()
                .HasForeignKey(static e => e.SensorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
