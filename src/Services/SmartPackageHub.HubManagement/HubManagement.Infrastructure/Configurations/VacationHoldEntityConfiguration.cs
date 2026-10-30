using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class VacationHoldEntityConfiguration : IEntityTypeConfiguration<VacationHold>
    {
        public void Configure(EntityTypeBuilder<VacationHold> builder)
        {
            builder.ToTable("VacationHolds");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.ResidentId).IsRequired();
            builder.Property(p => p.FromDate).IsRequired();
            builder.Property(p => p.ToDate).IsRequired();
            builder.Property(p => p.IsActive).IsRequired();

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();

            builder.HasOne<Resident>()
                .WithMany(r => r.VacationHolds)
                .HasForeignKey(e => e.ResidentId)
                .IsRequired();
        }
    }
}
