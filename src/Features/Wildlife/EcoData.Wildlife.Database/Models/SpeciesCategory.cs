using EcoData.Wildlife.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Wildlife.Database.Models;

public sealed class SpeciesCategory
{
    public required Guid Id { get; set; }
    public required string Code { get; set; }
    public required List<LocaleValue> Name { get; set; }

    public ICollection<SpeciesCategoryLink> SpeciesLinks { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SpeciesCategory>
    {
        public void Configure(EntityTypeBuilder<SpeciesCategory> builder)
        {
            builder.ToTable("species_categories");
            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Code).HasMaxLength(50).IsRequired();

            builder.OwnsMany(static e => e.Name, b => b.ToJson());

            builder
                .HasIndex(static e => e.Code)
                .IsUnique()
                .HasDatabaseName("species_categories_code_uidx");
        }
    }
}
