using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Services
{
    internal static class VacationHoldLookup
    {
        public static async Task<VacationHold?> FindCoveringAsync(
            IRepository<VacationHold> holds,
            Guid residentId,
            DateTime date)
        {
            var active = await holds.ListAsync(
                hold => hold.ResidentId == residentId && hold.IsActive).ConfigureAwait(false);
            return active.FirstOrDefault(hold => hold.CoversDate(date));
        }
    }
}
