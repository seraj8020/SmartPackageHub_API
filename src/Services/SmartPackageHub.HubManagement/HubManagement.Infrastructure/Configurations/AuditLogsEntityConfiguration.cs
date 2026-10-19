using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal class AuditLogsEntityConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SocietyId).IsRequired();
            builder.Property(e => e.UserId).IsRequired();
            builder.Property(p => p.EntityName).IsRequired().HasMaxLength(200);
            builder.Property(p => p.EntityId).IsRequired();
            builder.Property(p => p.Action).IsRequired().HasMaxLength(100);
            builder.Property(p => p.ExecutedAt).IsRequired();

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired();
        }
    }
}
