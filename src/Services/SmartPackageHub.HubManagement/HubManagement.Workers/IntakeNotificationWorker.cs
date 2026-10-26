using HubManagement.Application.Intake.Commands;
using HubManagement.Application.Intake.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HubManagement.Workers
{
    /// <summary>
    /// Drains in-memory intake notification channel and dispatches via MediatR.
    /// </summary>
    public sealed class IntakeNotificationWorker : BackgroundService
    {
        private static readonly Action<ILogger, Guid, Exception?> LogIntakeNotificationFailed =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(IntakeNotificationWorker)),
                "Intake notification failed for {IntakeId}");

        private readonly IntakeNotificationChannel _queue;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<IntakeNotificationWorker> _logger;

        public IntakeNotificationWorker(
            IntakeNotificationChannel queue,
            IServiceScopeFactory scopes,
            ILogger<IntakeNotificationWorker> logger)
        {
            _queue = queue;
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var intakeId in _queue.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new DispatchIntakeNotificationCommand(intakeId), stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogIntakeNotificationFailed(_logger, intakeId, ex);
                }
            }
        }
    }
}
