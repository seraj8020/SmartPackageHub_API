using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Application.HubUsers.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Guards;

namespace HubManagement.Application.HubUsers.Commands
{
    public sealed class UpdateResidentUserCommandHandler : UpdateCommandHandler<UpdateResidentUserCommand, UserCommandResult>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogResidentUserUpdated =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(UpdateResidentUserCommandHandler)),
                "Updated resident user {UserId}");

        private readonly IRepository<User> _users;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Society> _societies;
        private readonly IRepository<UserSociety> _userSocieties;
        private readonly ILogger<UpdateResidentUserCommandHandler> _logger;

        public UpdateResidentUserCommandHandler(
            IRepository<User> users,
            IRepository<Resident> residents,
            IRepository<Society> societies,
            IRepository<UserSociety> userSocieties,
            ILogger<UpdateResidentUserCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _residents = residents;
            _societies = societies;
            _userSocieties = userSocieties;
            _logger = logger;
        }

        public override async Task<UserCommandResult> Handle(UpdateResidentUserCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!string.Equals(request.CallerRole, "PlatformAdmin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.CallerRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return new UserCommandResult(true, false, false, true, "Admin access required.", null);
            }

            if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.PhoneNumber, out var phone))
            {
                return new UserCommandResult(true, false, false, false, "Enter a valid 10-digit Indian mobile number.", null);
            }

            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length > 100 ||
                string.IsNullOrWhiteSpace(request.TowerWing) || request.TowerWing.Trim().Length > 50 ||
                string.IsNullOrWhiteSpace(request.UnitNumber) ||
                (!string.Equals(request.Status, "Active", StringComparison.Ordinal) &&
                 !string.Equals(request.Status, "Not Active", StringComparison.Ordinal)))
            {
                return new UserCommandResult(true, false, false, false,
                    "Username, phone number, tower or wing, flat, and a valid status are required.", null);
            }

            try
            {
                _ = UnitNumber.Create(request.UnitNumber);
            }
            catch (ArgumentException)
            {
                return new UserCommandResult(true, false, false, false, "Enter a valid flat number.", null);
            }

            var resident = await _residents.FirstOrDefaultAsync(item => item.UserId == request.UserId).ConfigureAwait(false);
            if (resident is null)
                return new UserCommandResult(false, false, false, false, "User was not found.", null);

            var user = await _users.GetByIdAsync(request.UserId).ConfigureAwait(false);
            if (user is null)
                return new UserCommandResult(false, false, false, false, "User was not found.", null);

            var platformAdmin = string.Equals(request.CallerRole, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);
            if (!platformAdmin && !request.CallerSocietyIds.Contains(resident.SocietyId))
            {
                return new UserCommandResult(true, false, false, true,
                    "Society admins can edit residents in their society only.", null);
            }

            var targetSocietyId = platformAdmin ? request.SocietyId : resident.SocietyId;
            if (targetSocietyId is null || targetSocietyId == Guid.Empty ||
                await _societies.GetByIdAsync(targetSocietyId.Value).ConfigureAwait(false) is null)
            {
                return new UserCommandResult(true, false, false, false, "Select a valid society.", null);
            }

            if (UserPhoneMatcher.PhoneExists(_users, _residents, request.UserId, phone))
            {
                return new UserCommandResult(true, true, false, false, "Phone already exists.", null, "Phone");
            }

            var username = request.Username.Trim();
            var isActive = string.Equals(request.Status, "Active", StringComparison.Ordinal);
            resident.UpdateAdminManagedDetails(username, phone, request.TowerWing, request.UnitNumber, isActive, targetSocietyId.Value);
            user.ChangeUsername(username);
            user.SetPhoneNumber(phone);
            user.ChangeStatus(request.Status);

            if (platformAdmin)
                SyncResidentSocietyLink(request.UserId, targetSocietyId.Value);

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogResidentUserUpdated(_logger, request.UserId, null);

            var societyName = _societies.GetAll().AsEnumerable()
                .Where(society => society.Id == targetSocietyId.Value)
                .Select(society => society.Name)
                .FirstOrDefault() ?? string.Empty;

            return new UserCommandResult(true, false, true, false, "Resident updated.", new UserDetailsDto
            {
                Id = resident.UserId,
                Username = user.Username,
                Email = user.ContactInfo?.Email ?? string.Empty,
                PhoneNumber = resident.PhoneNumber,
                Role = "Resident",
                Status = resident.IsActive ? "Active" : "Not Active",
                SocietyIds = new[] { resident.SocietyId },
                SocietyNames = new[] { societyName },
                TowerWing = resident.TowerWing,
                UnitNumber = resident.Unit?.Value ?? string.Empty,
                IsMember = resident.IsMember
            });
        }

        private void SyncResidentSocietyLink(Guid userId, Guid societyId)
        {
            var currentLinks = _userSocieties.GetAll().AsEnumerable().Where(link => link.UserId == userId).ToList();
            if (currentLinks.Count > 0)
                _userSocieties.Remove(currentLinks);
            _userSocieties.Insert(UserSociety.Create(userId, societyId));
        }
    }
}