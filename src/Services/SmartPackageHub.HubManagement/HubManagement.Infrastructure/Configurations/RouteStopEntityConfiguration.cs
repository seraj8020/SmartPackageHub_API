using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal class RouteStopEntityConfiguration : IEntityTypeConfiguration<RouteStop>
    {
        public void Configure(EntityTypeBuilder<RouteStop> builder)
        {
            builder.ToTable("RouteStops");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.RouteId).IsRequired();
            builder.Property(e => e.PackageId).IsRequired();
            builder.Property(p => p.TowerWing).IsRequired().HasMaxLength(100);
            builder.Property(p => p.SequenceOrder).IsRequired();
            builder.Property(p => p.IsVisited).IsRequired();

            builder.HasOne<Route>()
                .WithMany(r => r.Stops)
                .HasForeignKey(e => e.RouteId)
                .IsRequired();

            builder.HasOne<Package>()
                .WithMany()
                .HasForeignKey(e => e.PackageId)
                .IsRequired();
        }
    }
}
