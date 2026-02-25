using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.AquaTrack.Database.Models;

public sealed class DataSource
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required DataSourceType Type { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public required int PullIntervalSeconds { get; set; }
    public required bool IsActive { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public ICollection<Sensor> Sensors { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<DataSource>
    {
        public void Configure(EntityTypeBuilder<DataSource> builder)
        {
            builder.ToTable("data_sources");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(static e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(static e => e.BaseUrl)
                .HasMaxLength(500);

            builder.Property(static e => e.ApiKey)
                .HasMaxLength(500);

            builder.HasMany(static e => e.Sensors)
                .WithOne(static e => e.DataSource)
                .HasForeignKey(static e => e.SourceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
