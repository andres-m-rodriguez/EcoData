using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Sensors.Database.Models;

public sealed class IngestionLog
{
    public required Guid Id { get; set; }
    public required Guid DataSourceId { get; set; }
    public required DateTimeOffset IngestedAt { get; set; }
    public required int RecordCount { get; set; }
    public required DateTimeOffset LastRecordedAt { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<IngestionLog>
    {
        public void Configure(EntityTypeBuilder<IngestionLog> builder)
        {
            builder.ToTable("ingestion_logs");

            builder.HasKey(static e => e.Id);

            builder.HasIndex(static e => new { e.DataSourceId, e.IngestedAt });
        }
    }
}
