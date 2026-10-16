using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.PackageHub
{
    internal static class PackagePlacement
    {
        public static async Task<Placement> ResolveAsync(
            IRepository<Resident> residents,
            IRepository<Society> societies,
            IRepository<User> users,
            Guid? residentId,
            Guid? societyId,
            Guid? staffUserId,
            bool requireResidentWhenSocietyMissing)
        {
            Resident? resident = null;
            if (residentId is Guid requestedResident && requestedResident != Guid.Empty)
            {
                resident = await residents.GetByIdAsync(requestedResident);
                if (resident == null)
                    return Placement.Fail(false, "Resident was not found.");
            }

            Guid resolvedSociety;
            if (resident != null)
            {
                resolvedSociety = resident.SocietyId;
            }
            else if (societyId is Guid requestedSociety && requestedSociety != Guid.Empty)
            {
                var society = await societies.GetByIdAsync(requestedSociety);
                if (society == null)
                    return Placement.Fail(false, "Society was not found.");
                resolvedSociety = society.Id;
            }
            else if (requireResidentWhenSocietyMissing)
            {
                return Placement.Fail(true, "ResidentId is required");
            }
            else
            {
                return Placement.Fail(true, "A resident or society is required");
            }

            Guid resolvedStaff;
            if (staffUserId is Guid requestedStaff && requestedStaff != Guid.Empty)
            {
                var staff = await users.GetByIdAsync(requestedStaff);
                if (staff == null)
                    return Placement.Fail(false, "Staff user was not found.");
                resolvedStaff = staff.Id;
            }
            else if (resident != null)
            {
                resolvedStaff = resident.UserId;
            }
            else
            {
                return Placement.Fail(true, "StaffUserId is required");
            }

            return Placement.Ok(resident, resolvedSociety, resolvedStaff);
        }
    }

    internal sealed record Placement(bool Found, bool Succeeded, string Message, Resident? Resident, Guid SocietyId, Guid StaffUserId)
    {
        public static Placement Ok(Resident? resident, Guid societyId, Guid staffUserId) =>
            new(true, true, string.Empty, resident, societyId, staffUserId);

        public static Placement Fail(bool found, string message) =>
            new(found, false, message, null, Guid.Empty, Guid.Empty);
    }
}
