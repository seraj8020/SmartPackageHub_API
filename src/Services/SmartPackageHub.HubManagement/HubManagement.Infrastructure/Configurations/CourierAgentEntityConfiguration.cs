using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal class CourierAgentEntityConfiguration : IEntityTypeConfiguration<CourierAgent>
    {
        public void Configure(EntityTypeBuilder<CourierAgent> builder)
        {
            builder.ToTable("CourierAgents");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.CarrierName).IsRequired().HasMaxLength(100);
            builder.Property(e => e.AgentName).IsRequired().HasMaxLength(150);
            builder.Property(e => e.Phone).IsRequired().HasMaxLength(50);
            builder.Property(e => e.VehicleNumber).HasMaxLength(50);
            builder.Property(e => e.EmployeeOrBadgeNumber).HasMaxLength(50);
            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired();

            builder.HasIndex(e => new { e.SocietyId, e.Phone });

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();
        }
    }
}
