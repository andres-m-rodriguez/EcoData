using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Sensors.Database.Models;

/// <summary>
/// Pre-computed dashboard row for one surface-water station, rewritten by the
/// snapshot refresher after each ingestion cycle. Status is not stored: it is
/// derived at read time so stations age into Offline without a write.
/// </summary>
public sealed class SurfaceWaterStationSnapshot
{
    public required Guid SensorId { get; set; }
    public required int Rank { get; set; }
    public required string Name { get; set; }
    public required string ExternalId { get; set; }
    public required Guid? MunicipalityId { get; set; }
    public required decimal Latitude { get; set; }
    public required decimal Longitude { get; set; }
    public required double? LatestStreamflowCfs { get; set; }
    public required double? LatestGageHeightFt { get; set; }
    public required DateTimeOffset? LatestRecordedAt { get; set; }
    public required List<double> SparklineFlow { get; set; }
    public required DateTimeOffset ComputedAt { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SurfaceWaterStationSnapshot>
    {
        public void Configure(EntityTypeBuilder<SurfaceWaterStationSnapshot> builder)
        {
            builder.ToTable("surface_water_station_snapshots");

            builder.HasKey(static e => e.SensorId);

            builder.Property(static e => e.SensorId).ValueGeneratedNever();

            builder.Property(static e => e.Name).HasMaxLength(300).IsRequired();

            builder.Property(static e => e.ExternalId).HasMaxLength(100).IsRequired();

            builder.Property(static e => e.Latitude).HasPrecision(9, 6).IsRequired();

            builder.Property(static e => e.Longitude).HasPrecision(9, 6).IsRequired();

            builder.HasIndex(static e => e.Rank).IsUnique();
        }
    }
}
