using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal class UserEntityConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Username).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Status).IsRequired().HasMaxLength(50);

            builder.OwnsOne(e => e.ContactInfo, contactBuilder =>
            {
                contactBuilder.Property(c => c.Email).HasColumnName("Email").IsRequired().HasMaxLength(150);
            });

            builder.OwnsOne(e => e.Role, roleBuilder =>
            {
                roleBuilder.Property(r => r.Type).HasColumnName("Role").HasConversion<string>().IsRequired().HasMaxLength(50);
            });

            builder.HasMany(e => e.Societies)
                .WithOne()
                .HasForeignKey(us => us.UserId);
        }
    }
}
