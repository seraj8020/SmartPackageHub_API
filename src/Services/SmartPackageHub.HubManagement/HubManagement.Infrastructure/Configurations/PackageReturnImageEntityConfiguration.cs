using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class PackageReturnImageEntityConfiguration : IEntityTypeConfiguration<PackageReturnImage>
    {
        public void Configure(EntityTypeBuilder<PackageReturnImage> builder)
        {
            builder.ToTable("PackageReturnImages");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.ReturnId).IsRequired();
            builder.Property(e => e.StorageKey).IsRequired().HasMaxLength(500);
            builder.Property(e => e.SHA256Hash).IsRequired().HasMaxLength(128);
            builder.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            builder.Property(e => e.ByteSize).IsRequired();
            builder.Property(e => e.SortOrder).IsRequired();
            builder.Property(e => e.CapturedAtUtc).IsRequired();

            builder.HasIndex(e => e.ReturnId)
                .HasDatabaseName("IX_PackageReturnImages_ReturnId");

            builder.HasIndex(e => new { e.ReturnId, e.SortOrder })
                .IsUnique()
                .HasDatabaseName("UX_PackageReturnImages_ReturnId_SortOrder");
        }
    }
}
