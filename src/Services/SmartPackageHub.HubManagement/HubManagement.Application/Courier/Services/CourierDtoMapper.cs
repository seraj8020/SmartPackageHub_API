using HubManagement.Application.Couriers.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Common.Services.Messaging.Guards;

namespace HubManagement.Application.Courier.Services
{
    internal static class CourierDtoMapper
    {
        public static bool PhonesMatch(string? left, string? right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            if (PhoneNumberGuard.TryNormalizeIndiaMobile(left, out var normalizedLeft) &&
                PhoneNumberGuard.TryNormalizeIndiaMobile(right, out var normalizedRight))
            {
                return string.Equals(normalizedLeft, normalizedRight, StringComparison.Ordinal);
            }

            return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static CourierAgentDto ToAgentDto(CourierAgent agent, string societyName, CourierBlacklist? blacklist) =>
            new()
            {
                Id = agent.Id,
                SocietyId = agent.SocietyId,
                SocietyName = societyName,
                CarrierName = agent.CarrierName,
                AgentName = agent.AgentName,
                Phone = agent.Phone,
                VehicleNumber = agent.VehicleNumber,
                EmployeeOrBadgeNumber = agent.EmployeeOrBadgeNumber,
                IsActive = agent.IsActive,
                IsBlacklisted = blacklist is not null,
                BlacklistReason = blacklist?.Reason,
                CreatedAt = agent.CreatedAt,
            };

        public static CourierBlacklistDto ToBlacklistDto(CourierBlacklist entry, string societyName) =>
            new()
            {
                Id = entry.Id,
                SocietyId = entry.SocietyId,
                SocietyName = societyName,
                CarrierName = entry.CarrierName,
                AgentName = entry.AgentName,
                Phone = entry.Phone,
                Reason = entry.Reason,
                BlacklistedAt = entry.BlacklistedAt,
                IsActive = entry.IsActive,
            };
    }
}