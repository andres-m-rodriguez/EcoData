using EcoData.Common.i18n;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Wildlife.Database.Models;

/// <summary>
/// Represents an NRCS (Natural Resources Conservation Service) conservation practice.
/// </summary>
public sealed class NrcsPractice
{
    public required Guid Id { get; set; }
    public required string Code { get; set; }
    public List<LocaleValue> Name { get; set; } = [];

    public ICollection<FwsLink> FwsLinks { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<NrcsPractice>
    {
        public void Configure(EntityTypeBuilder<NrcsPractice> builder)
        {
            builder.ToTable("nrcs_practices");
            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Code).HasMaxLength(20).IsRequired();

            builder.OwnsMany(static e => e.Name, b => b.ToJson());

            builder
                .HasIndex(static e => e.Code)
                .IsUnique()
                .HasDatabaseName("nrcs_practices_code_uidx");
        }
    }
}
