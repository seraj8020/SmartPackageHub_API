using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAppDDD.Common.Redis
{
    public static class Extensions
    {
        private static readonly string SectionName = "redis";

        public static IServiceCollection AddRedis(this IServiceCollection services)
        {
            IConfiguration? configuration;
            using (var serviceProvider = services.BuildServiceProvider())
            {
                configuration = serviceProvider.GetService<IConfiguration>();
            }

            if (configuration == null)
            {
                throw new InvalidOperationException("IConfiguration service is not registered in the service collection.");
            }

            services.Configure<RedisOptions>(configuration.GetSection(SectionName));
            var options = new RedisOptions();
            configuration.GetSection(SectionName).Bind(options);
            services.AddStackExchangeRedisCache(o => // Use AddStackExchangeRedisCache instead
            {
                o.Configuration = options.ConnectionString;
                o.InstanceName = options.Instance;
            });

            return services;
        }
    }
}
