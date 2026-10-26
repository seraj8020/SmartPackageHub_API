using Microsoft.Extensions.DependencyInjection;

namespace HubManagement.Workers
{
    /// <summary>
    /// bind options, register queue/client/worker, OTP + webhook rate limiters
    /// </summary>
    public static class HubWorkersServiceCollectionExtensions
    {
        /// <summary>
        /// Registers background workers (intake notifications + outbound SMS/WhatsApp queue).
        /// Call after AddOutboundMessaging so queue/sender services are available.
        /// </summary>
        public static IServiceCollection AddHubManagementWorkers(this IServiceCollection services)
        {
            services.AddHostedService<IntakeNotificationWorker>();
            services.AddHostedService<OutboundMessageWorker>();
            return services;
        }
    }
}
