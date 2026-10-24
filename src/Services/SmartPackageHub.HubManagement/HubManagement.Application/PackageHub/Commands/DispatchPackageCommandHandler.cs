using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.PackageHub.Commands
{
    public sealed class DispatchPackageCommandHandler : CreateCommandHandler<DispatchPackageCommand, bool>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogPackageDispatched =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(DispatchPackageCommandHandler)),
                "Dispatched package {PackageId}");
        private readonly IRepository<Package> _packages;
        private readonly ILogger<DispatchPackageCommandHandler> _logger;

        public DispatchPackageCommandHandler(
            IRepository<Package> packages,
            ILogger<DispatchPackageCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _packages = packages;
            _logger = logger;
        }

        public override async Task<bool> Handle(DispatchPackageCommand command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            var packageTask = _packages.GetByIdAsync(command.packageId);
            // 2. Await the task later using ConfigureAwait(false) to get the result
            var package = await packageTask.ConfigureAwait(false);
            if (package == null)
                return false;
            package.DispatchPackage();
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogPackageDispatched(_logger, package.Id, null);
            return true;
        }
    }
}
