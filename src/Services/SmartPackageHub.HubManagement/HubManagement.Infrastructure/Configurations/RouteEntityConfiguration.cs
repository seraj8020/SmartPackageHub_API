using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal class RouteEntityConfiguration : IEntityTypeConfiguration<Route>
    {
        public void Configure(EntityTypeBuilder<Route> builder)
        {
            builder.ToTable("Routes");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.BatchId).IsRequired();
            builder.Property(p => p.OriginLocation).IsRequired().HasMaxLength(255);
            builder.Property(p => p.DestinationLocation).IsRequired().HasMaxLength(255);
            builder.Property(p => p.DistanceMetres).IsRequired();
            builder.Property(p => p.MapProvider).HasMaxLength(100);
            builder.Property(p => p.Polyline);
            builder.Property(p => p.EstimatedDurationSecs);

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();

            builder.HasOne<DispatchBatch>()
                .WithMany()
                .HasForeignKey(e => e.BatchId)
                .IsRequired();

            builder.HasMany(e => e.Stops)
                .WithOne()
                .HasForeignKey(s => s.RouteId);
        }
    }
}
