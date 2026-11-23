using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed record GetResidentReturnPhotoBytesQuery(Guid ResidentId, Guid PackageId, Guid PhotoId)
        : Query<ResidentPhotoBytes?>;
}
