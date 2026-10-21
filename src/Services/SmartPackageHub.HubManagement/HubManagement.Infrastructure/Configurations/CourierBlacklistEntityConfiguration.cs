using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class CourierBlacklistEntityConfiguration : IEntityTypeConfiguration<CourierBlacklist>
    {
        public void Configure(EntityTypeBuilder<CourierBlacklist> builder)
        {
            builder.ToTable("CourierBlacklists");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.CarrierName).IsRequired().HasMaxLength(100);
            builder.Property(e => e.AgentName).HasMaxLength(150);
            builder.Property(e => e.Phone).IsRequired().HasMaxLength(50);
            builder.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            builder.Property(e => e.BlacklistedAt).IsRequired();
            builder.Property(e => e.IsActive).IsRequired();

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();
        }
    }
}
