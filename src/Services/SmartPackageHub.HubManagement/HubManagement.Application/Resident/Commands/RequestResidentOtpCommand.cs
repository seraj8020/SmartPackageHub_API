using System.Security.Cryptography;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed record RequestResidentOtpCommand(Guid ResidentId) : ICreateCommand<RequestOtpResponse>;

    public sealed class RequestResidentOtpCommandHandler : CreateCommandHandler<RequestResidentOtpCommand, RequestOtpResponse>
    {
        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);

        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<OTPLog> _otpLogs;
        private readonly ILogger<RequestResidentOtpCommandHandler> _logger;

        public RequestResidentOtpCommandHandler(
            IRepository<Resident> residents,
            IRepository<Package> packages,
            IRepository<OTPLog> otpLogs,
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
            var resident = await _residents.GetByIdAsync(request.ResidentId);
            if (resident == null)
                return new RequestOtpResponse(false, null, null, "Resident was not found.");

            var pendingPackage = _packages
                .FetchMulti(p => p.ResidentId == resident.Id && p.Status != PackageStatus.Delivered)
                .Select(p => p.Id)
                .FirstOrDefault();

            if (pendingPackage == Guid.Empty)
                return new RequestOtpResponse(true, resident.Id, null, "No packages are waiting for pickup.");

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var otp = OTPLog.Generate(resident.SocietyId, resident.Id, pendingPackage, code, OtpLifetime, "Pickup");
            _otpLogs.Insert(otp);
            await UnitOfWork!.CommitAsync(cancellationToken);

            _logger.LogInformation("Generated pickup OTP for resident {ResidentId}", resident.Id);
            return new RequestOtpResponse(true, resident.Id, code, "OTP generated (returning in response for demo).");
        }
    }
}
