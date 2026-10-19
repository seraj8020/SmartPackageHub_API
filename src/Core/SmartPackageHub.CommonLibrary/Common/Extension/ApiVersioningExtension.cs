
using Microsoft.Extensions.DependencyInjection;
using SmartPackageHub.Common.Types;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace SmartPackageHub.Common.Extension
{
    public static class ApiVersioningExtension
    {
        public static IServiceCollection AddApiVersioning(this IServiceCollection services, string apiVersion = ApiVersions.V2)
        {
            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = ApiVersion.Parse(apiVersion);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            });

            services.AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            return services;
        }
    }
}
