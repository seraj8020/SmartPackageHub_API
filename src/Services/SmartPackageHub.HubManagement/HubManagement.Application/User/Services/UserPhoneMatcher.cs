using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Common.Services.Messaging.Guards;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubUsers.Services
{
    internal static class UserPhoneMatcher
    {
        public static bool PhoneNumbersMatch(string? left, string? right)
        {
            if (PhoneNumberGuard.TryNormalizeIndiaMobile(left, out var normalizedLeft) &&
                PhoneNumberGuard.TryNormalizeIndiaMobile(right, out var normalizedRight))
                return string.Equals(normalizedLeft, normalizedRight, StringComparison.Ordinal);

            return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static string? FindDuplicateField(
            IRepository<User> users,
            IRepository<Resident> residents,
            string username,
            string email,
            string phone)
        {
            var userList = users.GetAll().AsEnumerable().ToList();
            if (userList.Any(user => user.Role.Type != RoleType.Resident &&
                string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)))
                return "Username";
            if (userList.Any(user => string.Equals(user.ContactInfo?.Email, email, StringComparison.OrdinalIgnoreCase)))
                return "Email";
            if (userList.Any(user => PhoneNumbersMatch(user.PhoneNumber, phone)))
                return "Phone";

            var residentPhones = residents.GetAll().AsEnumerable()
                .Where(resident => resident.PhoneNumber != null)
                .Select(resident => resident.PhoneNumber!)
                .ToList();
            if (residentPhones.Any(existing => PhoneNumbersMatch(existing, phone)))
                return "Phone";
            return null;
        }

        public static bool PhoneExists(
            IRepository<User> users,
            IRepository<Resident> residents,
            Guid excludedUserId,
            string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            var userPhones = users.GetAll().AsEnumerable()
                .Where(user => user.Id != excludedUserId && user.PhoneNumber != null)
                .Select(user => user.PhoneNumber!)
                .ToList();
            if (userPhones.Any(existing => PhoneNumbersMatch(existing, phone)))
                return true;

            var residentPhones = residents.GetAll().AsEnumerable()
                .Where(resident => resident.UserId != excludedUserId && resident.PhoneNumber != null)
                .Select(resident => resident.PhoneNumber!)
                .ToList();
            return residentPhones.Any(existing => PhoneNumbersMatch(existing, phone));
        }
    }
}