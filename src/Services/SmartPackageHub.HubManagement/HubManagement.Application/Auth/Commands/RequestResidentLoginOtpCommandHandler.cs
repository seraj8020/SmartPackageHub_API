using System.Globalization;
using System.Security.Cryptography;
using HubManagement.Application.Auth.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.ConfigOptions;
using SmartPackageHub.Common.Services.FeatureFlags.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using SmartPackageHub.Common.Services.Messaging.Encoding;
using SmartPackageHub.Common.Services.Messaging.Guards;
using SmartPackageHub.Common.Services.Messaging.Models;

namespace HubManagement.Application.Auth.Commands
{
    public sealed class RequestResidentLoginOtpCommandHandler
        : CreateCommandHandler<RequestResidentLoginOtpCommand, ResidentLoginOtpRequestResult>
    {
        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);

        private static readonly Action<ILogger, Guid, string, Exception?> LogSmsFailed =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Warning,
                new EventId(1, nameof(RequestResidentLoginOtpCommandHandler)),
                "Login OTP SMS failed for resident {ResidentId}: {Reason}");

        private static readonly Action<ILogger, Guid, Exception?> LogSmsSkipped =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(2, nameof(RequestResidentLoginOtpCommandHandler)),
                "Login OTP SMS skipped (FeatureFlags:SmsOtp=false) for resident {ResidentId}");

        private readonly IRepository<Resident> _residents;
        private readonly IMemoryCache _cache;
        private readonly ISmsSender _smsSender;
        private readonly IFeatureFlags _featureFlags;
        private readonly AuthOptions _authOptions;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<RequestResidentLoginOtpCommandHandler> _logger;

        public RequestResidentLoginOtpCommandHandler(
            IRepository<Resident> residents,
            IMemoryCache cache,
            ISmsSender smsSender,
            IFeatureFlags featureFlags,
            IOptions<AuthOptions> authOptions,
            IHostEnvironment environment,
            ILogger<RequestResidentLoginOtpCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _residents = residents;
            _cache = cache;
            _smsSender = smsSender;
            _featureFlags = featureFlags;
            ArgumentNullException.ThrowIfNull(authOptions);
            _authOptions = authOptions.Value;
            _environment = environment;
            _logger = logger;
        }

        public override async Task<ResidentLoginOtpRequestResult> Handle(
            RequestResidentLoginOtpCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.PhoneNumber, out var phone))
            {
                return new ResidentLoginOtpRequestResult(
                    false, false, null, null, "Enter a valid 10-digit mobile number.");
            }

            var resident = _residents.GetAll().AsEnumerable()
                .FirstOrDefault(item =>
                    item.IsActive &&
                    PhoneNumberGuard.TryNormalizeIndiaMobile(item.PhoneNumber, out var stored) &&
                    string.Equals(stored, phone, StringComparison.Ordinal));

            if (resident is null)
            {
                return new ResidentLoginOtpRequestResult(
                    false,
                    true,
                    null,
                    null,
                    "This mobile number is not registered. Please register first.");
            }

            var sendSms = _featureFlags.SmsOtp;
            var otp = ResolveOtp(sendSms);
            _cache.Set(CacheKey(resident.Id), otp, OtpLifetime);

            if (!sendSms)
            {
                LogSmsSkipped(_logger, resident.Id, null);
                return new ResidentLoginOtpRequestResult(
                    true,
                    false,
                    resident.Id,
                    otp,
                    $"Test OTP ready (SMS provider off). Use {otp}. Expires in 10 minutes.");
            }

            var body = SmsEncodingHelper.ToGsm7SafeOtpMessage(
                $"Smart Package Hub login OTP {otp}. Valid 10 min. Do not share.");

            var sms = await _smsSender.SendAsync(
                new SmsSendRequest(
                    phone,
                    body,
                    $"login-otp:{resident.Id:N}:{otp}"),
                cancellationToken).ConfigureAwait(false);

            if (!sms.Succeeded)
            {
                LogSmsFailed(_logger, resident.Id, sms.ErrorMessage ?? "unknown", null);
                if (!_environment.IsDevelopment())
                {
                    return new ResidentLoginOtpRequestResult(
                        false,
                        false,
                        resident.Id,
                        null,
                        sms.ErrorMessage ?? "We could not send an SMS code. Please try again.");
                }

                return new ResidentLoginOtpRequestResult(
                    true,
                    false,
                    resident.Id,
                    otp,
                    $"OTP generated but SMS failed: {sms.ErrorMessage}. Use the code shown in Development.");
            }

            return new ResidentLoginOtpRequestResult(
                true,
                false,
                resident.Id,
                _environment.IsDevelopment() ? otp : null,
                "SMS code sent. It expires in 10 minutes.");
        }

        private string ResolveOtp(bool sendSms)
        {
            if (!sendSms &&
                !string.IsNullOrWhiteSpace(_authOptions.FixedLoginOtp) &&
                _authOptions.FixedLoginOtp.Trim().Length == 6 &&
                _authOptions.FixedLoginOtp.Trim().All(char.IsDigit))
            {
                return _authOptions.FixedLoginOtp.Trim();
            }

            return RandomNumberGenerator.GetInt32(0, 1_000_000)
                .ToString("D6", CultureInfo.InvariantCulture);
        }

        internal static string CacheKey(Guid residentId) => $"resident-login-otp:{residentId:N}";
    }
}
