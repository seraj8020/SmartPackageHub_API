using System.Text.Json;
using HubManagement.Application.Support.DTOs;
using HubManagement.Application.Support.Queries;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.ConfigOptions;
using SmartPackageHub.Common.Services.Messaging.Guards;

namespace HubManagement.Application.Support.Commands
{
    public sealed class CreateSupportRequestCommandHandler
        : CreateCommandHandler<CreateSupportRequestCommand, SupportCommandResult>
    {
        private static readonly Action<ILogger, Guid, Guid, Exception?> LogSupportRequested =
            LoggerMessage.Define<Guid, Guid>(
                LogLevel.Information,
                new EventId(1, nameof(CreateSupportRequestCommandHandler)),
                "Support request {RequestId} created for resident {ResidentId}");

        private readonly IRepository<SupportRequest> _requests;
        private readonly IRepository<Resident> _residents;
        private readonly IRepository<Society> _societies;
        private readonly IRepository<AuditLog> _auditLogs;
        private readonly ResidentSupportOptions _options;
        private readonly ILogger<CreateSupportRequestCommandHandler> _logger;

        public CreateSupportRequestCommandHandler(
            IRepository<SupportRequest> requests,
            IRepository<Resident> residents,
            IRepository<Society> societies,
            IRepository<AuditLog> auditLogs,
            IOptions<ResidentSupportOptions> options,
            ILogger<CreateSupportRequestCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            ArgumentNullException.ThrowIfNull(options);
            _requests = requests;
            _residents = residents;
            _societies = societies;
            _auditLogs = auditLogs;
            _options = options.Value;
            _logger = logger;
        }

        public override async Task<SupportCommandResult> Handle(
            CreateSupportRequestCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var payload = request.Request ?? throw new ArgumentNullException(nameof(request));

            var resident = await ResolveResidentAsync(payload).ConfigureAwait(false);
            if (resident is null)
                return SupportCommandResult.NotFound("Resident was not found.");

            var society = await _societies.GetByIdAsync(resident.SocietyId).ConfigureAwait(false);
            if (society is null)
                return SupportCommandResult.NotFound("Society was not found.");

            var supportEmail = string.IsNullOrWhiteSpace(_options.SupportEmail)
                ? "hello@byonverse.com"
                : _options.SupportEmail.Trim();

            SupportRequest row;
            try
            {
                row = SupportRequest.Create(
                    resident.SocietyId,
                    resident.Id,
                    payload.Subject,
                    payload.ResolvedCategory,
                    payload.Urgency,
                    payload.Message,
                    supportEmail,
                    payload.ResidentName ?? resident.FullName,
                    payload.ResidentPhone ?? resident.PhoneNumber,
                    payload.TowerWing ?? resident.TowerWing,
                    payload.FlatNumber ?? resident.Unit?.Value);
            }
            catch (ArgumentException ex)
            {
                return SupportCommandResult.Fail(ex.Message);
            }

            _requests.Insert(row);
            _auditLogs.Insert(AuditLog.Create(
                resident.SocietyId,
                resident.UserId,
                nameof(SupportRequest),
                row.Id,
                "SupportRequested",
                DateTime.UtcNow,
                JsonSerializer.Serialize(new
                {
                    reference = row.ReferenceCode,
                    subject = row.Subject,
                    category = row.Topic,
                    urgency = row.Urgency,
                    supportEmail = row.SupportEmail,
                    society = society.Name,
                })));

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogSupportRequested(_logger, row.Id, row.ResidentId, null);

            return SupportCommandResult.Ok(
                GetResidentSupportRequestsQueryHandler.Map(row),
                "Support request received. Our team will follow up shortly.");
        }

        private async Task<Resident?> ResolveResidentAsync(CreateSupportRequestDto payload)
        {
            if (payload.ResidentId != Guid.Empty)
            {
                var byId = await _residents.GetByIdAsync(payload.ResidentId).ConfigureAwait(false);
                if (byId is not null)
                    return byId;
            }

            if (!PhoneNumberGuard.TryNormalizeIndiaMobile(payload.ResidentPhone, out var phone))
                return null;

            return _residents.GetAll().AsEnumerable()
                .FirstOrDefault(item =>
                    item.IsActive &&
                    PhoneNumberGuard.TryNormalizeIndiaMobile(item.PhoneNumber, out var stored) &&
                    string.Equals(stored, phone, StringComparison.Ordinal));
        }
    }
}
