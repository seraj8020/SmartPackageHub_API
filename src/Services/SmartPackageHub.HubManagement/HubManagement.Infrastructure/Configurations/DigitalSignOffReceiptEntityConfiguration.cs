using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class DigitalSignOffReceiptEntityConfiguration : IEntityTypeConfiguration<DigitalSignOffReceipt>
    {
        public void Configure(EntityTypeBuilder<DigitalSignOffReceipt> builder)
        {
            builder.ToTable("DigitalSignOffReceipts");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.ReceiptNumber).IsRequired().HasMaxLength(100);
            builder.Property(e => e.CourierPartner).IsRequired().HasMaxLength(100);
            builder.Property(e => e.CourierAgentId);
            builder.Property(e => e.CourierAgentName).IsRequired().HasMaxLength(150);
            builder.Property(e => e.CourierAgentPhone).IsRequired().HasMaxLength(50);
            builder.Property(e => e.StaffUserId).IsRequired();
            builder.Property(e => e.TotalPackagesCount).IsRequired();
            builder.Property(e => e.DigitalSignature).HasMaxLength(500);
            builder.Property(e => e.Status).IsRequired().HasMaxLength(50);
            builder.Property(e => e.SignedAt).IsRequired();
            builder.Property(e => e.Notes).HasMaxLength(500);

            builder.HasIndex(e => e.ReceiptNumber).IsUnique();
            builder.HasIndex(e => new { e.SocietyId, e.SignedAt });

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.StaffUserId)
                .IsRequired();

            builder.HasOne<CourierAgent>()
                .WithMany()
                .HasForeignKey(e => e.CourierAgentId)
                .IsRequired(false);

            builder.HasMany(e => e.Packages)
                .WithOne()
                .HasForeignKey(p => p.DigitalSignOffReceiptId)
                .IsRequired(false);
        }
    }
}
