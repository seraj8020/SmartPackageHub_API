using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class NotificationEntityConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.ResidentId).IsRequired();
            builder.Property(e => e.PackageId).IsRequired();
            builder.Property(p => p.Status).HasConversion<string>().IsRequired().HasMaxLength(50);
            builder.Property(p => p.CreatedAt).IsRequired();
            builder.Property(p => p.SentAt);
            builder.Property(p => p.DeliveredAt);
            builder.Property(p => p.FailureReason).HasMaxLength(500);
            builder.Property(p => p.IdempotencyKey).HasMaxLength(120);
            builder.Property(p => p.ProviderRequestId).HasMaxLength(120);
            builder.Property(p => p.FallbackOfNotificationId);
            builder.Property(p => p.SegmentCount).IsRequired().HasDefaultValue(1);

            builder.HasIndex(p => p.IdempotencyKey)
                .IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL")
                .HasDatabaseName("IX_Notifications_IdempotencyKey");
            builder.HasIndex(p => p.ProviderRequestId)
                .HasDatabaseName("IX_Notifications_ProviderRequestId");

            builder.OwnsOne(e => e.Channel, channelBuilder =>
            {
                channelBuilder.Property(c => c.Type).HasColumnName("ChannelType").HasConversion<string>().IsRequired().HasMaxLength(50);
                channelBuilder.Property(c => c.Destination).HasColumnName("Destination").IsRequired().HasMaxLength(255);
            });

            builder.OwnsOne(e => e.Content, contentBuilder =>
            {
                contentBuilder.Property(c => c.TemplateName).HasColumnName("TemplateName").HasMaxLength(100);
                contentBuilder.Property(c => c.Body).HasColumnName("Body").IsRequired();
            });

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
