using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.HubManagement.Infrastructure.Repositories;

namespace SmartPackageHub.HubManagement.Infrastructure.AutofacModules
{
    public sealed class InfrastructureModule : Module
    {
        private readonly DbContextOptions<HubDbContext> _options;
        private readonly IConfiguration Configuration;

        public InfrastructureModule(IConfiguration configuration) : this(CreateDbOptions(configuration), configuration)
        {

        }

        public InfrastructureModule(DbContextOptions<HubDbContext> options, IConfiguration configuration)
        {
            Configuration = configuration;
            _options = options;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HubDbContext>()
                .AsSelf()
                .InstancePerLifetimeScope()
                .WithParameter(new NamedParameter("options", _options));

            builder.RegisterType<UnitOfWork>()
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            builder.RegisterGeneric(typeof(Repository<>))
                .As(typeof(IRepository<>))
                .InstancePerLifetimeScope();
            builder.RegisterGeneric(typeof(Logger<>))
                .As(typeof(ILogger<>));
        }

        private static DbContextOptions<HubDbContext> CreateDbOptions(IConfiguration configuration)
        {
            var connectionString = configuration["DbContextSettings:ConnectionString"];
            return new DbContextOptionsBuilder<HubDbContext>()
              .UseNpgsql(connectionString)
              .Options;
        }
    }
}
