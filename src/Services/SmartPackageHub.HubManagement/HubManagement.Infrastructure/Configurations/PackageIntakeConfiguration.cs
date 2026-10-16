using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal class PackageIntakeConfiguration : IEntityTypeConfiguration<PackageIntake>
    {
        public void Configure(EntityTypeBuilder<PackageIntake> builder)
        {
            builder.ToTable("PackageIntakes");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.PackageId).IsRequired();
            builder.Property(e => e.StaffUserId);
            builder.Property(e => e.CourierAgentId);
            builder.Property(e => e.ScannedAt).IsRequired();
            builder.Property(p => p.MatchStatus).HasMaxLength(50);
            builder.Property(p => p.RawOcrText);
            builder.Property(p => p.OcrElapsedMs);
            builder.Property(p => p.ClientRequestId).HasMaxLength(100);
            builder.Property(p => p.Barcode).HasMaxLength(100);
            builder.Property(p => p.IsDamaged).IsRequired();
            builder.Property(p => p.DamageNote).HasMaxLength(500);
            builder.Property(p => p.DamageImagePath).HasMaxLength(500);

            builder.HasOne<Package>()
                .WithOne(p => p.PackageIntake)
                .HasForeignKey<PackageIntake>(e => e.PackageId)
                .IsRequired();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.StaffUserId)
                .IsRequired(false);

            builder.HasOne<CourierAgent>()
                .WithMany()
                .HasForeignKey(e => e.CourierAgentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
