using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class DeliverySlotEntityConfiguration : IEntityTypeConfiguration<DeliverySlot>
    {
        public void Configure(EntityTypeBuilder<DeliverySlot> builder)
        {
            builder.ToTable("DeliverySlots");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.ResidentId).IsRequired();
            builder.Property(e => e.PackageId).IsRequired();
            builder.Property(e => e.WindowStart).IsRequired();
            builder.Property(e => e.WindowEnd).IsRequired();
            builder.Property(e => e.Status).IsRequired().HasMaxLength(50);
            builder.Property(e => e.Notes).HasMaxLength(500);

            builder.HasIndex(e => e.PackageId);
            builder.HasIndex(e => new { e.SocietyId, e.WindowStart, e.WindowEnd });

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();

            builder.HasOne<Resident>()
                .WithMany()
                .HasForeignKey(e => e.ResidentId)
                .IsRequired();

            builder.HasOne<Package>()
                .WithMany()
                .HasForeignKey(e => e.PackageId)
                .IsRequired();
        }
    }
}
