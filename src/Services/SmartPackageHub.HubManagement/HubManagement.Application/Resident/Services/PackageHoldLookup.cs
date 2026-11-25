using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Services
{
    /// <summary>
    /// Door vs counter rules for vacation hold and authorized collectors.
    /// </summary>
    internal static class PackageHoldLookup
    {
        public const string DoorDeliveryBlockedMessage =
            "Door delivery is paused while the resident vacation hold is active. Use counter pickup with an authorized collector.";

        public const string CounterBlockedMessage =
            "This package cannot be handed over while the resident vacation hold is active and no authorized collector is set.";

        public static async Task<bool> HasVacationHoldAsync(
            IRepository<VacationHold> holds,
            Package package,
            DateTime utcNow)
        {
            return await VacationHoldLookup.IsDoorDeliveryBlockedAsync(holds, package, utcNow)
                .ConfigureAwait(false);
        }

        public static async Task<AuthorizedCollector?> FindActiveCollectorAsync(
            IRepository<AuthorizedCollector> collectors,
            Guid packageId,
            DateTime utcNow)
        {
            var rows = await collectors.ListAsync(
                    row => row.PackageId == packageId && row.IsActive,
                    cancellationToken: default,
                    noTracking: true)
                .ConfigureAwait(false);

            return rows
                .Where(row => row.IsValidAt(utcNow))
                .OrderByDescending(row => row.AuthorizedAtUtc)
                .FirstOrDefault();
        }

        /// <summary>
        /// Door dispatch / door delivery / tower routes are blocked when vacation hold applies.
        /// </summary>
        public static async Task<bool> IsDoorDeliveryBlockedAsync(
            IRepository<VacationHold> holds,
            Package package,
            DateTime utcNow)
        {
            return await HasVacationHoldAsync(holds, package, utcNow).ConfigureAwait(false);
        }

        /// <summary>
        /// Counter / authorized-collector handover is allowed during vacation hold only when
        /// an active authorized collector exists.
        /// </summary>
        public static async Task<bool> IsCounterHandoverAllowedAsync(
            IRepository<VacationHold> holds,
            IRepository<AuthorizedCollector> collectors,
            Package package,
            DateTime utcNow)
        {
            var vacation = await HasVacationHoldAsync(holds, package, utcNow).ConfigureAwait(false);
            if (!vacation)
                return true;

            var collector = await FindActiveCollectorAsync(collectors, package.Id, utcNow).ConfigureAwait(false);
            return collector is not null;
        }

        public static async Task<string?> GetDispatchBlockMessageAsync(
            IRepository<VacationHold> holds,
            IRepository<AuthorizedCollector> collectors,
            Package package,
            DateTime utcNow)
        {
            if (await IsCounterHandoverAllowedAsync(holds, collectors, package, utcNow).ConfigureAwait(false))
                return null;

            if (await HasVacationHoldAsync(holds, package, utcNow).ConfigureAwait(false))
                return VacationHoldLookup.DoorDeliveryBlockedMessage;

            return null;
        }

        public static async Task<string?> GetDeliverBlockMessageAsync(
            IRepository<VacationHold> holds,
            IRepository<AuthorizedCollector> collectors,
            Package package,
            string? method,
            DateTime utcNow)
        {
            var isCounter = string.Equals(method, "CounterPickup", StringComparison.OrdinalIgnoreCase);
            if (isCounter)
            {
                if (await IsCounterHandoverAllowedAsync(holds, collectors, package, utcNow).ConfigureAwait(false))
                    return null;

                return CounterBlockedMessage;
            }

            if (await IsDoorDeliveryBlockedAsync(holds, package, utcNow).ConfigureAwait(false))
                return DoorDeliveryBlockedMessage;

            return null;
        }
    }
}
