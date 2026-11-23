using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Guards;

namespace HubManagement.Application.HubResidents.Services
{
    /// <summary>
    /// Shared create path for mobile self-registration and admin CSV import.
    /// Creates Resident-role User (no password) + Resident profile when <paramref name="userId"/> is empty.
    /// </summary>
    internal static class ResidentProvisioner
    {
        public static async Task<AddResidentResult> ProvisionAsync(
            IRepository<Resident> residents,
            IRepository<User> users,
            IUnitOfWork unitOfWork,
            Guid societyId,
            Guid userId,
            string fullName,
            string phoneNo,
            string towerWing,
            string flatNumber,
            bool isMember,
            Uri? picUrl,
            bool isActive,
            CancellationToken cancellationToken)
        {
            if (societyId == Guid.Empty)
                return new AddResidentResult(false, false, null, "Society is required.");

            if (!PhoneNumberGuard.TryNormalizeIndiaMobile(phoneNo, out var phone))
                return new AddResidentResult(false, false, null, "Enter a valid 10-digit mobile number.");

            var duplicate = residents.GetAll().AsEnumerable().Any(resident =>
                resident.SocietyId == societyId &&
                PhoneNumberGuard.TryNormalizeIndiaMobile(resident.PhoneNumber, out var stored) &&
                string.Equals(stored, phone, StringComparison.Ordinal));

            if (duplicate)
            {
                return new AddResidentResult(
                    false,
                    true,
                    null,
                    "A resident with this mobile number is already registered in this society.");
            }

            try
            {
                var resolvedUserId = userId;
                if (resolvedUserId == Guid.Empty)
                {
                    var email = $"{phone}@resident.smartpackagehub.local";
                    var emailTaken = users.GetAll().AsEnumerable().Any(user =>
                        string.Equals(user.ContactInfo?.Email, email, StringComparison.OrdinalIgnoreCase));
                    if (emailTaken)
                    {
                        return new AddResidentResult(
                            false,
                            true,
                            null,
                            "An account with this mobile number already exists.");
                    }

                    var user = User.Create(
                        $"resident-{phone}",
                        ContactInfo.Create(email),
                        Role.Create(RoleType.Resident),
                        "Active",
                        passwordHash: null,
                        phoneNumber: phone);
                    user.LinkToSociety(societyId);
                    users.Insert(user);
                    resolvedUserId = user.Id;
                }
                else
                {
                    var existingUser = await users.GetByIdAsync(resolvedUserId).ConfigureAwait(false);
                    if (existingUser is null)
                    {
                        return new AddResidentResult(
                            false,
                            false,
                            null,
                            "Society and user are required.");
                    }
                }

                var resident = Resident.Create(
                    societyId,
                    resolvedUserId,
                    fullName,
                    phone,
                    towerWing,
                    flatNumber,
                    isMember,
                    picUrl,
                    isActive);
                residents.Insert(resident);
                await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new AddResidentResult(true, false, resident.Id, "Resident registered.");
            }
            catch (ArgumentException ex)
            {
                return new AddResidentResult(false, false, null, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return new AddResidentResult(false, false, null, ex.Message);
            }
        }
    }
}
