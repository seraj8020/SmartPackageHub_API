using HubManagement.Core.Hubs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Domains.Abstractions.Entities;
using SmartPackageHub.HubManagement.Infrastructure.Configurations;

namespace SmartPackageHub.HubManagement.Infrastructure
{
    public sealed class HubDbContext : DbContext
    {
        private static readonly ILoggerFactory DebugLoggerFactory = new LoggerFactory(new[] { new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider() });
        private readonly IHostEnvironment? _env;

        public HubDbContext(DbContextOptions<HubDbContext> options, IHostEnvironment? env
            ) : base(options)
        {
            _env = env;
        }

        public DbSet<Society> Societies { get; set; }
        public DbSet<Resident> Residents { get; set; }
        public DbSet<Package> Packages { get; set; }
        public DbSet<PackageIntake> PackageIntakes { get; set; }
        public DbSet<PackageImage> PackageImages { get; set; }
        public DbSet<Route> Routes { get; set; }
        public DbSet<RouteStop> RouteStops { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserSociety> UsersSocieties { get; set; }
        public DbSet<Shelf> Shelves { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<OTPLog> OTPLogs { get; set; }
        public DbSet<AnalyticsDaily> AnalyticsDailies { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<DispatchBatch> DispatchBatches { get; set; }
        public DbSet<GuestAccess> GuestAccesses { get; set; }
        public DbSet<VacationHold> VacationHolds { get; set; }
        public DbSet<CourierBlacklist> CourierBlacklists { get; set; }
        public DbSet<CourierAgent> CourierAgents { get; set; }
        public DbSet<SecureBag> SecureBags { get; set; }
        public DbSet<DigitalSignOffReceipt> DigitalSignOffReceipts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_env != null && _env.IsDevelopment())
            {
                // used to print activity when debugging
                optionsBuilder.UseLoggerFactory(DebugLoggerFactory);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnalyticsDailyEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditLogsEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CourierBlacklistEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DeliveryEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DispatchBatchEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(GuestAccessEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MembershipEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OTPLogEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PackageEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PackageIntakeConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PackageImageConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResidentEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(RouteEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(RouteStopEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShelfEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SocietyEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserSocietyEntityConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(VacationHoldEntityConfiguration).Assembly);

            var aggregateTypes = modelBuilder.Model
                                             .GetEntityTypes()
                                             .Select(e => e.ClrType)
                                             .Where(e => !e.IsAbstract && e.IsAssignableTo(typeof(EntityBase)));

            foreach (var type in aggregateTypes)
            {
                var aggregateBuild = modelBuilder.Entity(type);
                aggregateBuild.Ignore(nameof(EntityBase.DomainEvents));
                aggregateBuild.Property(nameof(EntityBase.Id)).ValueGeneratedNever();
            }
        }
    }
}
