using HubManagement.Application.PackageHub.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.PackageHub.Services
{
    internal static class PackagePlacement
    {
        public static async Task<Placement> ResolveAsync(
            IRepository<Resident> residents,
            IRepository<Society> societies,
            IRepository<User> users,
            IRepository<UserSociety> userSocieties,
            Guid? residentId,
            Guid? societyId,
            Guid? staffUserId,
            bool requireResidentWhenSocietyMissing)
        {
            Resident? resident = null;
            if (residentId is Guid requestedResident && requestedResident != Guid.Empty)
            {
                resident = await residents.GetByIdAsync(requestedResident).ConfigureAwait(false);
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
                var society = await societies.GetByIdAsync(requestedSociety).ConfigureAwait(false);
                if (society == null)
                    return Placement.Fail(false, "Society was not found.");
                resolvedSociety = society.Id;
            }
            else
            {
                if (staffUserId is not Guid staffId || staffId == Guid.Empty)
                    return Placement.Fail(true, requireResidentWhenSocietyMissing ? "ResidentId or StaffUserId is required" : "A resident, society, or staff user is required");

                var linkedSocieties = userSocieties.FetchMulti(link => link.UserId == staffId)
                    .Select(link => link.SocietyId)
                    .Distinct()
                    .ToList();
                if (linkedSocieties.Count == 0)
                    return Placement.Fail(true, "Staff user is not linked to a society.");
                if (linkedSocieties.Count > 1)
                    return Placement.Fail(true, "SocietyId is required because the staff user is linked to multiple societies.");

                resolvedSociety = linkedSocieties[0];
            }

            Guid resolvedStaff;
            if (staffUserId is Guid requestedStaff && requestedStaff != Guid.Empty)
            {
                var staff = await users.GetByIdAsync(requestedStaff).ConfigureAwait(false);
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

    
}
