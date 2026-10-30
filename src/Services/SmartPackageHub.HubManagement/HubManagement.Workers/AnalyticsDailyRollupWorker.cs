using HubManagement.Application.Analytics.Commands;
using HubManagement.Application.Analytics.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPackageHub.Common.ConfigOptions;

namespace HubManagement.Workers
{
    /// <summary>
    /// End-of-day style rollup: after <see cref="AnalyticsRollupOptions.RunAfterHourUtc"/> UTC,
    /// upserts yesterday's AnalyticsDailies once per calendar day. Does not poll packages every few minutes.
    /// </summary>
    public sealed class AnalyticsDailyRollupWorker : BackgroundService
    {
        private static readonly Action<ILogger, DateOnly, int, Exception?> LogRollupSucceeded =
            LoggerMessage.Define<DateOnly, int>(
                LogLevel.Information,
                new EventId(1, nameof(AnalyticsDailyRollupWorker)),
                "AnalyticsDaily rollup finished for {Day}; societies={Count}");

        private static readonly Action<ILogger, Exception?> LogRollupFailed =
            LoggerMessage.Define(
                LogLevel.Warning,
                new EventId(2, nameof(AnalyticsDailyRollupWorker)),
                "AnalyticsDaily rollup failed");

        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<AnalyticsRollupOptions> _options;
        private readonly ILogger<AnalyticsDailyRollupWorker> _logger;
        private DateOnly? _lastRolledDay;

        public AnalyticsDailyRollupWorker(
            IServiceScopeFactory scopes,
            IOptionsMonitor<AnalyticsRollupOptions> options,
            ILogger<AnalyticsDailyRollupWorker> logger)
        {
            _scopes = scopes;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var opts = _options.CurrentValue;
                var delay = TimeSpan.FromMinutes(Math.Clamp(opts.CheckIntervalMinutes, 5, 180));

                try
                {
                    if (opts.Enabled)
                        await TryRollupAsync(opts, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogRollupFailed(_logger, ex);
                }

                try
                {
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task TryRollupAsync(AnalyticsRollupOptions opts, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var targetDay = AnalyticsScope.TodayUtc().AddDays(-1);

            if (_lastRolledDay == targetDay)
                return;

            if (now.Hour < Math.Clamp(opts.RunAfterHourUtc, 0, 23))
                return;

            using var scope = _scopes.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator
                .Send(new RollupAnalyticsDailyCommand(targetDay), cancellationToken)
                .ConfigureAwait(false);

            _lastRolledDay = targetDay;
            LogRollupSucceeded(_logger, result.Day, result.SocietiesProcessed, null);
        }
    }
}
