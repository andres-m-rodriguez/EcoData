using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoData.Identity.Database.Models;

public sealed class AccessRequest
{
    public required Guid Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public required AccessRequestStatus Status { get; set; }
    public string? ReviewNotes { get; set; }
    public Guid? ReviewedById { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public User? ReviewedBy { get; set; }

    public sealed class EntityConfiguration : IEntityTypeConfiguration<AccessRequest>
    {
        public void Configure(EntityTypeBuilder<AccessRequest> builder)
        {
            builder.ToTable("access_requests");

            builder.HasKey(static e => e.Id);

            builder.Property(static e => e.Email).HasMaxLength(256).IsRequired();

            builder.Property(static e => e.DisplayName).HasMaxLength(200).IsRequired();

            builder.Property(static e => e.PasswordHash).HasMaxLength(500).IsRequired();

            builder.Property(static e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(static e => e.ReviewNotes).HasMaxLength(1000);

            builder.HasIndex(static e => e.Email).IsUnique();

            builder.HasIndex(static e => e.Status);

            builder
                .HasOne(static e => e.ReviewedBy)
                .WithMany()
                .HasForeignKey(static e => e.ReviewedById)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
