using MediatR;

namespace SmartPackageHub.Applications.Abstractions.Commands
{
    public abstract record CreateCommand : IRequest<Guid>;
}
