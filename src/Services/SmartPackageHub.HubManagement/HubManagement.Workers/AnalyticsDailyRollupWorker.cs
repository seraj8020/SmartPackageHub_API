using HubManagement.Application.Analytics.Commands;
using HubManagement.Application.Analytics.Services;
using HubManagement.Core.Hubs.Entities;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPackageHub.Common.ConfigOptions;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Workers
{
    /// <summary>
    /// Daily rollup: after <see cref="AnalyticsRollupOptions.RunAfterHourUtc"/> UTC,
    /// upserts today's AnalyticsDailies once per calendar day.
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
            var targetDay = AnalyticsScope.TodayUtc();
            var runAfterHourUtc = Math.Clamp(opts.RunAfterHourUtc, 0, 23);
            var runAfterMinuteUtc = Math.Clamp(opts.RunAfterMinuteUtc, 0, 59);
            if (now.Hour < runAfterHourUtc ||
                (now.Hour == runAfterHourUtc && now.Minute < runAfterMinuteUtc))
                return;

            if (_lastRolledDay == targetDay)
                return;

            using var scope = _scopes.CreateScope();
            var analytics = scope.ServiceProvider.GetRequiredService<IRepository<AnalyticsDaily>>();
            var societies = scope.ServiceProvider.GetRequiredService<IRepository<Society>>();
            var cutoffUtc = DateTime.SpecifyKind(
                targetDay.ToDateTime(new TimeOnly(runAfterHourUtc, runAfterMinuteUtc)),
                DateTimeKind.Utc);
            var societyCount = societies.GetAll().Count();
            var rolledSocietyCount = analytics.GetAll()
                .Where(row => row.Day == targetDay && row.LastRolledUpAtUtc >= cutoffUtc &&
                    row.PendingCountVersion >= 1)
                .Select(row => row.SocietyId)
                .Distinct()
                .Count();
            var alreadyRolledToday = societyCount == 0 || rolledSocietyCount >= societyCount;
            if (alreadyRolledToday)
            {
                _lastRolledDay = targetDay;
                return;
            }

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var firstDayToRefresh = targetDay.AddDays(-6);
            for (var day = firstDayToRefresh; day <= targetDay; day = day.AddDays(1))
            {
                var result = await mediator
                    .Send(new RollupAnalyticsDailyCommand(day), cancellationToken)
                    .ConfigureAwait(false);
                LogRollupSucceeded(_logger, result.Day, result.SocietiesProcessed, null);
            }

            _lastRolledDay = targetDay;
        }
    }
}
