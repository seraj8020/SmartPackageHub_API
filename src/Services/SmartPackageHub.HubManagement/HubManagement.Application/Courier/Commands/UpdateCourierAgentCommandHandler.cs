using HubManagement.Application.Courier.Services;
using HubManagement.Application.Couriers.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Guards;

namespace HubManagement.Application.Courier.Commands
{
    public sealed class UpdateCourierAgentCommandHandler
        : UpdateCommandHandler<UpdateCourierAgentCommand, CourierCommandResult>
    {
        private readonly IRepository<CourierAgent> _agents;
        private readonly IRepository<CourierBlacklist> _blacklists;
        private readonly IRepository<Society> _societies;

        public UpdateCourierAgentCommandHandler(
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
            UpdateCourierAgentCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var body = request.Body;

            var agent = await _agents.GetByIdAsync(request.AgentId).ConfigureAwait(false);
            if (agent is null)
                return CourierCommandResult.NotFound("Courier was not found.");
            if (!CourierAdminAccess.CanAccessSociety(request.Role, request.SocietyIds, agent.SocietyId))
                return CourierCommandResult.FailForbidden("You cannot manage couriers outside your society.");

            if (string.IsNullOrWhiteSpace(body.CarrierName))
                return CourierCommandResult.Fail("Courier name is required.");

            string? phone = null;
            if (!string.IsNullOrWhiteSpace(body.Phone))
            {
                if (!PhoneNumberGuard.TryNormalizeIndiaMobile(body.Phone, out phone))
                    return CourierCommandResult.Fail("Enter a valid 10-digit Indian mobile number.");

                var phoneTaken = _agents
                    .FetchMulti(
                        item => item.SocietyId == agent.SocietyId && item.Id != request.AgentId && item.Phone == phone,
                        noTracking: true)
                    .Any();
                if (phoneTaken)
                {
                    return CourierCommandResult.Fail(
                        "Another courier already uses this mobile number in the society.",
                        conflict: true);
                }
            }

            try
            {
                agent.UpdateDetails(
                    body.CarrierName,
                    body.AgentName,
                    phone,
                    body.VehicleNumber,
                    body.EmployeeOrBadgeNumber);
                if (body.IsActive == true) agent.Activate();
                if (body.IsActive == false) agent.Deactivate();
                await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);

                var society = await _societies.GetByIdAsync(agent.SocietyId).ConfigureAwait(false);
                var societyName = society?.Name ?? string.Empty;
                var hit = string.IsNullOrWhiteSpace(agent.Phone)
                    ? null
                    : await _blacklists.FirstOrDefaultAsync(
                        item => item.IsActive && item.SocietyId == agent.SocietyId && item.Phone == agent.Phone)
                        .ConfigureAwait(false);
                return CourierCommandResult.OkAgent(CourierDtoMapper.ToAgentDto(agent, societyName, hit));
            }
            catch (ArgumentException ex)
            {
                return CourierCommandResult.Fail(ex.Message);
            }
        }
    }
}