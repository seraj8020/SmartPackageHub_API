using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Intake.Commands
{
    public sealed record LinkParcelPhotoCommand(LinkParcelPhotoRequest Request)
        : ICreateCommand<PhotoUploadResult>;
}
