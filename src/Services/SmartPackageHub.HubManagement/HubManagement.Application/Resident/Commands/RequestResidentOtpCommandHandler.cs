using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using System.Globalization;
using System.Security.Cryptography;

namespace HubManagement.Application.HubResidents.Commands
{
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
        private readonly ILogger<RequestResidentOtpCommandHandler> _logger;

        public RequestResidentOtpCommandHandler(
            IRepository<Resident> residents,
            IRepository<Package> packages,
            IRepository<OtpLog> otpLogs,
            ILogger<RequestResidentOtpCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _residents = residents;
            _packages = packages;
            _otpLogs = otpLogs;
            _logger = logger;
        }

        public override async Task<RequestOtpResponse> Handle(RequestResidentOtpCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var resident = await _residents.GetByIdAsync(request.ResidentId).ConfigureAwait(false);
            if (resident == null)
                return new RequestOtpResponse(false, null, null, "Resident was not found.");

            var pendingPackage = _packages
                .FetchMulti(p => p.ResidentId == resident.Id && p.Status != PackageStatus.Delivered)
                .Select(p => p.Id)
                .FirstOrDefault();

            if (pendingPackage == Guid.Empty)
                return new RequestOtpResponse(true, resident.Id, null, "No packages are waiting for pickup.");

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
            var otp = OtpLog.Generate(resident.SocietyId, resident.Id, pendingPackage, code, OtpLifetime, "Pickup");
            _otpLogs.Insert(otp);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogResidentPickUpOtp(_logger, resident.Id, null);
            return new RequestOtpResponse(true, resident.Id, code, "OTP generated (returning in response for demo).");
        }
    }
}
