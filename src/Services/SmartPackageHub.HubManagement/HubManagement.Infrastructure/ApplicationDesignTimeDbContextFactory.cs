using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SmartPackageHub.HubManagement.Infrastructure
{
    public class ApplicationDesignTimeDbContextFactory : IDesignTimeDbContextFactory<HubDbContext>
    {
        public HubDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .Build();

            var connectionString = configuration["DbContextSettings:ConnectionString"];
            var optionsBuilder = new DbContextOptionsBuilder<HubDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            return new HubDbContext(optionsBuilder.Options);
        }
    }
}
