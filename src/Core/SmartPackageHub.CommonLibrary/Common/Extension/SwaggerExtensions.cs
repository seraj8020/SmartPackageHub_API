using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SmartPackageHub.Common.ConfigOptions;

namespace SmartPackageHub.Common.Extension
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            SwaggerOptions options;
            IConfiguration? configuration;
            using (var serviceProvider = services.BuildServiceProvider())
            {
                configuration = serviceProvider.GetService<IConfiguration>();
                services.Configure<SwaggerOptions>(configuration!.GetSection("swagger"));
                options = configuration.GetOptions<SwaggerOptions>("swagger");
            }

            if (!options.Enabled)
            {
                return services;
            }

            return services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(options.Version, new OpenApiInfo { Title = options.Title, Version = options.Version });
                c.DescribeAllParametersInCamelCase();
                c.CustomSchemaIds(x => x.FullName);
            });
        }

        public static IApplicationBuilder UseSwaggerDocs(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            var configuration = builder.ApplicationServices.GetService<IConfiguration>();
            if (configuration is null)
            {
                throw new InvalidOperationException("IConfiguration service is not registered in the application services.");
            }

            var options = configuration.GetOptions<SwaggerOptions>("swagger");
            if (!options.Enabled)
            {
                return builder;
            }

            var routePrefix = string.IsNullOrWhiteSpace(options.RoutePrefix) ? "swagger" : options.RoutePrefix;
            builder.UseSwagger();
            builder.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/{routePrefix}/{options.Version ?? ApiVersions.V2}/swagger.json", options.Title);
                c.RoutePrefix = routePrefix;
            });

            return builder;
        }

        public static TModel GetOptions<TModel>(this IConfiguration configuration, string section) where TModel : class
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var model = Activator.CreateInstance<TModel>();
            configuration.GetSection(section).Bind(model);
            return model;
        }
    }
}
