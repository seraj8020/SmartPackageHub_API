using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartPackageHub.Common.Services.Storage.Abstractions;
using SmartPackageHub.Common.Services.Storage.Options;
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
                .AddParcelPhotoStore(configuration);
            return services;
        }

        /// <summary>
        /// Parcel / damage photos: Local App_Data (dev) or Google Cloud Storage (QA/Prod).
        /// </summary>
        public static IServiceCollection AddParcelPhotoStore(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            services.Configure<ParcelPhotoStorageOptions>(
                configuration.GetSection(ParcelPhotoStorageOptions.SectionName));

            services.AddSingleton<IParcelPhotoStore>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ParcelPhotoStorageOptions>>().Value;
                if (options.IsGcs)
                    return ActivatorUtilities.CreateInstance<GcsParcelPhotoStore>(sp);

                return ActivatorUtilities.CreateInstance<LocalBlobPhotoStore>(sp);
            });

            return services;
        }

        /// <summary>Backward-compatible alias for older Program.cs call sites.</summary>
        public static IServiceCollection AddLocalParcelPhotoStore(this IServiceCollection services)
        {
            services.AddSingleton<IParcelPhotoStore, LocalBlobPhotoStore>();
            return services;
        }
    }
}
