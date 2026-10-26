using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class PackageEntityConfiguration : IEntityTypeConfiguration<Package>
    {
        public void Configure(EntityTypeBuilder<Package> builder)
        {
            ConfigureTableAndKeys(builder);
            ConfigureProperties(builder);
            ConfigureOwnedTypes(builder);
            ConfigureRelationships(builder);
        }

        private static void ConfigureTableAndKeys(EntityTypeBuilder<Package> builder)
        {
            builder.ToTable("Packages");
            builder.HasKey(e => e.Id);
        }

        private static void ConfigureProperties(EntityTypeBuilder<Package> builder)
        {
            builder.Property(e => e.PkgNumber).HasMaxLength(100);
            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.ResidentId);
            builder.Property(e => e.ShelfId);
            builder.Property(e => e.StaffUserId).IsRequired();
            builder.Property(e => e.VacationHoldId);
            builder.Property(e => e.CourierAwb).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Category).HasMaxLength(100);
            builder.Property(p => p.CourierPartner).HasMaxLength(100);
            builder.Property(p => p.RecipientName).HasMaxLength(150);
            builder.Property(p => p.TowerWing).HasMaxLength(100);
            builder.Property(p => p.Status).HasConversion<string>().IsRequired().HasMaxLength(50);
            builder.Property(p => p.IsColdStorage).IsRequired();
            builder.Property(p => p.IsCod).IsRequired();
            builder.Property(p => p.CodAmount).HasPrecision(18, 2);
            builder.Property(p => p.CodApprovalStatus).HasConversion<string>().IsRequired().HasMaxLength(50);
            builder.Property(p => p.RecievedAt).IsRequired();
        }

        private static void ConfigureOwnedTypes(EntityTypeBuilder<Package> builder)
        {
            builder.OwnsOne(e => e.SphTrackingNumber, trackingBuilder =>
            {
                trackingBuilder.Property(t => t.Value).HasColumnName("SphTrackingNumber").IsRequired().HasMaxLength(100);
            });

            builder.OwnsOne(e => e.Dimensions, dimBuilder =>
            {
                dimBuilder.Property(d => d.Weight).HasColumnName("Weight").IsRequired();
                dimBuilder.Property(d => d.Height).HasColumnName("Height").IsRequired();
                dimBuilder.Property(d => d.Width).HasColumnName("Width").IsRequired();
                dimBuilder.Property(d => d.Length).HasColumnName("Length").IsRequired();
            });

            builder.OwnsOne(e => e.FlatNumber, flatBuilder =>
            {
                flatBuilder.Property(f => f.Value).HasColumnName("FlatNumber").HasMaxLength(50);
            });
        }

        private static void ConfigureRelationships(EntityTypeBuilder<Package> builder)
        {
            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();

            builder.HasOne<Resident>()
                .WithMany()
                .HasForeignKey(e => e.ResidentId)
                .IsRequired(false);

            builder.HasOne<Shelf>()
                .WithMany()
                .HasForeignKey(e => e.ShelfId)
                .IsRequired(false);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.StaffUserId)
                .IsRequired();

            builder.HasOne<VacationHold>()
                .WithMany()
                .HasForeignKey(e => e.VacationHoldId)
                .IsRequired(false);

            builder.HasOne(p => p.PackageIntake)
                .WithOne()
                .HasForeignKey<PackageIntake>(pi => pi.PackageId);

            builder.HasMany(p => p.Images)
                .WithOne()
                .HasForeignKey(pi => pi.PackageId);

            // Hot staff list filters: society + status (+ received day for Receive).
            builder.HasIndex(e => new { e.SocietyId, e.Status, e.RecievedAt })
                .HasDatabaseName("IX_Packages_SocietyId_Status_RecievedAt");
            // Resident inbox: resident + not-delivered.
            builder.HasIndex(e => new { e.ResidentId, e.Status })
                .HasDatabaseName("IX_Packages_ResidentId_Status");
        }
    }
}
