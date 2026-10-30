using SmartPackageHub.Common.Services.Messaging.Models;

namespace SmartPackageHub.Common.Services.Messaging.Abstractions
{
    /// <summary>
    /// Abstraction for the outbound SMS/WhatsApp work queue.
    /// </summary>
    public interface IOutboundMessageQueue
    {
        /// <summary>
        /// push a work item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task EnqueueAsync(OutboundMessageWorkItem item, CancellationToken cancellationToken = default);

        /// <summary>
        /// pop next item (or null if empty)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<OutboundMessageWorkItem?> DequeueAsync(CancellationToken cancellationToken = default);
    }

    
}
