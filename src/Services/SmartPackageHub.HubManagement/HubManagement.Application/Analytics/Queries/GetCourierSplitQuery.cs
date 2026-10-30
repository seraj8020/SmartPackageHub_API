using HubManagement.Application.Analytics.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Analytics.Queries
{
    public sealed record GetCourierSplitQuery(Guid? SocietyId = null, int Days = 7)
        : Query<IReadOnlyList<CourierSplitSliceDto>>;
}
