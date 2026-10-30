using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartPackageHub.Common.Services.Storage.Abstractions;
using SmartPackageHub.Common.Services.Storage.Services;

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
                .AddSwaggerDocs()
                .AddLocalParcelPhotoStore();
            return services;
        }

        /// <summary>
        /// Local App_Data blob store for parcel / damage photos.
        /// </summary>
        public static IServiceCollection AddLocalParcelPhotoStore(this IServiceCollection services)
        {
            services.AddSingleton<IParcelPhotoStore, LocalBlobPhotoStore>();
            return services;
        }
    }
}
