using HubManagement.Application.Intake;
using HubManagement.Application.Intake.Commands;
using MediatR;

namespace SmartPackageHub.HubManagement.Api.Intake
{
    public sealed class IntakeNotificationWorker : BackgroundService
    {
        private readonly IntakeNotificationQueue _queue;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<IntakeNotificationWorker> _logger;

        public IntakeNotificationWorker(
            IntakeNotificationQueue queue,
            IServiceScopeFactory scopes,
            ILogger<IntakeNotificationWorker> logger)
        {
            _queue = queue;
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var intakeId in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new DispatchIntakeNotificationCommand(intakeId), stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Intake notification failed for {IntakeId}", intakeId);
                }
            }
        }
    }
}
