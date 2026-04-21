using EcoData.Common.i18n;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Wildlife.Database.Models;

public sealed class Species
{
    public required Guid Id { get; set; }
    public required List<LocaleValue> CommonName { get; set; }
    public required string ScientificName { get; set; }
    public required byte[]? ProfileImageData { get; set; }
    public required string? ProfileImageContentType { get; set; }
    public required string? ImageSourceUrl { get; set; }
    public required bool IsFauna { get; set; }

    /// <summary>
    /// NatureServe Element Code - standardized species identifier (e.g., "ABNKC12024")
    /// </summary>
    public required string ElCode { get; set; }

    /// <summary>
    /// Global Conservation Rank (e.g., "G1" = critically imperiled, "G5" = secure)
    /// </summary>
    public required string GRank { get; set; }

    /// <summary>
    /// Subnational (State/Territory) Conservation Rank (e.g., "S1" = critically imperiled)
    /// </summary>
    public required string SRank { get; set; }

    public ICollection<FwsLink> FwsLinks { get; set; } = [];
    public ICollection<MunicipalitySpecies> MunicipalitySpecies { get; set; } = [];
    public ICollection<SpeciesCategoryLink> CategoryLinks { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Species>
    {
        public void Configure(EntityTypeBuilder<Species> builder)
        {
            builder.ToTable("species");
            builder.HasKey(static e => e.Id);

            builder.OwnsMany(static e => e.CommonName, b => b.ToJson());

            builder.Property(static e => e.ScientificName).HasMaxLength(200).IsRequired();

            builder.Property(static e => e.ProfileImageContentType).HasMaxLength(100);

            builder.Property(static e => e.ImageSourceUrl).HasMaxLength(500);

            builder.Property(static e => e.ElCode).HasMaxLength(20);

            builder.Property(static e => e.GRank).HasMaxLength(20);

            builder.Property(static e => e.SRank).HasMaxLength(20);

            builder
                .HasIndex(static e => e.ScientificName)
                .IsUnique()
                .HasDatabaseName("species_scientific_name_uidx");
        }
    }
}
