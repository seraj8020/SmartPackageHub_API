using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class AuthorizedCollectorEntityConfiguration : IEntityTypeConfiguration<AuthorizedCollector>
    {
        public void Configure(EntityTypeBuilder<AuthorizedCollector> builder)
        {
            builder.ToTable("AuthorizedCollectors");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.PackageId).IsRequired();
            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.ResidentId).IsRequired();
            builder.Property(e => e.CollectorName).IsRequired().HasMaxLength(150);
            builder.Property(e => e.CollectorPhone).HasMaxLength(30);
            builder.Property(e => e.Note).HasMaxLength(300);
            builder.Property(e => e.AuthorizedAtUtc).IsRequired();
            builder.Property(e => e.ExpiresAtUtc);
            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.RevokedAtUtc);

            builder.HasOne<Package>()
                .WithMany()
                .HasForeignKey(e => e.PackageId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Resident>()
                .WithMany()
                .HasForeignKey(e => e.ResidentId)
                .IsRequired();

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();

            builder.HasIndex(e => new { e.PackageId, e.IsActive })
                .HasDatabaseName("IX_AuthorizedCollectors_PackageId_IsActive");
            builder.HasIndex(e => e.ResidentId)
                .HasDatabaseName("IX_AuthorizedCollectors_ResidentId");
        }
    }
}
