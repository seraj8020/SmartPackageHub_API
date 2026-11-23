using HubManagement.Application.HubResidents.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed record ImportResidentsFromCsvCommand(
        string CallerRole,
        IReadOnlyList<Guid> CallerSocietyIds,
        Guid? SocietyId,
        string CsvContent) : ICreateCommand<ImportResidentsResultDto>;
}
