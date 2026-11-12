using HubManagement.Application.HubResidents.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubResidents.Commands
{
    /// <summary>
    /// Registers a resident profile.
    /// When <paramref name="userId"/> is empty, creates a linked Resident-role user (self-registration).
    /// When <paramref name="userId"/> is set, links the profile to that existing user (admin/staff path).
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
