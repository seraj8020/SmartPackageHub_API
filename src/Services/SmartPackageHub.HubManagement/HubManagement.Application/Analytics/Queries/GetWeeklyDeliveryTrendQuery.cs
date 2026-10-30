using HubManagement.Application.Analytics.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed record GetWeeklyDeliveryTrendQuery(Guid? SocietyId = null, int Days = 7)
        : Query<IReadOnlyList<WeeklyTrendPointDto>>;
}
