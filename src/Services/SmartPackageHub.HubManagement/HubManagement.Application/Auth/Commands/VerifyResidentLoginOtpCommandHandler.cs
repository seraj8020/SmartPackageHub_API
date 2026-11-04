using System.Security.Cryptography;
using System.Text;
using HubManagement.Application.Auth.DTOs;
using HubManagement.Application.Auth.Services;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Caching.Memory;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Guards;

namespace HubManagement.Application.Auth.Commands
{
    public sealed class VerifyResidentLoginOtpCommandHandler
        : CreateCommandHandler<VerifyResidentLoginOtpCommand, ResidentLoginResult>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IMemoryCache _cache;

        public VerifyResidentLoginOtpCommandHandler(
            IRepository<Resident> residents,
            IMemoryCache cache,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _residents = residents;
            _cache = cache;
        }

        public override Task<ResidentLoginResult> Handle(
            VerifyResidentLoginOtpCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.PhoneNumber, out var phone))
            {
                return Task.FromResult(Fail(false, "Enter a valid 10-digit mobile number."));
            }

            if (string.IsNullOrWhiteSpace(request.Otp) || request.Otp.Trim().Length != 6)
            {
                return Task.FromResult(Fail(false, "Enter the complete 6-digit code."));
            }

            var resident = _residents.GetAll().AsEnumerable()
                .FirstOrDefault(item =>
                    item.IsActive &&
                    PhoneNumberGuard.TryNormalizeIndiaMobile(item.PhoneNumber, out var stored) &&
                    string.Equals(stored, phone, StringComparison.Ordinal));

            if (resident is null)
            {
                return Task.FromResult(Fail(true, "This mobile number is not registered. Please register first."));
            }

            var cacheKey = RequestResidentLoginOtpCommandHandler.CacheKey(resident.Id);
            if (!_cache.TryGetValue<string>(cacheKey, out var expectedOtp) ||
                string.IsNullOrEmpty(expectedOtp) ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedOtp),
                    Encoding.UTF8.GetBytes(request.Otp.Trim())))
            {
                return Task.FromResult(Fail(
                    false,
                    "That code is not correct or has expired. Please request a new one."));
            }

            _cache.Remove(cacheKey);
            return Task.FromResult(new ResidentLoginResult(
                true,
                false,
                AuthTokenFactory.CreateOpaqueToken(),
                resident.Id,
                resident.SocietyId,
                resident.FullName,
                phone,
                resident.TowerWing,
                resident.Unit?.Value,
                "Resident",
                "Signed in."));
        }

        private static ResidentLoginResult Fail(bool notFound, string message) =>
            new(false, notFound, null, null, null, null, null, null, null, "Resident", message);
    }
}
