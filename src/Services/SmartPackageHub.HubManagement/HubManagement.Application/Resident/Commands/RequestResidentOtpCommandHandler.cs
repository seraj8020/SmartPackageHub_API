using HubManagement.Application.HubResidents.DTOs;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using System.Globalization;
using System.Security.Cryptography;

namespace HubManagement.Application.HubResidents.Commands
{
    /// <summary>
    /// Records success if SMS queued. Rate-limited (otp-sms).
    /// </summary>
    public sealed class RequestResidentOtpCommandHandler : CreateCommandHandler<RequestResidentOtpCommand, RequestOtpResponse>
    {
        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
        private static readonly Action<ILogger, Guid, Exception?> LogResidentPickUpOtp =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(RequestResidentOtpCommandHandler)),
                "Generated pickup OTP for resident {ResidentId}");
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<OtpLog> _otpLogs;
        private readonly IOtpSmsDispatcher _otpSmsDispatcher;
        private readonly ISmsPumpingGuard _smsPumpingGuard;
        private readonly ILogger<RequestResidentOtpCommandHandler> _logger;

        public RequestResidentOtpCommandHandler(
            IRepository<Resident> residents,
            IRepository<Package> packages,
            IRepository<OtpLog> otpLogs,
            IOtpSmsDispatcher otpSmsDispatcher,
            ISmsPumpingGuard smsPumpingGuard,
            ILogger<RequestResidentOtpCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _residents = residents;
            _packages = packages;
            _otpLogs = otpLogs;
            _otpSmsDispatcher = otpSmsDispatcher;
            _smsPumpingGuard = smsPumpingGuard;
            _logger = logger;
        }

        /// <summary>
        /// Find shelved package, generate OTP, save OtpLog, queue SMS via IOtpSmsDispatcher.
        /// </summary>
        public override async Task<RequestOtpResponse> Handle(RequestResidentOtpCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident == null)
                return new RequestOtpResponse(false, null, null, "Resident was not found.", SmsQueued: false);

            var phone = resident.PhoneNumber ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(phone))
            {
                var pumping = await _smsPumpingGuard.CheckAsync(
                    request.ClientIp,
                    phone,
                    request.RecaptchaToken,
                    cancellationToken).ConfigureAwait(false);
                if (!pumping.Allowed)
                    return new RequestOtpResponse(
                        true,
                        resident.Id,
                        null,
                        pumping.Reason ?? "OTP SMS request was blocked.",
                        SmsQueued: false);
            }

            var pendingPackage = await _packages.FirstOrDefaultAsync(p =>
                p.ResidentId == resident.Id
                && p.Status == PackageStatus.Shelved).ConfigureAwait(false);

            if (pendingPackage == null)
                return new RequestOtpResponse(true, resident.Id, null, "No packages are waiting for pickup.", SmsQueued: false);

            // Keep one active pickup OTP per package so deliver never scans a growing unused set.
            var priorOtps = await _otpLogs.ListAsync(
                log => log.PackageId == pendingPackage.Id
                    && string.Equals(log.Purpose, "Pickup", StringComparison.Ordinal)
                    && !log.IsUsed,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            foreach (var prior in priorOtps)
                prior.MarkUsed();

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
            var otp = OtpLog.Generate(resident.SocietyId, resident.Id, pendingPackage.Id, code, OtpLifetime, "Pickup");
            _otpLogs.Insert(otp);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogResidentPickUpOtp(_logger, resident.Id, null);

            var smsQueued = false;
            string message;
            if (!string.IsNullOrWhiteSpace(phone))
            {
                // GSM-7 safe OTP copy — avoid ?/emoji so we stay on a single 160-char segment.
                var smsBody =
                    $"Smart Package Hub OTP {code} for AWB {pendingPackage.CourierAwb}. Valid 10 min. Do not share.";
                var dispatch = await _otpSmsDispatcher.QueuePickupOtpAsync(
                    resident.SocietyId,
                    resident.Id,
                    pendingPackage.Id,
                    phone,
                    code,
                    "PickupOtp",
                    smsBody,
                    cancellationToken).ConfigureAwait(false);

                smsQueued = dispatch.Queued;
                message = dispatch.Queued
                    ? "OTP sent via SMS. Delivery confirmed by provider webhook."
                    : $"OTP generated but SMS was not queued: {dispatch.ErrorMessage}";

                if (smsQueued)
                {
                    await _smsPumpingGuard.RecordSuccessAsync(
                        request.ClientIp,
                        phone,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                message = "OTP generated but resident has no phone number on file.";
            }

            // Development convenience: still return code when SMS is not configured / demo.
            return new RequestOtpResponse(true, resident.Id, code, message, smsQueued);
        }
    }
}
