using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class ResidentEntityConfiguration : IEntityTypeConfiguration<Resident>
    {
        public void Configure(EntityTypeBuilder<Resident> builder)
        {
            builder.ToTable("Residents");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.UserId).IsRequired();
            builder.Property(p => p.FullName).IsRequired().HasMaxLength(150);
            builder.Property(p => p.PhoneNumber).HasMaxLength(50);
            builder.Property(p => p.TowerWing).HasMaxLength(100);
            builder.Property(p => p.IsMember).IsRequired();
            builder.Property(p => p.AvatarUrl).HasMaxLength(500);
            builder.Property(p => p.IsActive).IsRequired();

            builder.OwnsOne(e => e.Unit, unitBuilder =>
            {
                unitBuilder.Property(u => u.Value).HasColumnName("UnitNumber").IsRequired().HasMaxLength(50);
            });

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired();

            builder.HasMany(e => e.VacationHolds)
                .WithOne()
                .HasForeignKey(vh => vh.ResidentId);

            builder.HasIndex(e => new { e.SocietyId, e.PhoneNumber });
            builder.HasIndex(e => new { e.SocietyId, e.TowerWing });
        }
    }
}
