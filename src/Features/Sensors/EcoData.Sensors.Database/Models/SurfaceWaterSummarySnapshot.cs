using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Sensors.Database.Models;

/// <summary>
/// Single-row pre-computed surface-water dashboard summary, rewritten by the
/// snapshot refresher after each ingestion cycle.
/// </summary>
public sealed class SurfaceWaterSummarySnapshot
{
    public required int Id { get; set; }
    public required int TotalStations { get; set; }
    public required int StationsReporting { get; set; }
    public required long Readings7d { get; set; }
    public required double? MedianStreamflowCfs { get; set; }
    public required double? MeanGageHeightFt { get; set; }
    public required double? MeanRainfallInches7d { get; set; }
    public required DateTimeOffset ComputedAt { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SurfaceWaterSummarySnapshot>
    {
        public void Configure(EntityTypeBuilder<SurfaceWaterSummarySnapshot> builder)
        {
            builder.ToTable("surface_water_summary_snapshots");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Id).ValueGeneratedNever();
        }
    }
}
