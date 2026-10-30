using System.Globalization;
using System.Text;
using AutoMapper;
using HubManagement.Application.Analytics.DTOs;
using MediatR;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed class ExportReportsQueryHandler
        : QueryHandler<ExportReportsQuery, ReportsExportResult>
    {
        private readonly IMediator _mediator;

        public ExportReportsQueryHandler(IMapper mapper, IMediator mediator) : base(mapper)
        {
            _mediator = mediator;
        }

        protected override async Task<ReportsExportResult> HandleAsync(ExportReportsQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var days = GetReportsSummaryQueryHandler.NormalizeDays(request.Days);

            var summary = await _mediator
                .Send(new GetReportsSummaryQuery(days, request.SocietyId))
                .ConfigureAwait(false);
            var series = await _mediator
                .Send(new GetReportsDailySeriesQuery(days, request.SocietyId))
                .ConfigureAwait(false);
            var performance = await _mediator
                .Send(new GetSocietyPerformanceQuery(days, request.SocietyId))
                .ConfigureAwait(false);

            var sb = new StringBuilder();
            sb.AppendLine("section,key,value");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"summary,rangeDays,{summary.RangeDays}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"summary,fromDay,{summary.FromDay:yyyy-MM-dd}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"summary,toDay,{summary.ToDay:yyyy-MM-dd}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"summary,totalPackages,{summary.TotalPackages}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"summary,averageHoldDays,{summary.AverageHoldDays}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"summary,memberPickupRatePercent,{summary.MemberPickupRatePercent}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"summary,received,{summary.Received}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"summary,delivered,{summary.Delivered}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"summary,pending,{summary.Pending}");

            sb.AppendLine();
            sb.AppendLine("day,received,delivered,pending");
            foreach (var point in series)
            {
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"{point.Day:yyyy-MM-dd},{point.Received},{point.Delivered},{point.Pending}");
            }

            sb.AppendLine();
            sb.AppendLine("societyId,societyName,received,delivered,pending,averageHoldDays,memberPickupRatePercent");
            foreach (var row in performance)
            {
                var name = row.SocietyName.Replace('"', '\'');
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"{row.SocietyId},\"{name}\",{row.Received},{row.Delivered},{row.Pending},{row.AverageHoldDays},{row.MemberPickupRatePercent}");
            }

            var fileName = $"sph-reports-{days}d-{summary.ToDay:yyyyMMdd}.csv";
            return new ReportsExportResult(fileName, "text/csv", sb.ToString());
        }
    }
}