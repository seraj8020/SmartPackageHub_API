using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class PackageDeliveryEvidenceEntityConfiguration
        : IEntityTypeConfiguration<PackageDeliveryEvidence>
    {
        public void Configure(EntityTypeBuilder<PackageDeliveryEvidence> builder)
        {
            builder.ToTable("PackageDeliveryEvidence");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.DeliveryId).IsRequired();
            builder.Property(e => e.PackageId).IsRequired();
            builder.Property(e => e.Kind).IsRequired().HasMaxLength(40);
            builder.Property(e => e.StorageKey).IsRequired().HasMaxLength(500);
            builder.Property(e => e.SHA256Hash).IsRequired().HasMaxLength(128);
            builder.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            builder.Property(e => e.ByteSize).IsRequired();
            builder.Property(e => e.CapturedAtUtc).IsRequired();
            builder.Property(e => e.SignedByName).HasMaxLength(120);

            builder.HasIndex(e => e.DeliveryId)
                .HasDatabaseName("IX_PackageDeliveryEvidence_DeliveryId");

            builder.HasIndex(e => e.PackageId)
                .HasDatabaseName("IX_PackageDeliveryEvidence_PackageId");

            builder.HasIndex(e => new { e.DeliveryId, e.Kind })
                .IsUnique()
                .HasDatabaseName("UX_PackageDeliveryEvidence_DeliveryId_Kind");

            builder.HasOne<Delivery>()
                .WithMany()
                .HasForeignKey(e => e.DeliveryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Package>()
                .WithMany()
                .HasForeignKey(e => e.PackageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
