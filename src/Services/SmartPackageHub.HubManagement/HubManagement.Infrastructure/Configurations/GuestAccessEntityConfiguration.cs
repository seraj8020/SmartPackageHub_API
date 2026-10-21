using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class GuestAccessEntityConfiguration : IEntityTypeConfiguration<GuestAccess>
    {
        public void Configure(EntityTypeBuilder<GuestAccess> builder)
        {
            builder.ToTable("GuestAccesses");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.ResidentId).IsRequired();
            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.GuestName).IsRequired().HasMaxLength(150);
            builder.Property(p => p.PhoneNo).IsRequired().HasMaxLength(50);

            builder.OwnsOne(e => e.Window, windowBuilder =>
            {
                windowBuilder.Property(w => w.ValidFrom).HasColumnName("ValidFrom").IsRequired();
                windowBuilder.Property(w => w.ValidTo).HasColumnName("ValidTo").IsRequired();
            });

            builder.HasOne<Resident>()
                .WithMany()
                .HasForeignKey(e => e.ResidentId)
                .IsRequired();

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();
        }
    }
}
