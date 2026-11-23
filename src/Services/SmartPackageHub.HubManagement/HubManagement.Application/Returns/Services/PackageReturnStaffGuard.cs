using HubManagement.Application.Returns.DTOs;
using HubManagement.Core.Hubs.Entities;

namespace HubManagement.Application.Returns.Services
{
    internal static class PackageReturnStaffGuard
    {
        public static PackageReturnCommandResult? EnsureSocietyAccess(
            PackageReturn packageReturn,
            IReadOnlyList<Guid> staffSocietyIds)
        {
            if (staffSocietyIds.Contains(packageReturn.SocietyId))
                return null;

            return PackageReturnCommandResult.FailForbidden(
                "Staff is not assigned to this return's society.");
        }
    }
}
