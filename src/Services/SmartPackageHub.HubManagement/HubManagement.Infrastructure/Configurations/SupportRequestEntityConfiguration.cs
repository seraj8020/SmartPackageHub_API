using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class SupportRequestEntityConfiguration : IEntityTypeConfiguration<SupportRequest>
    {
        public void Configure(EntityTypeBuilder<SupportRequest> builder)
        {
            builder.ToTable("SupportRequests");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.ResidentId).IsRequired();
            builder.Property(e => e.ReferenceCode).IsRequired().HasMaxLength(12);
            builder.Property(e => e.Subject).IsRequired().HasMaxLength(120);
            builder.Property(e => e.Topic).IsRequired().HasMaxLength(80);
            builder.Property(e => e.Urgency).IsRequired().HasMaxLength(20);
            builder.Property(e => e.Message).IsRequired().HasMaxLength(4000);
            builder.Property(e => e.ResidentName).HasMaxLength(120);
            builder.Property(e => e.ResidentPhone).HasMaxLength(20);
            builder.Property(e => e.TowerWing).HasMaxLength(40);
            builder.Property(e => e.FlatNumber).HasMaxLength(40);
            builder.Property(e => e.Status).IsRequired().HasMaxLength(40);
            builder.Property(e => e.SupportEmail).IsRequired().HasMaxLength(150);
            builder.Property(e => e.RequestedAtUtc).IsRequired();

            builder.HasIndex(e => e.ReferenceCode)
                .IsUnique()
                .HasDatabaseName("IX_SupportRequests_ReferenceCode");
            builder.HasIndex(e => new { e.SocietyId, e.RequestedAtUtc })
                .HasDatabaseName("IX_SupportRequests_SocietyId_RequestedAtUtc");
            builder.HasIndex(e => new { e.ResidentId, e.RequestedAtUtc })
                .HasDatabaseName("IX_SupportRequests_ResidentId_RequestedAtUtc");

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Resident>()
                .WithMany()
                .HasForeignKey(e => e.ResidentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
