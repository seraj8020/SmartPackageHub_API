using System.Text.Json;
using HubManagement.Application.Support.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Common.ConfigOptions;

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

            var resident = await _residents.GetByIdAsync(payload.ResidentId).ConfigureAwait(false);
            if (resident is null)
                return SupportCommandResult.NotFound("Resident was not found.");

            if (payload.SocietyId == Guid.Empty || payload.SocietyId != resident.SocietyId)
                return SupportCommandResult.Fail("Society does not match this resident.");

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
                    payload.Topic,
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
                    topic = row.Topic,
                    supportEmail = row.SupportEmail,
                    society = society.Name,
                })));

            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogSupportRequested(_logger, row.Id, row.ResidentId, null);

            return SupportCommandResult.Ok(
                new SupportRequestDto
                {
                    Id = row.Id,
                    SocietyId = row.SocietyId,
                    ResidentId = row.ResidentId,
                    Topic = row.Topic,
                    Message = row.Message,
                    Status = row.Status,
                    SupportEmail = row.SupportEmail,
                    RequestedAtUtc = row.RequestedAtUtc,
                },
                "Support request received. Our team will follow up shortly.");
        }
    }
}
