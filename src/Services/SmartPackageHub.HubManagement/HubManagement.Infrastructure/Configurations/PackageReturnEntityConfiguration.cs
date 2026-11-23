using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class PackageReturnEntityConfiguration : IEntityTypeConfiguration<PackageReturn>
    {
        public void Configure(EntityTypeBuilder<PackageReturn> builder)
        {
            ConfigureTableAndKeys(builder);
            ConfigureProperties(builder);
            ConfigureRelationships(builder);
            ConfigureIndexes(builder);
        }

        private static void ConfigureTableAndKeys(EntityTypeBuilder<PackageReturn> builder)
        {
            builder.ToTable("PackageReturns");
            builder.HasKey(e => e.Id);
        }

        private static void ConfigureProperties(EntityTypeBuilder<PackageReturn> builder)
        {
            builder.Property(e => e.PackageId).IsRequired();
            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.ResidentId).IsRequired();
            builder.Property(e => e.Status).HasConversion<string>().IsRequired().HasMaxLength(50);
            builder.Property(e => e.Reason).HasConversion<string>().IsRequired().HasMaxLength(50);
            builder.Property(e => e.Note).HasMaxLength(PackageReturn.NoteMaxLength);
            builder.Property(e => e.RequestedAtUtc).IsRequired();
            builder.Property(e => e.ReceivedAtUtc);
            builder.Property(e => e.ReceivedByStaffUserId);
            builder.Property(e => e.ShelfId);
            builder.Property(e => e.ShelfAssignedAtUtc);
            builder.Property(e => e.CourierPickupNote).HasMaxLength(500);
            builder.Property(e => e.CourierPickupAwb).HasMaxLength(100);
            builder.Property(e => e.ClosedAtUtc);
        }

        private static void ConfigureRelationships(EntityTypeBuilder<PackageReturn> builder)
        {
            builder.HasOne<Package>()
                .WithMany()
                .HasForeignKey(e => e.PackageId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Resident>()
                .WithMany()
                .HasForeignKey(e => e.ResidentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.ReceivedByStaffUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne<Shelf>()
                .WithMany()
                .HasForeignKey(e => e.ShelfId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(e => e.Images)
                .WithOne(i => i.Return!)
                .HasForeignKey(i => i.ReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(e => e.Images).UsePropertyAccessMode(PropertyAccessMode.Field);
        }

        private static void ConfigureIndexes(EntityTypeBuilder<PackageReturn> builder)
        {
            builder.HasIndex(e => new { e.SocietyId, e.Status, e.RequestedAtUtc })
                .HasDatabaseName("IX_PackageReturns_SocietyId_Status_RequestedAtUtc");

            builder.HasIndex(e => new { e.ResidentId, e.Status })
                .HasDatabaseName("IX_PackageReturns_ResidentId_Status");

            builder.HasIndex(e => e.PackageId)
                .HasDatabaseName("IX_PackageReturns_PackageId");

            builder.HasIndex(e => e.PackageId)
                .IsUnique()
                .HasFilter(
                    $"\"Status\" IN ('{PackageReturnStatus.ReturnRequested}', '{PackageReturnStatus.ReturnReceived}', '{PackageReturnStatus.ReturnShelved}')")
                .HasDatabaseName("UX_PackageReturns_PackageId_Open");
        }
    }
}
