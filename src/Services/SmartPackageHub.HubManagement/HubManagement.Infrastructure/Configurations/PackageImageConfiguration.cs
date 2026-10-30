using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class PackageImageConfiguration : IEntityTypeConfiguration<PackageImage>
    {
        public void Configure(EntityTypeBuilder<PackageImage> builder)
        {
            builder.ToTable("PackageImages");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.PackageId).IsRequired();
            builder.Property(e => e.IntakeId).IsRequired();
            builder.Property(e => e.Kind).HasMaxLength(50);
            builder.Property(p => p.StorageKey).IsRequired().HasMaxLength(500);
            builder.Property(p => p.SHA256Hash).IsRequired().HasMaxLength(128);
            builder.Property(p => p.ContentType).IsRequired().HasMaxLength(100);
            builder.Property(p => p.ByteSize).IsRequired();
            builder.Property(p => p.CapturedAt).IsRequired();

            builder.HasOne<Package>()
                .WithMany(p => p.Images)
                .HasForeignKey(e => e.PackageId)
                .IsRequired();

            builder.HasOne(e => e.Intake)
                .WithMany()
                .HasForeignKey(e => e.IntakeId)
                .IsRequired();
        }
    }
}
