using SmartPackageHub.Common.Services.Messaging.Models;

namespace SmartPackageHub.Common.Services.Messaging.Abstractions
{
    /// <summary>
    /// Same shape as SMS sender, used for fallback after SMS failure.
    /// </summary>
    public interface IWhatsAppSender
    {
        /// <summary>
        /// send WhatsApp message
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken cancellationToken = default);
    }
}
