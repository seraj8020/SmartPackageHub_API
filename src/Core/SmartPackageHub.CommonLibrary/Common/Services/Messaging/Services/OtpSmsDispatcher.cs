using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.ConfigOptions;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Encoding;
using SmartPackageHub.Common.Services.Messaging.Guards;
using SmartPackageHub.Common.Services.Messaging.Models;

namespace SmartPackageHub.Common.Services.Messaging.Services
{
    public sealed class OtpSmsDispatcher : IOtpSmsDispatcher
    {
        private static readonly Action<ILogger, Guid, string, Exception?> LogQueued =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Information,
                new EventId(1, nameof(OtpSmsDispatcher)),
                "Queued pickup OTP SMS for package {PackageId} key {IdempotencyKey}");

        private static readonly Action<ILogger, string, Exception?> LogRejectedPhone =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(2, nameof(OtpSmsDispatcher)),
                "Rejected OTP SMS destination (India-only pumping guard): {Phone}");

        private readonly IRepository<Notification> _notifications;
        private readonly IOutboundMessageQueue _queue;
        private readonly IUnitOfWork _unitOfWork;
        private readonly SmsPumpingProtectionOptions _pumping;
        private readonly ILogger<OtpSmsDispatcher> _logger;

        public OtpSmsDispatcher(
            IRepository<Notification> notifications,
            IOutboundMessageQueue queue,
            IUnitOfWork unitOfWork,
            IOptions<SmsPumpingProtectionOptions> pumping,
            ILogger<OtpSmsDispatcher> logger)
        {
            _notifications = notifications;
            _queue = queue;
            _unitOfWork = unitOfWork;
            _pumping = pumping.Value;
            _logger = logger;
        }
        /// <summary>
        /// – India phone check, GSM encode, idempotent insert, Redis enqueue
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
        public async Task<OtpSmsDispatchResult> QueuePickupOtpAsync(
            Guid societyId,
            Guid residentId,
            Guid packageId,
            string phoneNumber,
            string otpCode,
            string templateName,
            string messageBody,
            CancellationToken cancellationToken = default)
        {
            if (!PhoneNumberGuard.TryNormalizeIndiaMobile(phoneNumber, out var local10))
            {
                if (_pumping.RestrictToIndia)
                {
                    LogRejectedPhone(_logger, phoneNumber, null);
                    return new OtpSmsDispatchResult(false, null, null, "SMS is restricted to valid India mobile numbers (+91).");
                }

                local10 = phoneNumber.Trim();
            }

            var gsmBody = SmsEncodingHelper.ToGsm7SafeOtpMessage(messageBody);
            var segments = SmsEncodingHelper.EstimateSegmentCount(gsmBody);
            // Prefer single-segment OTP: truncate carefully if somehow over limit.
            if (segments > 1 && gsmBody.Length > 160)
                gsmBody = gsmBody[..157] + "...";

            segments = SmsEncodingHelper.EstimateSegmentCount(gsmBody);
            var idempotencyKey = $"otp:{packageId:N}:{otpCode}:{ChannelType.SMS}";

            var existing = await _notifications.FirstOrDefaultAsync(
                n => n.IdempotencyKey == idempotencyKey).ConfigureAwait(false);
            if (existing != null)
            {
                return new OtpSmsDispatchResult(true, existing.Id, existing.IdempotencyKey, "Already queued (idempotent).");
            }

            var notification = Notification.Create(
                societyId,
                residentId,
                packageId,
                NotificationChannel.Create(ChannelType.SMS, local10),
                MessageContent.Create(templateName, gsmBody),
                idempotencyKey,
                fallbackOfNotificationId: null,
                segmentCount: segments);

            _notifications.Insert(notification);
            await _unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

            await _queue.EnqueueAsync(
                new OutboundMessageWorkItem(
                    notification.Id,
                    ChannelType.SMS.ToString(),
                    idempotencyKey,
                    DateTime.UtcNow),
                cancellationToken).ConfigureAwait(false);

            LogQueued(_logger, packageId, idempotencyKey, null);
            return new OtpSmsDispatchResult(true, notification.Id, idempotencyKey, null);
        }
    }
}
