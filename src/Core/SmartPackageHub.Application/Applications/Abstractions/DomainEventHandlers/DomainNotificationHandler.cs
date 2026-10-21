using SmartPackageHub.Domains.Abstractions.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SmartPackageHub.Applications.Abstractions.DomainEventHandlers
{
    public abstract class DomainNotificationHandler<T> : INotificationHandler<T> where T : DomainEvent
    {
        private static readonly Action<ILogger, string, Exception?> LogProcessing =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "DomainEventProcessing"), "Processing domain event: {Handler}");

        private static readonly Action<ILogger, string, Exception?> LogCompleted =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, "DomainEventCompleted"), "Completed processing domain event: {Handler}");

        protected ILogger<DomainNotificationHandler<T>> Logger { get; }

        protected DomainNotificationHandler(ILogger<DomainNotificationHandler<T>> logger)
        {
            Logger = logger;
        }

        public async Task Handle(T notification, CancellationToken cancellationToken)
        {
            LogProcessing(Logger, GetType().Name, null);
            await OnHandleAsync(notification, cancellationToken).ConfigureAwait(false);
            LogCompleted(Logger, GetType().Name, null);
        }

        protected abstract Task OnHandleAsync(T domainEvent, CancellationToken cancellationToken);

        protected static string CorrelationId => System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }
}
