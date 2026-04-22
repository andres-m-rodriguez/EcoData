using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Sensors.Database.Models;

public sealed class UserNotification
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required Guid SensorId { get; set; }
    public required Guid? AlertId { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required NotificationType Type { get; set; }
    public required bool IsRead { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset? ReadAt { get; set; }

    public Sensor? Sensor { get; set; }
    public SensorHealthAlert? Alert { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<UserNotification>
    {
        public void Configure(EntityTypeBuilder<UserNotification> builder)
        {
            builder.ToTable("user_notifications");

            builder.HasKey(static e => e.Id);

            builder.HasIndex(static e => new { e.UserId, e.IsRead });

            builder.HasIndex(static e => e.UserId);

            builder.HasIndex(static e => e.CreatedAt);

            builder.Property(static e => e.Title).HasMaxLength(200).IsRequired();

            builder.Property(static e => e.Message).HasMaxLength(500).IsRequired();

            builder.Property(static e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder
                .HasOne(static e => e.Sensor)
                .WithMany()
                .HasForeignKey(static e => e.SensorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(static e => e.Alert)
                .WithMany()
                .HasForeignKey(static e => e.AlertId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
