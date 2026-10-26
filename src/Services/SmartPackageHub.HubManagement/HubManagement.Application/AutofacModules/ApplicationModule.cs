using Autofac;
using AutoMapper;
using MediatR;
using MediatR.NotificationPublishers;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace BankingApp.AccountManagement.Infrastructure.AutofacModules
{
    public sealed class ApplicationModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(IMediator).GetTypeInfo().Assembly)
                .Where(e => e != typeof(TaskWhenAllPublisher))
                .AsImplementedInterfaces();

            // Register the DomainEventHandler classes (they implement INotificationHandler<>) in assembly
            builder.RegisterAssemblyTypes(ThisAssembly)
                .AsClosedTypesOf(typeof(INotificationHandler<>))
                .AsSelf();

            // Register the Command and Query handler classes (they implement IRequestHandler<>)
            builder.RegisterAssemblyTypes(ThisAssembly)
                .AsClosedTypesOf(typeof(IRequestHandler<,>))
                .AsSelf();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(ThisAssembly);
            }, NullLoggerFactory.Instance);
            config.AssertConfigurationIsValid();

            builder.Register(c => config)
                .AsSelf()
                .SingleInstance();

            builder.Register(c =>
            {
                var ctx = c.Resolve<IComponentContext>();
                var mapperConfig = c.Resolve<MapperConfiguration>();
                return mapperConfig.CreateMapper(ctx.Resolve);
            }).As<IMapper>()
              .SingleInstance();
        }
    }
}
