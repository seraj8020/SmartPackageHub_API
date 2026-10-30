using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class AnalyticsDailyEntityConfiguration : IEntityTypeConfiguration<AnalyticsDaily>
    {
        public void Configure(EntityTypeBuilder<AnalyticsDaily> builder)
        {
            builder.ToTable("AnalyticsDailies");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.Day).IsRequired();
            builder.Property(p => p.PackagesReceived).IsRequired();
            builder.Property(p => p.PackagesDelivered).IsRequired();
            builder.Property(p => p.OverdueCount).IsRequired();

            builder.HasIndex(e => e.SocietyId);
            builder.HasIndex(e => new { e.SocietyId, e.Day })
                .IsUnique()
                .HasDatabaseName("IX_AnalyticsDailies_SocietyId_Day");

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();
        }
    }
}
