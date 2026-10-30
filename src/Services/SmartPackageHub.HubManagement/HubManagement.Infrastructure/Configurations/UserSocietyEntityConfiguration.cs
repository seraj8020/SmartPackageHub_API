using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartPackageHub.HubManagement.Infrastructure.Configurations
{
    internal sealed class UserSocietyEntityConfiguration : IEntityTypeConfiguration<UserSociety>
    {
        public void Configure(EntityTypeBuilder<UserSociety> builder)
        {
            builder.ToTable("UserSocieties");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.UserId).IsRequired();
            builder.Property(e => e.SocietyId).IsRequired();

            builder.HasOne<User>()
                .WithMany(u => u.Societies)
                .HasForeignKey(e => e.UserId)
                .IsRequired();

            builder.HasOne<Society>()
                .WithMany()
                .HasForeignKey(e => e.SocietyId)
                .IsRequired();
        }
    }
}
