using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace EcoData.Sensors.Database.Models;

public sealed class Sensor
{
    public required Guid Id { get; set; }
    public required Guid OrganizationId { get; set; }
    public required Guid? SourceId { get; set; }
    public required string ExternalId { get; set; }
    public required string Name { get; set; }
    public required decimal Latitude { get; set; }
    public required decimal Longitude { get; set; }
    public Point? Location { get; set; }
    public required Guid MunicipalityId { get; set; }
    public required bool IsActive { get; set; }
    public required ReportingMode ReportingMode { get; set; }
    public required Guid? SensorTypeId { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }

    public SensorType? SensorType { get; set; }
    public ICollection<Reading> Readings { get; set; } = [];
    public ICollection<Alert> Alerts { get; set; } = [];
    public SensorHealthConfig? HealthConfig { get; set; }
    public SensorHealthStatus? HealthStatus { get; set; }
    public ICollection<SensorHealthAlert> HealthAlerts { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Sensor>
    {
        public void Configure(EntityTypeBuilder<Sensor> builder)
        {
            builder.ToTable("sensors");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.ExternalId).HasMaxLength(100).IsRequired();

            builder.Property(static e => e.Name).HasMaxLength(300).IsRequired();

            builder.Property(static e => e.Latitude).HasPrecision(9, 6).IsRequired();

            builder.Property(static e => e.Longitude).HasPrecision(9, 6).IsRequired();

            builder.HasIndex(static e => e.MunicipalityId);

            builder.Property(static e => e.Location).HasColumnType("geometry(Point, 4326)");

            builder.HasIndex(static e => e.Location).HasMethod("GIST");

            builder
                .Property(static e => e.ReportingMode)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();

            builder.HasIndex(static e => new { e.OrganizationId, e.ExternalId }).IsUnique();

            builder.HasIndex(static e => e.OrganizationId);

            builder
                .HasMany(static e => e.Readings)
                .WithOne(static e => e.Sensor)
                .HasForeignKey(static e => e.SensorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasMany(static e => e.Alerts)
                .WithOne(static e => e.Sensor)
                .HasForeignKey(static e => e.SensorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(static e => e.HealthConfig)
                .WithOne(static e => e.Sensor)
                .HasForeignKey<SensorHealthConfig>(static e => e.SensorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(static e => e.HealthStatus)
                .WithOne(static e => e.Sensor)
                .HasForeignKey<SensorHealthStatus>(static e => e.SensorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasMany(static e => e.HealthAlerts)
                .WithOne(static e => e.Sensor)
                .HasForeignKey(static e => e.SensorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
