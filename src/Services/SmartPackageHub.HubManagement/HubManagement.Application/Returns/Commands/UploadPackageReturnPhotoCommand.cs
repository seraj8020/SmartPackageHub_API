using HubManagement.Application.Returns.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Returns.Commands
{
    public sealed record UploadPackageReturnPhotoCommand(Guid ReturnId, UploadPackageReturnPhotoRequest Request)
        : ICreateCommand<PackageReturnCommandResult>;
}
