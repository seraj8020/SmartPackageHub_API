using Microsoft.EntityFrameworkCore;
using SmartPackageHub_API.Models;

namespace SmartPackageHub_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Resident> Residents { get; set; }
        public DbSet<Package> Packages { get; set; }
        public DbSet<DeliveryHistory> DeliveryHistories { get; set; }
        public DbSet<OtpCode> OtpCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Resident>().HasKey(r => r.Id);
            modelBuilder.Entity<Package>().HasKey(p => p.Id);
            modelBuilder.Entity<DeliveryHistory>().HasKey(h => h.Id);
            modelBuilder.Entity<OtpCode>().HasKey(o => o.Id);

            modelBuilder.Entity<Resident>()
                .HasMany(r => r.Packages)
                .WithOne(p => p.Resident)
                .HasForeignKey(p => p.ResidentId)
                .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(modelBuilder);
        }
    }
}
