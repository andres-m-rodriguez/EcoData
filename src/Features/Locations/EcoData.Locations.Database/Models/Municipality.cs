using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace EcoData.Locations.Database.Models;

public sealed class Municipality
{
    public required Guid Id { get; set; }
    public required Guid StateId { get; set; }
    public required string Name { get; set; }
    public required string GeoJsonId { get; set; }
    public required string CountyFipsCode { get; set; }
    public Geometry? Boundary { get; set; }
    public required decimal CentroidLatitude { get; set; }
    public required decimal CentroidLongitude { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public State? State { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Municipality>
    {
        public void Configure(EntityTypeBuilder<Municipality> builder)
        {
            builder.ToTable("municipalities");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Name).HasMaxLength(100).IsRequired();

            builder.Property(static e => e.GeoJsonId).HasMaxLength(50).IsRequired();

            builder.Property(static e => e.CountyFipsCode).HasMaxLength(10).IsRequired();

            builder.Property(static e => e.Boundary).HasColumnType("geometry(Geometry, 4326)");

            builder.Property(static e => e.CentroidLatitude).HasPrecision(9, 6).IsRequired();

            builder.Property(static e => e.CentroidLongitude).HasPrecision(9, 6).IsRequired();

            builder.HasIndex(static e => e.GeoJsonId).IsUnique();

            builder.HasIndex(static e => e.StateId);

            builder.HasIndex(static e => e.Boundary).HasMethod("GIST");
        }
    }
}
