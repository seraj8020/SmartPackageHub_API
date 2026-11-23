using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class PackageCustodyHopEntityConfiguration : IEntityTypeConfiguration<PackageCustodyHop>
    {
        public void Configure(EntityTypeBuilder<PackageCustodyHop> builder)
        {
            builder.ToTable("PackageCustodyHops");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.PackageId).IsRequired();
            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.Kind).IsRequired().HasMaxLength(PackageCustodyHop.KindMaxLength);
            builder.Property(e => e.FromZone).HasMaxLength(PackageCustodyHop.ZoneMaxLength);
            builder.Property(e => e.ToZone).HasMaxLength(PackageCustodyHop.ZoneMaxLength);
            builder.Property(e => e.LocationLabel).HasMaxLength(PackageCustodyHop.LocationMaxLength);
            builder.Property(e => e.StaffUserId);
            builder.Property(e => e.StaffUsername).HasMaxLength(PackageCustodyHop.StaffNameMaxLength);
            builder.Property(e => e.OccurredAtUtc).IsRequired();
            builder.Property(e => e.Notes).HasMaxLength(PackageCustodyHop.NotesMaxLength);

            builder.HasOne<Package>()
                .WithMany()
                .HasForeignKey(e => e.PackageId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.StaffUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(e => new { e.SocietyId, e.OccurredAtUtc })
                .HasDatabaseName("IX_PackageCustodyHops_SocietyId_OccurredAtUtc");

            builder.HasIndex(e => new { e.PackageId, e.OccurredAtUtc })
                .HasDatabaseName("IX_PackageCustodyHops_PackageId_OccurredAtUtc");
        }
    }
}
