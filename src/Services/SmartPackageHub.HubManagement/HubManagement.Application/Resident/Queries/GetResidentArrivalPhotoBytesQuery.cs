using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed record GetResidentArrivalPhotoBytesQuery(Guid ResidentId, Guid PackageId)
        : Query<ResidentPhotoBytes?>;
}
