using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubResidents.Commands
{
    /// <summary>
    /// Registers a resident profile linked to an existing user account.
    /// <paramref name="picUrl"/> accepts a URI string or empty (optional avatar).
    /// </summary>
    public sealed record AddResidentCommand(
        Guid societyId,
        Guid userId,
        string fullname,
        string phoneNo,
        string towerWing,
        string flatnumber,
        bool isMember,
        string? picUrl,
        bool isActive) : ICreateCommand<AddResidentResult>;

    public sealed record AddResidentResult(
        bool Succeeded,
        bool Conflict,
        Guid? Id,
        string Message);
}
