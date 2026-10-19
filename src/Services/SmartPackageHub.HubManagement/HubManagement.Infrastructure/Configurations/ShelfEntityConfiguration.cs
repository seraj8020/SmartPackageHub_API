using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal class ShelfEntityConfiguration : IEntityTypeConfiguration<Shelf>
    {
        public void Configure(EntityTypeBuilder<Shelf> builder)
        {
            builder.ToTable("Shelves");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.Code).IsRequired().HasMaxLength(50);
            builder.Property(p => p.TowerOrWing).HasMaxLength(100);
            builder.Property(p => p.IsColdStorage).IsRequired();
            builder.Property(p => p.Capacity).IsRequired();

            builder.OwnsOne(e => e.Location, locBuilder =>
            {
                locBuilder.Property(l => l.Zone).HasColumnName("Zone").IsRequired().HasMaxLength(50);
                locBuilder.Property(l => l.Aisle).HasColumnName("Aisle").IsRequired().HasMaxLength(50);
                locBuilder.Property(l => l.Tier).HasColumnName("Tier").IsRequired();
            });

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();
        }
    }
}
