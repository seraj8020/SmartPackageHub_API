using SmartPackageHub.Common.Services.Messaging.Models;

namespace SmartPackageHub.Common.Services.Messaging.Abstractions
{
    /// <summary>
    /// Protects OTP endpoint from abuse.
    /// </summary>
    public interface ISmsPumpingGuard
    {
        /// <summary>
        /// allow/deny before send
        /// </summary>
        /// <param name="clientIp"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="recaptchaToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SmsPumpingCheckResult> CheckAsync(
            string? clientIp,
            string phoneNumber,
            string? recaptchaToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// bump IP/phone counters after a queued SMS
        /// </summary>
        /// <param name="clientIp"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RecordSuccessAsync(string? clientIp, string phoneNumber, CancellationToken cancellationToken = default);
    }

    
}
