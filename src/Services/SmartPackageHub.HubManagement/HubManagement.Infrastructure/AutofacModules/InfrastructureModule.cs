using Autofac;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.HubManagement.Infrastructure.Repositories;

namespace SmartPackageHub.HubManagement.Infrastructure.AutofacModules
{
    public sealed class InfrastructureModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // HubDbContext comes from MS DI AddDbContextPool — do not re-register here
            // or Autofac would create a new context per scope and defeat pooling.

            builder.RegisterType<UnitOfWork>()
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            builder.RegisterGeneric(typeof(Repository<>))
                .As(typeof(IRepository<>))
                .InstancePerLifetimeScope();
            builder.RegisterGeneric(typeof(Logger<>))
                .As(typeof(ILogger<>));
        }
    }
}
