using HubManagement.Application.HubResidents.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.HubResidents.Commands
{
    public sealed record RequestResidentOtpCommand(Guid ResidentId) : ICreateCommand<RequestOtpResponse>;
    
}
