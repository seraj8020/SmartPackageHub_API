using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Application.HubResidents.Services;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed class ImportResidentsFromCsvCommandHandler
        : CreateCommandHandler<ImportResidentsFromCsvCommand, ImportResidentsResultDto>
    {
        private static readonly Action<ILogger, Guid, int, Exception?> LogImported =
            LoggerMessage.Define<Guid, int>(
                LogLevel.Information,
                new EventId(1, nameof(ImportResidentsFromCsvCommandHandler)),
                "Imported {Count} residents for society {SocietyId}");

        private readonly IRepository<Resident> _residents;
        private readonly IRepository<User> _users;
        private readonly IRepository<Society> _societies;
        private readonly ILogger<ImportResidentsFromCsvCommandHandler> _logger;

        public ImportResidentsFromCsvCommandHandler(
            IRepository<Resident> residents,
            IRepository<User> users,
            IRepository<Society> societies,
            ILogger<ImportResidentsFromCsvCommandHandler> logger,
            IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _residents = residents;
            _users = users;
            _societies = societies;
            _logger = logger;
        }

        public override async Task<ImportResidentsResultDto> Handle(
            ImportResidentsFromCsvCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!TryResolveSociety(request, out var societyId, out var accessError))
                return accessError!;

            if (await _societies.GetByIdAsync(societyId).ConfigureAwait(false) is null)
            {
                return new ImportResidentsResultDto
                {
                    Failed = 1,
                    Message = "Selected society was not found.",
                    Rows =
                    [
                        new ImportResidentsRowResultDto
                        {
                            Line = 0,
                            Status = "failed",
                            Message = "Selected society was not found.",
                        },
                    ],
                };
            }

            var (parsedRows, parseError) = ResidentCsvParser.Parse(request.CsvContent);
            if (parseError is not null)
            {
                return new ImportResidentsResultDto
                {
                    Failed = 1,
                    Message = parseError,
                    Rows =
                    [
                        new ImportResidentsRowResultDto
                        {
                            Line = 0,
                            Status = "failed",
                            Message = parseError,
                        },
                    ],
                };
            }

            var results = new List<ImportResidentsRowResultDto>(parsedRows.Count);
            var created = 0;
            var skipped = 0;
            var failed = 0;

            foreach (var row in parsedRows)
            {
                var rowResult = await ImportRowAsync(societyId, row, cancellationToken).ConfigureAwait(false);
                results.Add(rowResult);
                switch (rowResult.Status)
                {
                    case "created":
                        created++;
                        break;
                    case "skipped":
                        skipped++;
                        break;
                    default:
                        failed++;
                        break;
                }
            }

            if (created > 0)
                LogImported(_logger, societyId, created, null);

            return new ImportResidentsResultDto
            {
                Created = created,
                Skipped = skipped,
                Failed = failed,
                Rows = results,
            };
        }

        private async Task<ImportResidentsRowResultDto> ImportRowAsync(
            Guid societyId,
            ResidentCsvRow row,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(row.FullName) ||
                string.IsNullOrWhiteSpace(row.Phone) ||
                string.IsNullOrWhiteSpace(row.TowerWing) ||
                string.IsNullOrWhiteSpace(row.FlatNumber))
            {
                return new ImportResidentsRowResultDto
                {
                    Line = row.Line,
                    Phone = row.Phone,
                    Status = "failed",
                    Message = "fullName, phone, towerWing, and flatNumber are required.",
                };
            }

            var result = await ResidentProvisioner.ProvisionAsync(
                _residents,
                _users,
                UnitOfWork!,
                societyId,
                Guid.Empty,
                row.FullName,
                row.Phone,
                row.TowerWing,
                row.FlatNumber,
                row.IsMember,
                picUrl: null,
                isActive: true,
                cancellationToken).ConfigureAwait(false);

            if (result.Succeeded)
            {
                return new ImportResidentsRowResultDto
                {
                    Line = row.Line,
                    Phone = row.Phone,
                    Status = "created",
                    Message = result.Message,
                };
            }

            if (result.Conflict)
            {
                return new ImportResidentsRowResultDto
                {
                    Line = row.Line,
                    Phone = row.Phone,
                    Status = "skipped",
                    Message = result.Message,
                };
            }

            return new ImportResidentsRowResultDto
            {
                Line = row.Line,
                Phone = row.Phone,
                Status = "failed",
                Message = result.Message,
            };
        }

        private static bool TryResolveSociety(
            ImportResidentsFromCsvCommand request,
            out Guid societyId,
            out ImportResidentsResultDto? error)
        {
            societyId = Guid.Empty;
            error = null;
            var isPlatform = string.Equals(request.CallerRole, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);
            var isSocietyAdmin = string.Equals(request.CallerRole, "Admin", StringComparison.OrdinalIgnoreCase);

            if (!isPlatform && !isSocietyAdmin)
            {
                error = new ImportResidentsResultDto
                {
                    Forbidden = true,
                    Message = "Admin access required.",
                };
                return false;
            }

            if (isPlatform)
            {
                if (request.SocietyId is null || request.SocietyId == Guid.Empty)
                {
                    error = new ImportResidentsResultDto
                    {
                        Failed = 1,
                        Message = "Select a society.",
                        Rows =
                        [
                            new ImportResidentsRowResultDto
                            {
                                Line = 0,
                                Status = "failed",
                                Message = "Select a society.",
                            },
                        ],
                    };
                    return false;
                }

                societyId = request.SocietyId.Value;
                return true;
            }

            if (request.CallerSocietyIds.Count == 0)
            {
                error = new ImportResidentsResultDto
                {
                    Forbidden = true,
                    Message = "No society is linked to this admin.",
                };
                return false;
            }

            if (request.SocietyId is Guid requested && requested != Guid.Empty)
            {
                if (!request.CallerSocietyIds.Contains(requested))
                {
                    error = new ImportResidentsResultDto
                    {
                        Forbidden = true,
                        Message = "You cannot import residents to that society.",
                    };
                    return false;
                }

                societyId = requested;
                return true;
            }

            societyId = request.CallerSocietyIds[0];
            return true;
        }
    }
}
