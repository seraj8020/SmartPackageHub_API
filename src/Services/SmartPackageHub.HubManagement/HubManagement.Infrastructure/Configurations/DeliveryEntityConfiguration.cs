using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValueObjects = HubManagement.Core.Hubs.ValueObjects;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal class DeliveryEntityConfiguration : IEntityTypeConfiguration<Delivery>
    {
        public void Configure(EntityTypeBuilder<Delivery> builder)
        {
            builder.ToTable("Deliveries");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.PackageId).IsRequired();
            builder.Property(e => e.HandedByUserId).IsRequired();
            builder.Property(e => e.ResidentId).IsRequired();
            builder.Property(e => e.OtpLogId);
            builder.Property(e => e.BatchId);
            builder.Property(p => p.Method).HasMaxLength(100);
            builder.Property(p => p.DeliveredAt).IsRequired();
            builder.Property(p => p.Notes).HasMaxLength(500);

            builder.OwnsOne(e => e.Slot, slotBuilder =>
            {
                slotBuilder.Property(s => s.StartTime).HasColumnName("SlotStartTime").IsRequired();
                slotBuilder.Property(s => s.EndTime).HasColumnName("SlotEndTime").IsRequired();
            });

            builder.OwnsOne(e => e.Status, statusBuilder =>
            {
                statusBuilder.Property(s => s.Value).HasColumnName("Status").IsRequired().HasMaxLength(50);
            });

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();

            builder.HasOne<Package>()
                .WithMany()
                .HasForeignKey(e => e.PackageId)
                .IsRequired();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.HandedByUserId)
                .IsRequired();

            builder.HasOne<Resident>()
                .WithMany()
                .HasForeignKey(e => e.ResidentId)
                .IsRequired();

            builder.HasOne<OTPLog>()
                .WithMany()
                .HasForeignKey(e => e.OtpLogId)
                .IsRequired(false);

            builder.HasOne<DispatchBatch>()
                .WithMany()
                .HasForeignKey(e => e.BatchId)
                .IsRequired(false);
        }
    }
}
