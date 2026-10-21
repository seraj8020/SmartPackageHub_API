using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed record AddResidentCommand(Guid societyId, Guid userId, string fullname, string phoneNo, string towerWing, string flatnumber, bool isMember, Uri picUrl, bool isActive) : CreateCommand;
   
}
