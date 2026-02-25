using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.AquaTrack.Database.Models;

public sealed class Sensor
{
    public required Guid Id { get; set; }
    public required Guid SourceId { get; set; }
    public required string ExternalId { get; set; }
    public required string Name { get; set; }
    public required decimal Latitude { get; set; }
    public required decimal Longitude { get; set; }
    public string? Municipality { get; set; }
    public required bool IsActive { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public DataSource DataSource { get; set; } = null!;
    public ICollection<Reading> Readings { get; set; } = [];
    public ICollection<Alert> Alerts { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Sensor>
    {
        public void Configure(EntityTypeBuilder<Sensor> builder)
        {
            builder.ToTable("sensors");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.ExternalId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(static e => e.Name)
                .HasMaxLength(300)
                .IsRequired();

            builder.Property(static e => e.Latitude)
                .HasPrecision(9, 6)
                .IsRequired();

            builder.Property(static e => e.Longitude)
                .HasPrecision(9, 6)
                .IsRequired();

            builder.Property(static e => e.Municipality)
                .HasMaxLength(100);

            builder.HasIndex(static e => new { e.SourceId, e.ExternalId })
                .IsUnique();

            builder.HasMany(static e => e.Readings)
                .WithOne(static e => e.Sensor)
                .HasForeignKey(static e => e.SensorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(static e => e.Alerts)
                .WithOne(static e => e.Sensor)
                .HasForeignKey(static e => e.SensorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
