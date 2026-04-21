using EcoData.Common.i18n;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Wildlife.Database.Models;

/// <summary>
/// Represents a Fish and Wildlife Service action for species conservation.
/// </summary>
public sealed class FwsAction
{
    public required Guid Id { get; set; }
    public required string Code { get; set; }
    public List<LocaleValue> Name { get; set; } = [];

    public ICollection<FwsLink> FwsLinks { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<FwsAction>
    {
        public void Configure(EntityTypeBuilder<FwsAction> builder)
        {
            builder.ToTable("fws_actions");
            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Code).HasMaxLength(20).IsRequired();

            builder.OwnsMany(static e => e.Name, b => b.ToJson());

            builder
                .HasIndex(static e => e.Code)
                .IsUnique()
                .HasDatabaseName("fws_actions_code_uidx");
        }
    }
}
