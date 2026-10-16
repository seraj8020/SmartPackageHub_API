using SmartPackageHub.Common.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace SmartPackageHub.Common.Extension
{
    public static class Extensions
    {
        public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
        {
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
            var appSettings = configuration!.Get<AppSettings>();
            return services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(options.Version, new OpenApiInfo { Title = options.Title, Version = options.Version });
                c.DescribeAllParametersInCamelCase();
                c.CustomSchemaIds(x => x.FullName);
            });
        }

        public static IApplicationBuilder UseSwaggerDocs(this IApplicationBuilder builder)
        {
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
            var appSettings = configuration!.Get<AppSettings>();
            var routePrefix = string.IsNullOrWhiteSpace(options.RoutePrefix) ? "swagger" : options.RoutePrefix;
            builder.UseSwagger();
            builder.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/{routePrefix}/{options.Version ?? ApiVersions.V2}/swagger.json", options.Title);
                c.RoutePrefix = routePrefix; // Sets Swagger UI at root/swagger
            });

            return builder;

        }
    }
    
}
public static class Extensions
{
    public static TModel GetOptions<TModel>(this IConfiguration configuration, string section) where TModel : class
    {
        var model = Activator.CreateInstance(typeof(TModel)) as TModel;
        if (model == null)
            throw new InvalidOperationException($"Could not create an instance of type {typeof(TModel).FullName}.");
        configuration.GetSection(section).Bind(model);

        return model;
    }
}
