using HubManagement.Application.Analytics.Commands;
using HubManagement.Application.Analytics.DTOs;
using HubManagement.Application.Analytics.Queries;
using HubManagement.Application.Auth.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.ConfigOptions;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    /// <summary>
    /// Admin dashboard and reports analytics.
    /// Requires Bearer admin session. PlatformAdmin may omit <c>societyId</c> (all societies).
    /// Society Admin is limited to their linked societies.
    /// Dates and rollups use UTC calendar days.
    /// </summary>
    [Route("api/v{version:apiVersion}/analytics")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly PlatformAdminSessionStore _sessions;

        public AnalyticsController(IMediator mediator, PlatformAdminSessionStore sessions)
        {
            _mediator = mediator;
            _sessions = sessions;
        }

        /// <summary>Live dashboard KPIs for today.</summary>
        [HttpGet("dashboard/summary")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetDashboardSummary(
            [FromQuery] Guid? societyId,
            CancellationToken cancellationToken)
        {
            if (!TryResolveSocietyScope(societyId, out var scope, out var error))
                return error!;
            var result = await _mediator
                .Send(new GetDashboardSummaryQuery(scope), cancellationToken)
                .ConfigureAwait(false);
            return Ok(result);
        }

        /// <summary>Received vs delivered trend (default last 7 days). Prefers AnalyticsDaily when present.</summary>
        [HttpGet("dashboard/weekly-trend")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(IReadOnlyList<WeeklyTrendPointDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetWeeklyTrend(
            [FromQuery] Guid? societyId,
            [FromQuery] int days = 7,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveSocietyScope(societyId, out var scope, out var error))
                return error!;
            var result = await _mediator
                .Send(new GetWeeklyDeliveryTrendQuery(scope, days), cancellationToken)
                .ConfigureAwait(false);
            return Ok(result);
        }

        /// <summary>Courier partner distribution for packages received in the period.</summary>
        [HttpGet("dashboard/courier-split")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(IReadOnlyList<CourierSplitSliceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetCourierSplit(
            [FromQuery] Guid? societyId,
            [FromQuery] int days = 7,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveSocietyScope(societyId, out var scope, out var error))
                return error!;
            var result = await _mediator
                .Send(new GetCourierSplitQuery(scope, days), cancellationToken)
                .ConfigureAwait(false);
            return Ok(result);
        }

        /// <summary>Recent intake / shelf / pickup events, newest first.</summary>
        [HttpGet("dashboard/recent-activity")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(IReadOnlyList<RecentActivityItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetRecentActivity(
            [FromQuery] Guid? societyId,
            [FromQuery] int take = 20,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveSocietyScope(societyId, out var scope, out var error))
                return error!;
            var result = await _mediator
                .Send(new GetRecentActivityQuery(scope, take), cancellationToken)
                .ConfigureAwait(false);
            return Ok(result);
        }

        /// <summary>
        /// Reports summary KPIs for 7 / 30 / 90 days.
        /// Total packages = received in range; avg hold = mean delivery lag in days;
        /// member pickup rate = delivered/received × 100.
        /// </summary>
        [HttpGet("reports/summary")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ReportsSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetReportsSummary(
            [FromQuery] int days = 7,
            [FromQuery] Guid? societyId = null,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveSocietyScope(societyId, out var scope, out var error))
                return error!;
            var result = await _mediator
                .Send(new GetReportsSummaryQuery(days, scope), cancellationToken)
                .ConfigureAwait(false);
            return Ok(result);
        }

        /// <summary>Packages by day: received / delivered / pending for the range.</summary>
        [HttpGet("reports/daily")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(IReadOnlyList<ReportsDailyPointDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetReportsDaily(
            [FromQuery] int days = 7,
            [FromQuery] Guid? societyId = null,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveSocietyScope(societyId, out var scope, out var error))
                return error!;
            var result = await _mediator
                .Send(new GetReportsDailySeriesQuery(days, scope), cancellationToken)
                .ConfigureAwait(false);
            return Ok(result);
        }

        /// <summary>Per-society performance for the range (platform sees all; society filter optional).</summary>
        [HttpGet("reports/society-performance")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(IReadOnlyList<SocietyPerformanceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetSocietyPerformance(
            [FromQuery] int days = 7,
            [FromQuery] Guid? societyId = null,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveSocietyScope(societyId, out var scope, out var error))
                return error!;
            var result = await _mediator
                .Send(new GetSocietyPerformanceQuery(days, scope), cancellationToken)
                .ConfigureAwait(false);
            return Ok(result);
        }

        /// <summary>CSV export of summary, daily series, and society performance.</summary>
        [HttpGet("reports/export")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> ExportReports(
            [FromQuery] int days = 7,
            [FromQuery] Guid? societyId = null,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveSocietyScope(societyId, out var scope, out var error))
                return error!;
            var result = await _mediator
                .Send(new ExportReportsQuery(days, scope), cancellationToken)
                .ConfigureAwait(false);
            var bytes = System.Text.Encoding.UTF8.GetBytes(result.CsvContent);
            return File(bytes, result.ContentType, result.FileName);
        }

        /// <summary>
        /// Manually trigger AnalyticsDaily rollup for a day (default: yesterday UTC).
        /// Platform admin only. Prefer the background worker at hub end-of-day.
        /// </summary>
        [HttpPost("rollup")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(RollupAnalyticsDailyResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> Rollup(
            [FromQuery] DateOnly? day = null,
            [FromQuery] Guid? societyId = null,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetAdminSession(out var session, out var error))
                return error!;
            if (!string.Equals(session!.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Platform admin only." });

            var result = await _mediator
                .Send(new RollupAnalyticsDailyCommand(day, societyId), cancellationToken)
                .ConfigureAwait(false);
            return Ok(result);
        }

        private bool TryGetAdminSession(out PlatformAdminSession? session, out ActionResult? error)
        {
            session = null;
            error = null;
            var auth = Request.Headers.Authorization.ToString();
            var token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? auth["Bearer ".Length..].Trim()
                : null;
            if (string.IsNullOrWhiteSpace(token) || !_sessions.TryGetSession(token, out session))
            {
                error = Unauthorized(new { Message = "Admin sign-in required." });
                return false;
            }

            return true;
        }

        /// <summary>
        /// PlatformAdmin: null scope = all societies; optional societyId filter allowed.
        /// Admin: must use a linked society (defaults to first linked society when omitted).
        /// </summary>
        private bool TryResolveSocietyScope(
            Guid? requestedSocietyId,
            out Guid? effectiveSocietyId,
            out ActionResult? error)
        {
            effectiveSocietyId = null;
            error = null;
            if (!TryGetAdminSession(out var session, out error))
                return false;

            var isPlatform = string.Equals(session!.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);
            if (isPlatform)
            {
                effectiveSocietyId = requestedSocietyId is null || requestedSocietyId == Guid.Empty
                    ? null
                    : requestedSocietyId;
                return true;
            }

            if (session.SocietyIds.Count == 0)
            {
                error = StatusCode(StatusCodes.Status403Forbidden, new { Message = "No society linked to this admin." });
                return false;
            }

            if (requestedSocietyId is null || requestedSocietyId == Guid.Empty)
            {
                effectiveSocietyId = session.SocietyIds[0];
                return true;
            }

            if (!session.SocietyIds.Contains(requestedSocietyId.Value))
            {
                error = StatusCode(StatusCodes.Status403Forbidden, new { Message = "Society is outside this admin's scope." });
                return false;
            }

            effectiveSocietyId = requestedSocietyId;
            return true;
        }
    }
}
