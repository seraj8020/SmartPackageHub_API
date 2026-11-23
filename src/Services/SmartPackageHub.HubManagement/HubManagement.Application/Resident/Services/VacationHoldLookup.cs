using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Services
{
    internal static class VacationHoldLookup
    {
        public const string DoorDeliveryBlockedMessage =
            "Door delivery is paused while this resident's vacation hold is active. Keep the package at the hub or use counter pickup after the hold ends.";

        public static async Task<VacationHold?> FindCoveringAsync(
            IRepository<VacationHold> holds,
            Guid residentId,
            DateTime date)
        {
            var active = await holds.ListAsync(
                hold => hold.ResidentId == residentId && hold.IsActive).ConfigureAwait(false);
            return active.FirstOrDefault(hold => hold.CoversDate(date));
        }

        /// <summary>
        /// Blocks door dispatch/handover when the resident has a covering hold or the package
        /// was tagged with a vacation hold at intake.
        /// </summary>
        public static async Task<bool> IsDoorDeliveryBlockedAsync(
            IRepository<VacationHold> holds,
            Package package,
            DateTime date)
        {
            if (package.VacationHoldId.HasValue)
                return true;

            if (package.ResidentId is not Guid residentId || residentId == Guid.Empty)
                return false;

            var covering = await FindCoveringAsync(holds, residentId, date).ConfigureAwait(false);
            return covering is not null;
        }
    }
}
