using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class SocietyCarrierReturnPolicyEntityConfiguration
        : IEntityTypeConfiguration<SocietyCarrierReturnPolicy>
    {
        public void Configure(EntityTypeBuilder<SocietyCarrierReturnPolicy> builder)
        {
            builder.ToTable("SocietyCarrierReturnPolicies");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.CarrierName).IsRequired().HasMaxLength(100);
            builder.Property(e => e.ReturnWindowDays)
                .IsRequired()
                .HasDefaultValue(SocietyCarrierReturnPolicy.DefaultReturnWindowDays);

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // One return-window policy per carrier brand within a society.
            builder.HasIndex(e => new { e.SocietyId, e.CarrierName })
                .IsUnique()
                .HasDatabaseName("UX_SocietyCarrierReturnPolicies_SocietyId_CarrierName");
        }
    }
}
