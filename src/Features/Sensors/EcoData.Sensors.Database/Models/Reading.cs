using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Sensors.Database.Models;

public sealed class Reading
{
    public required Guid Id { get; set; }
    public required Guid SensorId { get; set; }
    public required string Parameter { get; set; }
    public required string? Description { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required DateTimeOffset RecordedAt { get; set; }
    public required DateTimeOffset IngestedAt { get; set; }

    public Guid? PhenomenonId { get; set; }
    public Guid? ParameterId { get; set; }
    public double? CanonicalValue { get; set; }

    public Sensor? Sensor { get; set; }
    public Phenomenon? Phenomenon { get; set; }
    public Parameter? ResolvedParameter { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Reading>
    {
        public void Configure(EntityTypeBuilder<Reading> builder)
        {
            builder.ToTable("readings");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Parameter).HasMaxLength(50).IsRequired();

            builder.Property(static e => e.Description).HasMaxLength(200);

            builder.Property(static e => e.Unit).HasMaxLength(30).IsRequired();

            builder.HasIndex(static e => e.RecordedAt);

            builder.HasIndex(static e => new { e.SensorId, e.RecordedAt });

            builder.HasIndex(static e => new
            {
                e.SensorId,
                e.Parameter,
                e.RecordedAt,
            });

            builder.HasIndex(static e => new
            {
                e.SensorId,
                e.PhenomenonId,
                e.RecordedAt,
            });

            builder
                .HasOne(static e => e.Phenomenon)
                .WithMany()
                .HasForeignKey(static e => e.PhenomenonId)
                .OnDelete(DeleteBehavior.SetNull);

            builder
                .HasOne(static e => e.ResolvedParameter)
                .WithMany()
                .HasForeignKey(static e => e.ParameterId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
