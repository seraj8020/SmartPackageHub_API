using SmartPackageHub.Common.Cache;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;

namespace SmartPackageHub.Common.Extension
{
    public static class HostDiExtensions
    {

        public static IServiceCollection AddWebHostInfrastructure(this IServiceCollection services, IConfiguration configuration, string serviceName)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);
            services
                .AddHostOpenTelemetry(configuration, serviceName);
            return services;
        }

        public static void AddHostLogging(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
        }
        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var redisConnectionString = configuration
            .GetValue<string>("RedisConnectionString:Redis")
            ?? throw new InvalidOperationException("RedisConnectionString:Redis section was not found");

            ConfigurationOptions option = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
               // ClientName = "MyApp-RedisCacheProvider",
                //ReconnectRetryPolicy = new ExponentialRetry(5000, 10000),
                EndPoints = { redisConnectionString }
            };

            IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(option);
            services
                .AddSingleton<ICacheService, CacheService>()
                .AddSingleton(connectionMultiplexer)
                .AddStackExchangeRedisCache(options =>
                {

                    options.ConnectionMultiplexerFactory = () => Task.FromResult(connectionMultiplexer);
                });

            return services;
        }
        private static IServiceCollection AddHostOpenTelemetry(this IServiceCollection services, IConfiguration configuration, string serviceName)
        {
            services
                .AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(serviceName))
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRedisInstrumentation()
                        .AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName)
                        .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
                        .AddSource("MailKit");

                    var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                    if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
                    {
                        tracing.AddOtlpExporter(options => options.Endpoint = endpoint);
                    }
                });

            return services;
        }
    }
}
