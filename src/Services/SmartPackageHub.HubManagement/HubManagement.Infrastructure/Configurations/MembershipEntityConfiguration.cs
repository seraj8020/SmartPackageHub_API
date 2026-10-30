using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class MembershipEntityConfiguration : IEntityTypeConfiguration<Membership>
    {
        public void Configure(EntityTypeBuilder<Membership> builder)
        {
            builder.ToTable("Memberships");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.ResidentId).IsRequired();
            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.Status).HasMaxLength(50);
            builder.Property(p => p.IsAutoRenewEnabled);

            builder.OwnsOne(e => e.Tier, tierBuilder =>
            {
                tierBuilder.Property(t => t.Name).HasColumnName("SubscriptionTier").IsRequired().HasMaxLength(50);
            });

            builder.OwnsOne(e => e.Period, periodBuilder =>
            {
                periodBuilder.Property(p => p.StartDate).HasColumnName("StartDate").IsRequired();
                periodBuilder.Property(p => p.EndDate).HasColumnName("EndDate").IsRequired();
            });

            builder.HasOne<Resident>()
                .WithMany(r => r.Memberships)
                .HasForeignKey(e => e.ResidentId)
                .IsRequired();

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();
        }
    }
}
