using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Identity.Database.Models;

public sealed class SensorCredential
{
    public required Guid SensorId { get; set; }
    public required Guid OrganizationId { get; set; }
    public required string OrganizationName { get; set; }
    public required string SensorName { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SensorCredential>
    {
        public void Configure(EntityTypeBuilder<SensorCredential> builder)
        {
            builder.ToTable("sensor_credentials");

            builder.HasKey(e => e.SensorId);

            builder.Property(e => e.OrganizationName).HasMaxLength(200).IsRequired();
            builder.Property(e => e.SensorName).HasMaxLength(200).IsRequired();
        }
    }
}
