using HubManagement.Core.Hubs.Entities;

namespace HubManagement.Application.PackageHub.Services
{
    internal static class PackageStaffGuard
    {
        public static bool HasSocietyAccess(Package package, IReadOnlyList<Guid> staffSocietyIds) =>
            staffSocietyIds.Contains(package.SocietyId);
    }
}
