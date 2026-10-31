using HubManagement.Application.Messages.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Messages.Commands
{
    /// <summary>
    /// Applies Fast2SMS delivery receipts to <see cref="Notification"/> rows.
    /// Delivery is not assumed from HTTP 200 on send — only webhook (or DLR poll) marks Delivered/Failed.
    /// </summary>
    public sealed class ProcessFast2SmsWebhookCommandHandler
        : CreateCommandHandler<ProcessFast2SmsWebhookCommand, Fast2SmsWebhookResult>
    {
        private static readonly Action<ILogger, string, string, Exception?> LogUnknownWebhook =
            LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(1, nameof(ProcessFast2SmsWebhookCommandHandler)),
                "Fast2SMS webhook for unknown request_id={RequestId} status={Status}");

        private readonly IRepository<Notification> _notifications;
        private readonly ILogger<ProcessFast2SmsWebhookCommandHandler> _logger;

        public ProcessFast2SmsWebhookCommandHandler(
            IRepository<Notification> notifications,
            ILogger<ProcessFast2SmsWebhookCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _notifications = notifications;
            _logger = logger;
        }

        public override async Task<Fast2SmsWebhookResult> Handle(
            ProcessFast2SmsWebhookCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var payload = request.Payload;
            if (payload is null || string.IsNullOrWhiteSpace(payload.RequestId))
                return new Fast2SmsWebhookResult(false, false);

            var notification = await _notifications.FirstOrDefaultAsync(
                    n => n.ProviderRequestId == payload.RequestId)
                .ConfigureAwait(false);

            // Also match via udf2 (notification id) when request_id mapping is delayed.
            if (notification is null && Guid.TryParse(payload.Udf2, out var notificationId))
                notification = await _notifications.GetByIdAsync(notificationId).ConfigureAwait(false);

            if (notification is null)
            {
                LogUnknownWebhook(
                    _logger,
                    payload.RequestId,
                    payload.Status ?? string.Empty,
                    null);
                return new Fast2SmsWebhookResult(true, false);
            }

            var status = payload.Status?.Trim() ?? string.Empty;
            if (status.Equals("delivered", StringComparison.OrdinalIgnoreCase)
                || status.Equals("success", StringComparison.OrdinalIgnoreCase))
            {
                if (notification.DeliveredAt is null)
                {
                    notification.MarkAsDelivered();
                    if (UnitOfWork is not null)
                        await UnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else if (status.Equals("failed", StringComparison.OrdinalIgnoreCase)
                     || status.Equals("undelivered", StringComparison.OrdinalIgnoreCase)
                     || status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
            {
                var reason = payload.FailureReason
                    ?? payload.StatusDescription
                    ?? payload.Description
                    ?? "Delivery failed";
                notification.MarkAsFailed(reason);
                if (UnitOfWork is not null)
                    await UnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            return new Fast2SmsWebhookResult(true, true, notification.Id);
        }
    }
}
