using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Sensors.Database.Models;

/// <summary>
/// Single-row portal-wide reading stats, rewritten by the snapshot refresher
/// after each ingestion cycle so hero counters never scan the readings table.
/// </summary>
public sealed class ReadingStatsSnapshot
{
    public required int Id { get; set; }
    public required long TotalReadings { get; set; }
    public required DateTimeOffset ComputedAt { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<ReadingStatsSnapshot>
    {
        public void Configure(EntityTypeBuilder<ReadingStatsSnapshot> builder)
        {
            builder.ToTable("reading_stats_snapshots");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Id).ValueGeneratedNever();
        }
    }
}
