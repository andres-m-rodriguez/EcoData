using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace EcoData.Locations.Database.Models;

public sealed class State
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required string FipsCode { get; set; }
    public Geometry? Boundary { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public ICollection<Municipality> Municipalities { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<State>
    {
        public void Configure(EntityTypeBuilder<State> builder)
        {
            builder.ToTable("states");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Name).HasMaxLength(100).IsRequired();

            builder.Property(static e => e.Code).HasMaxLength(10).IsRequired();

            builder.Property(static e => e.FipsCode).HasMaxLength(10).IsRequired();

            builder.Property(static e => e.Boundary).HasColumnType("geometry(Geometry, 4326)");

            builder.HasIndex(static e => e.Code).IsUnique();

            builder.HasIndex(static e => e.FipsCode).IsUnique();

            builder
                .HasMany(static e => e.Municipalities)
                .WithOne(static e => e.State)
                .HasForeignKey(static e => e.StateId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
