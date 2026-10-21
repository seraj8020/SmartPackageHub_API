using HubManagement.Application.HubResidents.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed record GetResidentDeliveryHistoryQuery(Guid ResidentId) : Query<List<DeliveryHistoryDto>?>;

    
}
