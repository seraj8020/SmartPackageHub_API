using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SmartPackageHub.Common.Extension
{
    public static class CoreInfrastructureExtensions
    {
        public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            services.AddRedis(configuration)
                .AddHttpContextAccessor()
                //.AddJwt(configuration)
                //.ConfigureTokenRequester(configuration)
                .AddSwaggerDocs();
            return services;
        }
    }
}
