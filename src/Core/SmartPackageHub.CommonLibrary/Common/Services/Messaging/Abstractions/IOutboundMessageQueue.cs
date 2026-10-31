using System.Diagnostics.CodeAnalysis;
using SmartPackageHub.Common.Services.Messaging.Models;

namespace SmartPackageHub.Common.Services.Messaging.Abstractions
{
    /// <summary>
    /// Abstraction for the outbound SMS/WhatsApp work queue.
    /// </summary>
    [SuppressMessage(
        "Naming",
        "CA1711:Identifiers should not have incorrect suffix",
        Justification = "Queue is intentional domain naming for the Redis-backed outbound work buffer.")]
    public interface IOutboundMessageQueue
    {
        Task EnqueueAsync(OutboundMessageWorkItem item, CancellationToken cancellationToken = default);

        Task<OutboundMessageWorkItem?> DequeueAsync(CancellationToken cancellationToken = default);
    }
}
