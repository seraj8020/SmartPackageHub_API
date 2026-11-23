using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.HubResidents.Queries
{
    public sealed record GetResidentDamagePhotoBytesQuery(Guid ResidentId, Guid PackageId)
        : Query<ResidentPhotoBytes?>;
}
