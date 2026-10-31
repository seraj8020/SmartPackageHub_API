using HubManagement.Application.HubResidents.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubResidents.Commands
{
    /// <summary>
    /// Registers a resident profile linked to an existing user account.
    /// </summary>
    public sealed record AddResidentCommand(
        Guid societyId,
        Guid userId,
        string fullname,
        string phoneNo,
        string towerWing,
        string flatnumber,
        bool isMember,
        Uri? picUrl,
        bool isActive) : ICreateCommand<AddResidentResult>;
}
