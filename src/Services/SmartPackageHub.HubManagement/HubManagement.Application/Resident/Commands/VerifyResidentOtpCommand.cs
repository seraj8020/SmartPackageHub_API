using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed record VerifyResidentOtpCommand(Guid ResidentId, string Code) : IUpdateCommand<VerifyOtpResponse>;

    public sealed class VerifyResidentOtpCommandHandler : UpdateCommandHandler<VerifyResidentOtpCommand, VerifyOtpResponse>
    {
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<OTPLog> _otpLogs;
        private readonly IRepository<Delivery> _deliveries;
        private readonly ILogger<VerifyResidentOtpCommandHandler> _logger;

        public VerifyResidentOtpCommandHandler(
            IRepository<Resident> residents,
            IRepository<Package> packages,
            IRepository<OTPLog> otpLogs,
            IRepository<Delivery> deliveries,
            ILogger<VerifyResidentOtpCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _residents = residents;
            _packages = packages;
            _otpLogs = otpLogs;
            _deliveries = deliveries;
            _logger = logger;
        }

        public override async Task<VerifyOtpResponse> Handle(VerifyResidentOtpCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return new VerifyOtpResponse(true, false, "OTP code is required.");

            var resident = await _residents.GetByIdAsync(request.ResidentId);
            if (resident == null)
                return new VerifyOtpResponse(false, false, "Resident was not found.");

            var logs = _otpLogs
                .FetchMulti(o => o.ResidentId == resident.Id && !o.IsUsed && o.Purpose == "Pickup")
                .ToList();

            OTPLog? matched = null;
            foreach (var log in logs)
            {
                if (log.ValidateCollection(request.Code.Trim()))
                {
                    matched = log;
                    break;
                }
            }

            if (matched == null)
                return new VerifyOtpResponse(true, false, "Invalid or expired OTP.");

            var pending = _packages
                .FetchMulti(p => p.ResidentId == resident.Id && p.Status != PackageStatus.Delivered)
                .ToList();

            var deliveredAt = DateTime.UtcNow;
            foreach (var package in pending)
            {
                package.MarkCollected();
                var delivery = Delivery.Create(
                    resident.SocietyId,
                    package.Id,
                    resident.UserId,
                    resident.Id,
                    matched.Id,
                    null,
                    "CounterPickup",
                    deliveredAt,
                    "Picked up via OTP");
                delivery.CompleteDelivery();
                _deliveries.Insert(delivery);
            }

            await UnitOfWork!.CommitAsync(cancellationToken);
            _logger.LogInformation("Verified pickup OTP for resident {ResidentId}. Packages collected: {Count}", resident.Id, pending.Count);
            return new VerifyOtpResponse(true, true, "OTP verified. Packages marked picked up.");
        }
    }
}
