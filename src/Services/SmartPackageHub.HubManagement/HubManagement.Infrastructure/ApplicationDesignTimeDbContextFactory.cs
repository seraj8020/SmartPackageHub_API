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

            // 2. Get the connection string from the configuration
            var connectionString = configuration["DbContextSettings:ConnectionString"];

            // 3. Configure DbContext options
            var optionsBuilder = new DbContextOptionsBuilder<HubDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            // 4. Return the context (Pass configuration instead of builder.Environment if needed)
            return new HubDbContext(optionsBuilder.Options, null);

        }
    }
}
