using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal class SocietyEntityConfiguration : IEntityTypeConfiguration<Society>
    {
        public void Configure(EntityTypeBuilder<Society> builder)
        {
            builder.ToTable("Societies");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
            builder.Property(p => p.ContactEmail).HasMaxLength(150);
            builder.Property(p => p.Phone).HasMaxLength(50);

            builder.OwnsOne(e => e.Address, addrBuilder =>
            {
                addrBuilder.Property(a => a.Street).HasColumnName("Street").IsRequired().HasMaxLength(200);
                addrBuilder.Property(a => a.City).HasColumnName("City").IsRequired().HasMaxLength(100);
                addrBuilder.Property(a => a.State).HasColumnName("State").IsRequired().HasMaxLength(100);
                addrBuilder.Property(a => a.ZipCode).HasColumnName("ZipCode").IsRequired().HasMaxLength(20);
                addrBuilder.Property(a => a.Country).HasColumnName("Country").IsRequired().HasMaxLength(100);
            });
        }
    }
}
