using HubManagement.Core.Hubs.Entities;

namespace HubManagement.Application.PackageHub.DTOs
{
    internal sealed record Placement(bool Found, bool Succeeded, string Message, Resident? Resident, Guid SocietyId, Guid StaffUserId)
    {
        public static Placement Ok(Resident? resident, Guid societyId, Guid staffUserId) =>
            new(true, true, string.Empty, resident, societyId, staffUserId);

        public static Placement Fail(bool found, string message) =>
            new(found, false, message, null, Guid.Empty, Guid.Empty);
    }
}
