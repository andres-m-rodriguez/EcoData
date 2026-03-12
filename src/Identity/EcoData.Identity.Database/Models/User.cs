using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Identity.Database.Models;

public sealed class User : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
    public required GlobalRole? GlobalRole { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.Property(static e => e.DisplayName).HasMaxLength(200).IsRequired();

            builder.Property(static e => e.GlobalRole).HasConversion<string>().HasMaxLength(50);
        }
    }
}
