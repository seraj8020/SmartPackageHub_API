using HubManagement.Application.HubResidents.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.HubResidents.Queries
{
    /// <param name="FromDate">Inclusive start date (calendar day). Null = no lower bound.</param>
    /// <param name="ToDate">Inclusive end date (calendar day). Null = no upper bound.</param>
    public sealed record GetResidentDeliveryHistoryQuery(
        Guid ResidentId,
        DateOnly? FromDate = null,
        DateOnly? ToDate = null) : Query<List<DeliveryHistoryDto>?>;
}
