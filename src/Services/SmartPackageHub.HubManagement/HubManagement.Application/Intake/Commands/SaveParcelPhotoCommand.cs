using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Intake.Commands
{
    public sealed record SaveParcelPhotoCommand(string? ClientPhotoId, ReadOnlyMemory<byte> Bytes, string ContentType) : ICreateCommand<PhotoUploadResult>;
    
}
