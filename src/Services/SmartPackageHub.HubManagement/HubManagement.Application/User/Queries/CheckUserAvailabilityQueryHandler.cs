using AutoMapper;
using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Application.HubUsers.Services;
using SmartPackageHub.Applications.Abstractions.Queries;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubUsers.Queries
{
    public sealed class CheckUserAvailabilityQueryHandler : QueryHandler<CheckUserAvailabilityQuery, UserAvailabilityQueryResult>
    {
        private readonly IRepository<User> _users;
        private readonly IRepository<Resident> _residents;

        public CheckUserAvailabilityQueryHandler(
            IMapper mapper,
            IRepository<User> users,
            IRepository<Resident> residents) : base(mapper)
        {
            _users = users;
            _residents = residents;
        }

        protected override Task<UserAvailabilityQueryResult> HandleAsync(CheckUserAvailabilityQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!string.Equals(request.CallerRole, "PlatformAdmin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.CallerRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new UserAvailabilityQueryResult(true, false, "Admin access required.", false));
            }

            if (string.IsNullOrWhiteSpace(request.Value))
                return Task.FromResult(new UserAvailabilityQueryResult(false, true, string.Empty, true));

            if (!string.Equals(request.Field, "username", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.Field, "email", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.Field, "phone", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new UserAvailabilityQueryResult(false, false, "Field must be username, email, or phone.", false));
            }

            var normalized = request.Value.Trim();
            var users = _users.GetAll().AsEnumerable().ToList();
            if (request.ExcludeUserId.HasValue)
                users = users.Where(user => user.Id != request.ExcludeUserId.Value).ToList();

            var fieldName = request.Field.ToUpperInvariant();
            var exists = fieldName switch
            {
                "USERNAME" => users.Any(user => user.Role.Type != RoleType.Resident &&
                    string.Equals(user.Username, normalized, StringComparison.OrdinalIgnoreCase)),
                "EMAIL" => users.Any(user => string.Equals(user.ContactInfo?.Email, normalized, StringComparison.OrdinalIgnoreCase)),
                _ => users.Any(user => UserPhoneMatcher.PhoneNumbersMatch(user.PhoneNumber, normalized))
            };

            if (string.Equals(fieldName, "PHONE", StringComparison.Ordinal))
            {
                var residentPhones = _residents.GetAll().AsEnumerable()
                    .Where(resident => resident.PhoneNumber != null &&
                        (!request.ExcludeUserId.HasValue || resident.UserId != request.ExcludeUserId.Value))
                    .Select(resident => resident.PhoneNumber!)
                    .ToList();
                exists |= residentPhones.Any(phone => UserPhoneMatcher.PhoneNumbersMatch(phone, normalized));
            }

            return Task.FromResult(new UserAvailabilityQueryResult(false, true, string.Empty, !exists));
        }
    }
}