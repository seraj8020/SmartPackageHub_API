using SmartPackageHub.Common.Services.Messaging.Abstractions;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Models;
using SmartPackageHub.Common.ConfigOptions;

namespace HubManagement.Workers
{
    /// <summary>
    /// Drains the Redis outbound queue at a provider-compliant rate (default 1 SMS/sec).
    /// On SMS failure, enqueues a WhatsApp fallback notification.
    /// </summary>
    public sealed class OutboundMessageWorker : BackgroundService
    {
        private static readonly Action<ILogger, Guid, string, Exception?> LogSendFailed =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Warning,
                new EventId(1, nameof(OutboundMessageWorker)),
                "Outbound send failed for notification {NotificationId}: {Reason}");

        private static readonly Action<ILogger, Guid, string, Exception?> LogSubmitted =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Information,
                new EventId(2, nameof(OutboundMessageWorker)),
                "Submitted notification {NotificationId} providerRequestId={ProviderRequestId}");

        private readonly IServiceScopeFactory _scopes;
        private readonly IOutboundMessageQueue _queue;
        private readonly Fast2SmsOptions _smsOptions;
        private readonly OutboundMessageQueueOptions _queueOptions;
        private readonly WhatsAppFallbackOptions _whatsAppOptions;
        private readonly ILogger<OutboundMessageWorker> _logger;
        private DateTime _nextAllowedSendUtc = DateTime.MinValue;

        public OutboundMessageWorker(
            IServiceScopeFactory scopes,
            IOutboundMessageQueue queue,
            IOptions<Fast2SmsOptions> smsOptions,
            IOptions<OutboundMessageQueueOptions> queueOptions,
            IOptions<WhatsAppFallbackOptions> whatsAppOptions,
            ILogger<OutboundMessageWorker> logger)
        {
            _scopes = scopes;
            _queue = queue;
            _smsOptions = smsOptions.Value;
            _queueOptions = queueOptions.Value;
            _whatsAppOptions = whatsAppOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var minInterval = TimeSpan.FromSeconds(1.0 / Math.Max(0.1, _smsOptions.MaxSendsPerSecond));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var item = await _queue.DequeueAsync(stoppingToken).ConfigureAwait(false);
                    if (item is null)
                    {
                        await Task.Delay(_queueOptions.DequeuePollMilliseconds, stoppingToken).ConfigureAwait(false);
                        continue;
                    }

                    var delay = _nextAllowedSendUtc - DateTime.UtcNow;
                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay, stoppingToken).ConfigureAwait(false);

                    await ProcessAsync(item, stoppingToken).ConfigureAwait(false);
                    _nextAllowedSendUtc = DateTime.UtcNow + minInterval;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbound message worker loop error");
                    await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// load notification, send SMS/WhatsApp, mark submitted/failed
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ProcessAsync(OutboundMessageWorkItem item, CancellationToken cancellationToken)
        {
            using var scope = _scopes.CreateScope();
            var notifications = scope.ServiceProvider.GetRequiredService<IRepository<Notification>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var smsSender = scope.ServiceProvider.GetRequiredService<ISmsSender>();
            var whatsAppSender = scope.ServiceProvider.GetRequiredService<IWhatsAppSender>();

            var notification = await notifications.GetByIdAsync(item.NotificationId).ConfigureAwait(false);
            if (notification is null)
                return;

            if (notification.Status is NotificationStatus.Sent or NotificationStatus.Failed
                && !string.IsNullOrEmpty(notification.ProviderRequestId))
            {
                // Idempotent: already submitted to provider.
                return;
            }

            var sendRequest = new SmsSendRequest(
                notification.Channel.Destination,
                notification.Content.Body,
                notification.IdempotencyKey ?? item.IdempotencyKey,
                SenderId: null,
                UserDefinedFields: new Dictionary<string, string>
                {
                    ["udf1"] = notification.IdempotencyKey ?? item.IdempotencyKey,
                    ["udf2"] = notification.Id.ToString("N"),
                    ["udf3"] = notification.PackageId.ToString("N")
                });

            SmsSendResult result;
            if (string.Equals(item.Channel, nameof(ChannelType.WhatsApp), StringComparison.OrdinalIgnoreCase))
                result = await whatsAppSender.SendAsync(sendRequest, cancellationToken).ConfigureAwait(false);
            else
                result = await smsSender.SendAsync(sendRequest, cancellationToken).ConfigureAwait(false);

            if (result.Succeeded)
            {
                notification.MarkAsSubmitted(result.ProviderRequestId ?? $"ok-{notification.Id:N}");
                await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
                LogSubmitted(_logger, notification.Id, notification.ProviderRequestId ?? string.Empty, null);
                return;
            }

            notification.MarkAsFailed(result.ErrorMessage ?? "Provider rejected send.");
            await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogSendFailed(_logger, notification.Id, result.ErrorMessage ?? "unknown", null);

            if (string.Equals(item.Channel, nameof(ChannelType.SMS), StringComparison.OrdinalIgnoreCase)
                && _whatsAppOptions.Enabled)
            {
                await EnqueueWhatsAppFallbackAsync(notifications, unitOfWork, notification, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// on SMS fail, create WhatsApp notification + re-queue
        /// </summary>
        /// <param name="notifications"></param>
        /// <param name="unitOfWork"></param>
        /// <param name="failedSms"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task EnqueueWhatsAppFallbackAsync(
            IRepository<Notification> notifications,
            IUnitOfWork unitOfWork,
            Notification failedSms,
            CancellationToken cancellationToken)
        {
            var fallbackKey = $"wa-fallback:{failedSms.Id:N}";
            var existing = await notifications.FirstOrDefaultAsync(
                n => n.IdempotencyKey == fallbackKey).ConfigureAwait(false);
            if (existing != null)
                return;

            var wa = Notification.Create(
                failedSms.SocietyId,
                failedSms.ResidentId,
                failedSms.PackageId,
                NotificationChannel.Create(ChannelType.WhatsApp, failedSms.Channel.Destination),
                MessageContent.Create(failedSms.Content.TemplateName, failedSms.Content.Body),
                fallbackKey,
                fallbackOfNotificationId: failedSms.Id,
                segmentCount: 1);

            notifications.Insert(wa);
            await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

            await _queue.EnqueueAsync(
                new OutboundMessageWorkItem(
                    wa.Id,
                    ChannelType.WhatsApp.ToString(),
                    fallbackKey,
                    DateTime.UtcNow),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
