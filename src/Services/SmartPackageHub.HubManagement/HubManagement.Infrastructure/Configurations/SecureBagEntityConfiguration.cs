using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal class SecureBagEntityConfiguration : IEntityTypeConfiguration<SecureBag>
    {
        public void Configure(EntityTypeBuilder<SecureBag> builder)
        {
            builder.ToTable("SecureBags");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.BagNumber).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Barcode).HasMaxLength(100);
            builder.Property(e => e.QrCode).HasMaxLength(100);
            builder.Property(e => e.SealNumber).HasMaxLength(100);
            builder.Property(e => e.Status).IsRequired().HasMaxLength(50);
            builder.Property(e => e.CapacityOrSize).HasMaxLength(50);
            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired();

            builder.HasIndex(e => new { e.SocietyId, e.BagNumber }).IsUnique();

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();

            builder.HasMany(e => e.Packages)
                .WithOne()
                .HasForeignKey(p => p.SecureBagId)
                .IsRequired(false);
        }
    }
}
