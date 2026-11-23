using HubManagement.Application.Courier.Services;
using HubManagement.Application.Couriers.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Courier.Commands
{
    public sealed class LiftCourierBlacklistCommandHandler
        : UpdateCommandHandler<LiftCourierBlacklistCommand, CourierCommandResult>
    {
        private readonly IRepository<CourierBlacklist> _blacklists;
        private readonly IRepository<CourierAgent> _agents;

        public LiftCourierBlacklistCommandHandler(
            IRepository<CourierBlacklist> blacklists,
            IRepository<CourierAgent> agents,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _blacklists = blacklists;
            _agents = agents;
        }

        public override async Task<CourierCommandResult> Handle(
            LiftCourierBlacklistCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var entry = await _blacklists.GetByIdAsync(request.BlacklistId).ConfigureAwait(false);
            if (entry is null)
                return CourierCommandResult.NotFound("Blacklist entry was not found.");
            if (!CourierAdminAccess.CanAccessSociety(request.Role, request.SocietyIds, entry.SocietyId))
                return CourierCommandResult.FailForbidden("You cannot manage couriers outside your society.");

            entry.Deactivate();

            var agent = await _agents.FirstOrDefaultAsync(
                item => item.SocietyId == entry.SocietyId && item.Phone == entry.Phone)
                .ConfigureAwait(false);
            agent?.Activate();

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            return CourierCommandResult.OkMessage("Blacklist lifted.");
        }
    }
}