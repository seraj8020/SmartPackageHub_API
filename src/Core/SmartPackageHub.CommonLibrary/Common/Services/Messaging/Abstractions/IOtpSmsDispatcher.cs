using SmartPackageHub.Common.Services.Messaging.Models;

namespace SmartPackageHub.Common.Services.Messaging.Abstractions
{
    /// <summary>
    /// Creates a pending SMS notification and enqueues it for rate-limited delivery.
    /// </summary>
    public interface IOtpSmsDispatcher
    {
        /// <summary>
        /// validate phone, build GSM-safe body, create Notification, enqueue
        /// </summary>
        /// <param name="societyId"></param>
        /// <param name="residentId"></param>
        /// <param name="packageId"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="otpCode"></param>
        /// <param name="templateName"></param>
        /// <param name="messageBody"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<OtpSmsDispatchResult> QueuePickupOtpAsync(
            Guid societyId,
            Guid residentId,
            Guid packageId,
            string phoneNumber,
            string otpCode,
            string templateName,
            string messageBody,
            CancellationToken cancellationToken = default);
    }

    
}
