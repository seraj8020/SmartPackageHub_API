using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class OtpLogEntityConfiguration : IEntityTypeConfiguration<OtpLog>
    {
        public void Configure(EntityTypeBuilder<OtpLog> builder)
        {
            builder.ToTable("OtpLogs");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.ResidentId).IsRequired();
            builder.Property(p => p.PackageId).IsRequired();
            builder.Property(p => p.Purpose).IsRequired().HasMaxLength(50);
            builder.Property(p => p.IsUsed).IsRequired();

            builder.OwnsOne(e => e.Token, tokenBuilder =>
            {
                tokenBuilder.Property(t => t.Value).HasColumnName("Token").IsRequired().HasMaxLength(50);
            });

            builder.OwnsOne(e => e.Expiry, expiryBuilder =>
            {
                expiryBuilder.Property(x => x.Value).HasColumnName("ExpiryTime").IsRequired();
            });

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();

            builder.HasOne<Resident>()
                .WithMany()
                .HasForeignKey(e => e.ResidentId)
                .IsRequired();

            builder.HasOne<Package>()
                .WithMany()
                .HasForeignKey(e => e.PackageId)
                .IsRequired();
        }
    }
}
