using System.Globalization;
using System.Security.Cryptography;
using HubManagement.Application.PackageHub.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SmartPackageHub.Common.Services.Messaging.Models;
using StackExchange.Redis;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed class GeneratePackagePickupOtpCommandHandler
        : CreateCommandHandler<GeneratePackagePickupOtpCommand, GeneratePackagePickupOtpResult>
    {
        private static readonly TimeSpan PickupOtpLifetime = TimeSpan.FromDays(7);
        private static readonly Action<ILogger, Guid, Exception?> LogPickupOtpSmsQueueFailed =
            LoggerMessage.Define<Guid>(
                LogLevel.Error,
                new EventId(1, nameof(GeneratePackagePickupOtpCommandHandler)),
                "Pickup OTP was saved but its SMS could not be queued for package {PackageId}");
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<OtpLog> _otpLogs;
        private readonly IOtpSmsDispatcher _otpSmsDispatcher;
        private readonly ILogger<GeneratePackagePickupOtpCommandHandler> _logger;

        public GeneratePackagePickupOtpCommandHandler(
            IRepository<Package> packages,
            IRepository<Resident> residents,
            IRepository<OtpLog> otpLogs,
            IOtpSmsDispatcher otpSmsDispatcher,
            ILogger<GeneratePackagePickupOtpCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _residents = residents;
            _otpLogs = otpLogs;
            _otpSmsDispatcher = otpSmsDispatcher;
            _logger = logger;
        }

        public override async Task<GeneratePackagePickupOtpResult> Handle(
            GeneratePackagePickupOtpCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var package = await _packages.GetByIdAsync(request.PackageId).ConfigureAwait(false);
            if (package == null)
                return new GeneratePackagePickupOtpResult { Found = false, Message = "Package was not found." };

            if (package.Status != PackageStatus.Dispatched)
                return new GeneratePackagePickupOtpResult
                {
                    Found = true,
                    Conflict = true,
                    Message = "A pickup OTP can only be generated for a dispatched package.",
                };

            if (package.ResidentId is not Guid residentId || residentId == Guid.Empty)
                return new GeneratePackagePickupOtpResult
                {
                    Found = true,
                    Message = "Package has no resident assigned.",
                };

            var resident = await _residents.GetByIdAsync(residentId).ConfigureAwait(false);
            if (resident == null)
                return new GeneratePackagePickupOtpResult { Found = false, Message = "Resident was not found." };

            var priorOtps = await _otpLogs.ListAsync(
                log => log.PackageId == package.Id
                    && log.Purpose == "Pickup"
                    && !log.IsUsed,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            foreach (var priorOtp in priorOtps)
                priorOtp.MarkUsed();

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000)
                .ToString("D6", CultureInfo.InvariantCulture);
            _otpLogs.Insert(OtpLog.Generate(
                package.SocietyId, resident.Id, package.Id, code, PickupOtpLifetime, "Pickup"));
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);

            var phone = resident.PhoneNumber ?? string.Empty;
            if (string.IsNullOrWhiteSpace(phone))
            {
                return new GeneratePackagePickupOtpResult
                {
                    Found = true,
                    Succeeded = true,
                    PickupOtp = code,
                    Message = "OTP generated, but the resident has no phone number on file.",
                    SmsQueued = false,
                };
            }

            var smsBody =
                $"Smart Package Hub OTP {code} for AWB {package.CourierAwb}. Valid 7 days. Do not share.";
            OtpSmsDispatchResult dispatch;
            try
            {
                dispatch = await _otpSmsDispatcher.QueuePickupOtpAsync(
                    package.SocietyId,
                    resident.Id,
                    package.Id,
                    phone,
                    code,
                    "PickupOtp",
                    smsBody,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is DbUpdateException or RedisException or TimeoutException or InvalidOperationException or OperationCanceledException)
            {
                LogPickupOtpSmsQueueFailed(_logger, package.Id, exception);
                return new GeneratePackagePickupOtpResult
                {
                    Found = true,
                    Succeeded = true,
                    PickupOtp = code,
                    SmsQueued = false,
                    Message = "OTP was created, but SMS delivery could not be confirmed. Share the displayed code with the resident.",
                };
            }

            return new GeneratePackagePickupOtpResult
            {
                Found = true,
                Succeeded = true,
                PickupOtp = code,
                SmsQueued = dispatch.Queued,
                Message = dispatch.Queued
                    ? "New pickup OTP generated and queued for the resident."
                    : $"OTP generated, but the resident SMS was not queued: {dispatch.ErrorMessage}",
            };
        }
    }
}
