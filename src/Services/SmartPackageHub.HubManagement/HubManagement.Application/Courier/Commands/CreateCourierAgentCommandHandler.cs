using HubManagement.Application.Courier.Services;
using HubManagement.Application.Couriers.DTOs;
using HubManagement.Core.Hubs.Entities;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.Services.Messaging.Guards;

namespace HubManagement.Application.Courier.Commands
{
    public sealed class CreateCourierAgentCommandHandler
        : CreateCommandHandler<CreateCourierAgentCommand, CourierCommandResult>
    {
        private readonly IRepository<CourierAgent> _agents;
        private readonly IRepository<CourierBlacklist> _blacklists;
        private readonly IRepository<Society> _societies;

        public CreateCourierAgentCommandHandler(
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
            CreateCourierAgentCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var body = request.Body;

            var (societyId, forbidden, badRequest, societyMessage) = CourierAdminAccess.TryResolveSocietyWrite(
                request.Role,
                request.SocietyIds,
                body.SocietyId);
            if (forbidden)
                return CourierCommandResult.FailForbidden(societyMessage!);
            if (badRequest)
                return CourierCommandResult.Fail(societyMessage!);

            var validationError = await ValidateCreateAsync(body, societyId, cancellationToken).ConfigureAwait(false);
            if (validationError is not null)
                return validationError;

            string? phone = null;
            if (!string.IsNullOrWhiteSpace(body.Phone) &&
                !PhoneNumberGuard.TryNormalizeIndiaMobile(body.Phone, out phone))
            {
                return CourierCommandResult.Fail("Enter a valid 10-digit Indian mobile number.");
            }

            try
            {
                var agent = CourierAgent.Create(
                    societyId,
                    body.CarrierName!,
                    body.AgentName,
                    phone,
                    body.VehicleNumber,
                    body.EmployeeOrBadgeNumber);
                _agents.Insert(agent);
                await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);

                var society = await _societies.GetByIdAsync(societyId).ConfigureAwait(false);
                var societyName = society?.Name ?? string.Empty;
                return CourierCommandResult.OkAgent(CourierDtoMapper.ToAgentDto(agent, societyName, null));
            }
            catch (ArgumentException ex)
            {
                return CourierCommandResult.Fail(ex.Message);
            }
        }

        private async Task<CourierCommandResult?> ValidateCreateAsync(
            CreateCourierAgentRequest request,
            Guid societyId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.CarrierName))
                return CourierCommandResult.Fail("Courier name is required.");

            if (await _societies.GetByIdAsync(societyId).ConfigureAwait(false) is null)
                return CourierCommandResult.Fail("Selected society was not found.");

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                if (!PhoneNumberGuard.TryNormalizeIndiaMobile(request.Phone, out var phone))
                    return CourierCommandResult.Fail("Enter a valid 10-digit Indian mobile number.");

                var societyAgents = await _agents
                    .ListAsync(agent => agent.SocietyId == societyId, noTracking: true, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                if (societyAgents.Any(agent => string.Equals(agent.Phone, phone, StringComparison.Ordinal)))
                    return CourierCommandResult.Fail(
                        "A courier with this mobile number already exists in the society.",
                        conflict: true);

                var blacklisted = _blacklists
                    .FetchMulti(
                        item => item.IsActive && item.SocietyId == societyId && item.Phone == phone,
                        noTracking: true)
                    .Any();
                if (blacklisted)
                    return CourierCommandResult.Fail(
                        "This mobile number is blacklisted for the society.",
                        conflict: true);

                return null;
            }

            var carrierName = request.CarrierName.Trim();
            var existingNames = await _agents
                .ListAsync(agent => agent.SocietyId == societyId, noTracking: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (existingNames.Any(agent =>
                    string.Equals(agent.CarrierName, carrierName, StringComparison.OrdinalIgnoreCase)))
            {
                return CourierCommandResult.Fail(
                    "This courier name already exists in the society.",
                    conflict: true);
            }

            return null;
        }
    }
}