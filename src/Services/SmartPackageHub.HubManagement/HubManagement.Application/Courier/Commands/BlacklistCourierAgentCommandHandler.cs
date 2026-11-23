using HubManagement.Application.Courier.Services;
using HubManagement.Application.Couriers.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.Courier.Commands
{
    public sealed class BlacklistCourierAgentCommandHandler
        : UpdateCommandHandler<BlacklistCourierAgentCommand, CourierCommandResult>
    {
        private readonly IRepository<CourierAgent> _agents;
        private readonly IRepository<CourierBlacklist> _blacklists;
        private readonly IRepository<Society> _societies;

        public BlacklistCourierAgentCommandHandler(
            IRepository<CourierAgent> agents,
            IRepository<CourierBlacklist> blacklists,
            IRepository<Society> societies,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _agents = agents;
            _blacklists = blacklists;
            _societies = societies;
        }

        public override async Task<CourierCommandResult> Handle(
            BlacklistCourierAgentCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var body = request.Body;

            var agent = await _agents.GetByIdAsync(request.AgentId).ConfigureAwait(false);
            if (agent is null)
                return CourierCommandResult.NotFound("Courier was not found.");
            if (!CourierAdminAccess.CanAccessSociety(request.Role, request.SocietyIds, agent.SocietyId))
                return CourierCommandResult.FailForbidden("You cannot manage couriers outside your society.");

            if (string.IsNullOrWhiteSpace(agent.Phone))
                return CourierCommandResult.Fail("Add a mobile number before blacklisting this courier.");

            var reason = string.IsNullOrWhiteSpace(body.Reason)
                ? "Blacklisted by admin"
                : body.Reason.Trim();

            var existing = await _blacklists.FirstOrDefaultAsync(
                item => item.IsActive && item.SocietyId == agent.SocietyId && item.Phone == agent.Phone)
                .ConfigureAwait(false);
            if (existing is not null)
                return CourierCommandResult.Fail("This courier is already blacklisted.", conflict: true);

            var entry = CourierBlacklist.Create(
                agent.SocietyId,
                agent.CarrierName,
                agent.AgentName,
                agent.Phone,
                reason);
            agent.Deactivate();
            _blacklists.Insert(entry);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);

            var society = await _societies.GetByIdAsync(agent.SocietyId).ConfigureAwait(false);
            var societyName = society?.Name ?? string.Empty;
            return CourierCommandResult.OkBlacklist(CourierDtoMapper.ToBlacklistDto(entry, societyName));
        }
    }
}