using MediatR;

namespace SmartPackageHub.Applications.Abstractions.Commands
{
#pragma warning disable CA1040 // Marker command contract carried by MediatR.
    public interface IUpdateCommand<out TResponse> : IRequest<TResponse>
    {
    }
#pragma warning restore CA1040
}
