using HubManagement.Application.Analytics.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Analytics.Commands
{
    /// <summary>
    /// Aggregates package/delivery counts into AnalyticsDailies for one calendar day (UTC).
    /// When <paramref name="Day"/> is null, rolls up today UTC after the configured daily cutoff.
    /// </summary>
    public sealed record RollupAnalyticsDailyCommand(DateOnly? Day = null, Guid? SocietyId = null)
        : ICreateCommand<RollupAnalyticsDailyResult>;
}
