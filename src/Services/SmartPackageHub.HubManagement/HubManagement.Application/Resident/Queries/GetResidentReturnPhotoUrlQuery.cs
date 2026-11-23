using HubManagement.Application.HubResidents.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed record GetResidentReturnPhotoUrlQuery(Guid ResidentId, Guid PackageId, Guid PhotoId)
        : Query<ReturnPhotoUrlDto?>;
}
