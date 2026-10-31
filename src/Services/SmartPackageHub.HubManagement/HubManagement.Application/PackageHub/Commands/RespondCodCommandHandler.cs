using SmartPackageHub.Common.Services.FeatureFlags.Abstractions;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using HubManagement.Application.PackageHub.DTOs;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed class RespondCodCommandHandler : CreateCommandHandler<RespondCodCommand, CodResponseResult>
    {
        private static readonly Action<ILogger, Guid, bool, Exception?> LogCodResponded =
            LoggerMessage.Define<Guid, bool>(
                LogLevel.Information,
                new EventId(1, nameof(RespondCodCommandHandler)),
                "COD response for package {PackageId}: approved={Approved}");

        private readonly IRepository<Package> _packages;
        private readonly IFeatureFlags _featureFlags;
        private readonly ILogger<RespondCodCommandHandler> _logger;

        public RespondCodCommandHandler(
            IRepository<Package> packages,
            IFeatureFlags featureFlags,
            ILogger<RespondCodCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _featureFlags = featureFlags;
            _logger = logger;
        }

        public override async Task<CodResponseResult> Handle(RespondCodCommand command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            if (!_featureFlags.CashOnDelivery)
            {
                return new CodResponseResult
                {
                    Found = true,
                    Succeeded = false,
                    Message = "Cash on delivery is disabled for this hub.",
                };
            }

            var package = await _packages.GetByIdAsync(command.PackageId).ConfigureAwait(false);
            if (package == null)
            {
                return new CodResponseResult { Found = false, Message = "Package was not found." };
            }

            if (!package.IsCod)
            {
                return new CodResponseResult
                {
                    Found = true,
                    Succeeded = false,
                    Message = "This package is not marked as cash on delivery.",
                };
            }

            if (package.CodApprovalStatus != CodApprovalStatus.Pending)
            {
                return new CodResponseResult
                {
                    Found = true,
                    Conflict = true,
                    Succeeded = false,
                    Message = $"COD is already {package.CodApprovalStatus}.",
                    Status = package.Status.ToString(),
                    CodApprovalStatus = package.CodApprovalStatus.ToString(),
                };
            }

            if (!command.Approve)
            {
                package.RejectCod();
                await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
                LogCodResponded(_logger, package.Id, false, null);
                return new CodResponseResult
                {
                    Found = true,
                    Succeeded = true,
                    Message = "COD amount rejected. Package stays received.",
                    Status = package.Status.ToString(),
                    CodApprovalStatus = package.CodApprovalStatus.ToString(),
                };
            }

            // Approval only — staff still assigns shelf later; OTP is generated at shelving.
            package.ApproveCod();
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogCodResponded(_logger, package.Id, true, null);

            return new CodResponseResult
            {
                Found = true,
                Succeeded = true,
                Message = "COD approved. Staff can now assign a shelf.",
                Status = package.Status.ToString(),
                CodApprovalStatus = package.CodApprovalStatus.ToString(),
            };
        }
    }
}
