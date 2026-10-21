using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class DispatchBatchEntityConfiguration : IEntityTypeConfiguration<DispatchBatch>
    {
        public void Configure(EntityTypeBuilder<DispatchBatch> builder)
        {
            builder.ToTable("DispatchBatches");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.RunnerUserId).IsRequired();
            builder.Property(e => e.TowerWing).HasMaxLength(100);
            builder.Property(e => e.Floor);
            builder.Property(p => p.Status).HasMaxLength(50);
            builder.Property(p => p.CreatedAt).IsRequired();

            builder.OwnsOne(e => e.FlatNumber, flatBuilder =>
            {
                flatBuilder.Property(f => f.Value).HasColumnName("FlatNumber").IsRequired().HasMaxLength(50);
            });

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.RunnerUserId)
                .IsRequired();
        }
    }
}
