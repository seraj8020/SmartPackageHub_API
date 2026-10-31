using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartPackageHub.Common.ConfigOptions;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Services;

namespace SmartPackageHub.Common.Extension
{
    public static class MessagingServiceCollectionExtensions
    {
        /// <summary>
        /// Registers Fast2SMS / WhatsApp / Redis outbound messaging (no hosted workers).
        /// Call <c>AddHubManagementWorkers()</c> from HubManagement.Workers for background processing.
        /// </summary>
        public static IServiceCollection AddOutboundMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.Configure<Fast2SmsOptions>(configuration.GetSection(Fast2SmsOptions.SectionName));
            services.Configure<WhatsAppFallbackOptions>(configuration.GetSection(WhatsAppFallbackOptions.SectionName));
            services.Configure<SmsPumpingProtectionOptions>(configuration.GetSection(SmsPumpingProtectionOptions.SectionName));
            services.Configure<OutboundMessageQueueOptions>(configuration.GetSection(OutboundMessageQueueOptions.SectionName));
            services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

            services.AddSingleton<IOutboundMessageQueue, RedisOutboundMessageQueue>();
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
            services.AddScoped<IOtpSmsDispatcher, OtpSmsDispatcher>();
            services.AddScoped<ISmsPumpingGuard, SmsPumpingGuard>();

            services.AddHttpClient("EmailHttp", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddHttpClient<ISmsSender, Fast2SmsClient>((_, client) =>
            {
                var options = configuration.GetSection(Fast2SmsOptions.SectionName).Get<Fast2SmsOptions>()
                    ?? new Fast2SmsOptions();
                // Absolute path requests (/dev/bulkV2) work without a trailing slash on BaseAddress.
                client.BaseAddress = options.BaseUrl;
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddHttpClient<IWhatsAppSender, WhatsAppFallbackSender>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddHttpClient("Recaptcha");

            return services;
        }
    }
}
