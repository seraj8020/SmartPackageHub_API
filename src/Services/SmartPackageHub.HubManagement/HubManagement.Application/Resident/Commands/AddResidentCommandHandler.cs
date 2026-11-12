using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Guards;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed class AddResidentCommandHandler
        : CreateCommandHandler<AddResidentCommand, AddResidentResult>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogCreatedResident =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(AddResidentCommandHandler)),
                "Created Resident: {ResidentId}");

        private readonly IRepository<Resident> _repository;
        private readonly IRepository<User> _users;
        private readonly ILogger<AddResidentCommandHandler> _logger;

        public AddResidentCommandHandler(
            IRepository<Resident> repository,
            IRepository<User> users,
            ILogger<AddResidentCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _repository = repository;
            _users = users;
            _logger = logger;
        }

        public override async Task<AddResidentResult> Handle(
            AddResidentCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.societyId == Guid.Empty)
                return new AddResidentResult(false, false, null, "Society is required.");

            if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.phoneNo, out var phone))
                return new AddResidentResult(false, false, null, "Enter a valid 10-digit mobile number.");

            var duplicate = _repository.GetAll().AsEnumerable().Any(resident =>
                resident.SocietyId == request.societyId &&
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
                var userId = request.userId;
                if (userId == Guid.Empty)
                {
                    // Self-registration path used by mobile web + Expo app.
                    var email = $"{phone}@resident.smartpackagehub.local";
                    var emailTaken = _users.GetAll().AsEnumerable().Any(user =>
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
                    user.LinkToSociety(request.societyId);
                    _users.Insert(user);
                    userId = user.Id;
                }
                else
                {
                    // Existing admin/staff path: keep requiring a real user id.
                    var existingUser = await _users.GetByIdAsync(userId).ConfigureAwait(false);
                    if (existingUser is null)
                    {
                        return new AddResidentResult(
                            false,
                            false,
                            null,
                            "Society and user are required.");
                    }
                }

                // Prefer the Uri overload (CA2234). Empty/null picUrl is fine.
                Uri? avatar = request.picUrl;
                var resident = Resident.Create(
                    request.societyId,
                    userId,
                    request.fullname,
                    phone,
                    request.towerWing,
                    request.flatnumber,
                    request.isMember,
                    avatar,
                    request.isActive);
                _repository.Insert(resident);
                await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
                LogCreatedResident(_logger, resident.Id, null);
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
