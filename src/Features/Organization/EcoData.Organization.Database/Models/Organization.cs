using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Organization.Database.Models;

public sealed class Organization
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string? Tagline { get; set; }
    public required string? ProfilePictureUrl { get; set; }
    public required string? CardPictureUrl { get; set; }
    public required string? AboutUs { get; set; }
    public required string? WebsiteUrl { get; set; }
    public required string? Location { get; set; }
    public required int? FoundedYear { get; set; }
    public required string? LegalStatus { get; set; }
    public required string? TaxId { get; set; }
    public required string? PrimaryColor { get; set; }
    public required string? AccentColor { get; set; }
    public required string? ContactEmail { get; set; }
    public required OrganizationType? Type { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }

    public ICollection<DataSource> DataSources { get; set; } = [];
    public ICollection<OrganizationMember> Members { get; set; } = [];

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.ToTable("organizations");
            builder.HasKey(static e => e.Id);
            builder.Property(static e => e.Name).HasMaxLength(200).IsRequired();
            builder.Property(static e => e.Slug).HasMaxLength(80).IsRequired();
            builder.Property(static e => e.Tagline).HasMaxLength(280);
            builder.Property(static e => e.ProfilePictureUrl).HasMaxLength(500);
            builder.Property(static e => e.CardPictureUrl).HasMaxLength(500);
            builder.Property(static e => e.AboutUs).HasMaxLength(2000);
            builder.Property(static e => e.WebsiteUrl).HasMaxLength(500);
            builder.Property(static e => e.Location).HasMaxLength(200);
            builder.Property(static e => e.LegalStatus).HasMaxLength(80);
            builder.Property(static e => e.TaxId).HasMaxLength(40);
            // Brand colors are stored as 7-char hex strings (e.g. "#1f4d3a"). The design
            // injects them as CSS variables, so we keep the leading "#" in the value.
            builder.Property(static e => e.PrimaryColor).HasMaxLength(7);
            builder.Property(static e => e.AccentColor).HasMaxLength(7);
            builder.Property(static e => e.ContactEmail).HasMaxLength(256);
            builder.Property(static e => e.Type).HasConversion<string>().HasMaxLength(40);
            builder.Property(static e => e.CreatedAt).IsRequired();
            builder.Property(static e => e.UpdatedAt).IsRequired();

            builder.HasIndex(static e => e.Name).IsUnique();
            builder.HasIndex(static e => e.Slug).IsUnique();
        }
    }
}
